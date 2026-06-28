using Avalonia.Controls;
using FinanceTracker.ViewModels;

namespace FinanceTracker.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }
}
