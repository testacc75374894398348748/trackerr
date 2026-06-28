using System;

namespace FinanceTracker.Models;

public class Transaction
{
    public int     Id          { get; set; }
    public DateTime Date       { get; set; } = DateTime.Today;
    public string  Category    { get; set; } = string.Empty;
    public string  Type        { get; set; } = "Расход";
    public decimal Amount      { get; set; }
    public string  Description { get; set; } = string.Empty;
}
