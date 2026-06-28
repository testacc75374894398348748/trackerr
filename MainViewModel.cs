using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using FinanceTracker.Data;
using FinanceTracker.Models;

namespace FinanceTracker.ViewModels;

// Простая реализация ICommand
public class RelayCommand : ICommand
{
    private readonly Func<Task> _execute;
    private readonly Func<bool>? _canExecute;
    public event EventHandler? CanExecuteChanged;
    public RelayCommand(Func<Task> execute, Func<bool>? canExecute = null)
    { _execute = execute; _canExecute = canExecute; }
    public bool CanExecute(object? p) => _canExecute?.Invoke() ?? true;
    public async void Execute(object? p) => await _execute();
    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}

public class MainViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? n = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));

    // ── Данные ──────────────────────────────────────────────────────────
    private ObservableCollection<Transaction> _transactions = new();
    public  ObservableCollection<Transaction> Transactions
    { get => _transactions; set { _transactions = value; OnPropertyChanged(); RefreshTotals(); } }

    private Transaction? _selected;
    public  Transaction? SelectedTransaction
    {
        get => _selected;
        set
        {
            _selected = value;
            OnPropertyChanged();
            (EditCommand  as RelayCommand)?.RaiseCanExecuteChanged();
            (DeleteCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }
    }

    // ── Форма ───────────────────────────────────────────────────────────
    private bool _formVisible;
    public  bool FormVisible { get => _formVisible; set { _formVisible = value; OnPropertyChanged(); } }

    private string _formDate = DateTime.Today.ToString("dd.MM.yyyy");
    public  string FormDate  { get => _formDate;  set { _formDate  = value; OnPropertyChanged(); } }

    private string _formCategory = string.Empty;
    public  string FormCategory  { get => _formCategory; set { _formCategory = value; OnPropertyChanged(); } }

    private string _formType = "Расход";
    public  string FormType  { get => _formType;  set { _formType  = value; OnPropertyChanged(); } }

    private string _formAmount = string.Empty;
    public  string FormAmount  { get => _formAmount; set { _formAmount = value; OnPropertyChanged(); } }

    private string _formDescription = string.Empty;
    public  string FormDescription  { get => _formDescription; set { _formDescription = value; OnPropertyChanged(); } }

    private string _status = string.Empty;
    public  string Status  { get => _status; set { _status = value; OnPropertyChanged(); } }

    // ── Итоги ───────────────────────────────────────────────────────────
    private decimal _income, _expense, _balance;
    public decimal Income  { get => _income;  set { _income  = value; OnPropertyChanged(); } }
    public decimal Expense { get => _expense; set { _expense = value; OnPropertyChanged(); } }
    public decimal Balance { get => _balance; set { _balance = value; OnPropertyChanged(); } }

    // ── Типы для ComboBox ────────────────────────────────────────────────
    public string[] Types { get; } = { "Доход", "Расход" };
    public string[] Categories { get; } = { "Зарплата", "Фриланс", "Еда", "Транспорт", "ЖКХ", "Развлечения", "Прочее" };

    // ── Команды ──────────────────────────────────────────────────────────
    public ICommand AddCommand    { get; }
    public ICommand EditCommand   { get; }
    public ICommand DeleteCommand { get; }
    public ICommand SaveCommand   { get; }
    public ICommand CancelCommand { get; }

    private bool _isEditing;
    private int  _editingId;

    public MainViewModel()
    {
        AddCommand    = new RelayCommand(OpenAdd);
        EditCommand   = new RelayCommand(OpenEdit,  () => SelectedTransaction != null);
        DeleteCommand = new RelayCommand(DeleteItem, () => SelectedTransaction != null);
        SaveCommand   = new RelayCommand(Save);
        CancelCommand = new RelayCommand(() => { FormVisible = false; return Task.CompletedTask; });
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            using var db = new AppDbContext();
            db.Database.EnsureCreated();
            var list = await db.Transactions.OrderByDescending(t => t.Date).ToListAsync();
            Transactions = new ObservableCollection<Transaction>(list);
            Status = $"Загружено записей: {list.Count}";
        }
        catch (Exception ex) { Status = $"Ошибка: {ex.Message}"; }
    }

    private void RefreshTotals()
    {
        Income  = Transactions.Where(t => t.Type == "Доход") .Sum(t => t.Amount);
        Expense = Transactions.Where(t => t.Type == "Расход").Sum(t => t.Amount);
        Balance = Income - Expense;
    }

    private Task OpenAdd()
    {
        _isEditing = false;
        FormDate = DateTime.Today.ToString("dd.MM.yyyy");
        FormCategory = "Еда"; FormType = "Расход";
        FormAmount = string.Empty; FormDescription = string.Empty;
        Status = string.Empty; FormVisible = true;
        return Task.CompletedTask;
    }

    private Task OpenEdit()
    {
        if (SelectedTransaction is null) return Task.CompletedTask;
        _isEditing  = true;
        _editingId  = SelectedTransaction.Id;
        FormDate        = SelectedTransaction.Date.ToString("dd.MM.yyyy");
        FormCategory    = SelectedTransaction.Category;
        FormType        = SelectedTransaction.Type;
        FormAmount      = SelectedTransaction.Amount.ToString();
        FormDescription = SelectedTransaction.Description;
        Status = string.Empty; FormVisible = true;
        return Task.CompletedTask;
    }

    private async Task Save()
    {
        if (string.IsNullOrWhiteSpace(FormCategory)) { Status = "Введите категорию."; return; }
        if (!decimal.TryParse(FormAmount, out var amount) || amount <= 0)
        { Status = "Введите корректную сумму (больше 0)."; return; }
        if (!DateTime.TryParseExact(FormDate, "dd.MM.yyyy",
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out var date))
        { Status = "Формат даты: дд.мм.гггг"; return; }

        try
        {
            using var db = new AppDbContext();
            if (_isEditing)
            {
                var e = await db.Transactions.FindAsync(_editingId);
                if (e != null)
                {
                    e.Date = date; e.Category = FormCategory;
                    e.Type = FormType; e.Amount = amount; e.Description = FormDescription;
                    await db.SaveChangesAsync();
                }
            }
            else
            {
                db.Transactions.Add(new Transaction {
                    Date = date, Category = FormCategory,
                    Type = FormType, Amount = amount, Description = FormDescription
                });
                await db.SaveChangesAsync();
            }
            FormVisible = false;
            Status = _isEditing ? "Запись обновлена." : "Запись добавлена.";
            await LoadAsync();
        }
        catch (Exception ex) { Status = $"Ошибка сохранения: {ex.Message}"; }
    }

    private async Task DeleteItem()
    {
        if (SelectedTransaction is null) return;
        try
        {
            using var db = new AppDbContext();
            var e = await db.Transactions.FindAsync(SelectedTransaction.Id);
            if (e != null) { db.Transactions.Remove(e); await db.SaveChangesAsync(); }
            Status = "Запись удалена.";
            await LoadAsync();
        }
        catch (Exception ex) { Status = $"Ошибка: {ex.Message}"; }
    }
}
