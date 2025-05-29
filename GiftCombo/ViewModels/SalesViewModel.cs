using GiftCombo.Common;
using GiftCombo.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace GiftCombo.ViewModels;

public sealed class SalesViewModel : ViewModelBase
{
    /* ---------- look-up list (item + combo merged) ---------- */

    public record Lookup(int Id, string Name, bool IsCombo, decimal Price);

    public IEnumerable<Lookup> ItemsAndCombos { get; }

    /* ---------- draft line input (bound to controls) ---------- */

    private Lookup? _draftLine;
    public Lookup? DraftLine
    {
        get => _draftLine;
        set
        {
            _draftLine = value;
            OnPropertyChanged();

            // Notify the command that CanExecute might have changed
            ((RelayCommand)AddLineCmd).RaiseCanExecuteChanged();

            System.Diagnostics.Debug.WriteLine($"DraftLine set to: {value?.Name ?? "null"}");
        }
    }

    private int _draftQty = 1;
    public int DraftQuantity
    {
        get => _draftQty;
        set { _draftQty = Math.Max(1, value); OnPropertyChanged(); }
    }

    /* ---------- cart + recent ---------- */

    public ObservableCollection<CartLine> Lines { get; } = new();
    public ObservableCollection<Sale> Recent { get; } = new();

    /* ---------- derived total ---------- */

    public decimal Total => Lines.Sum(l => l.LineTotal);

    /* ---------- commands ---------- */

    public ICommand AddLineCmd { get; }
    public ICommand CommitCmd { get; }

    /* ---------- ctor ---------- */

    public SalesViewModel()
    {
        System.Diagnostics.Debug.WriteLine("SalesViewModel: Constructor starting");

        // Debug items loading
        var items = Services.Items.GetAll().ToList();
        System.Diagnostics.Debug.WriteLine($"SalesViewModel: Loaded {items.Count} items from database");

        // Debug combos loading  
        var combos = Services.Combos.GetAll().ToList();
        System.Diagnostics.Debug.WriteLine($"SalesViewModel: Loaded {combos.Count} combos from database");

        // Build lookup collection
        var itemLookups = items.Select(i => new Lookup(i.ItemId, i.Name, false, i.Price)).ToList();
        var comboLookups = combos.Select(c => new Lookup(c.ComboId, c.Name, true, c.Price)).ToList();

        ItemsAndCombos = itemLookups.Concat(comboLookups).ToList();

        System.Diagnostics.Debug.WriteLine($"SalesViewModel: Final ItemsAndCombos collection has {ItemsAndCombos.Count()} items");

        // Create commands with correct syntax
        AddLineCmd = new RelayCommand(
            _ => {
                System.Diagnostics.Debug.WriteLine("AddLineCmd: Execute triggered");
                AddLine();
            },
            _ => {
                var canExecute = DraftLine != null;
                System.Diagnostics.Debug.WriteLine($"AddLineCmd: CanExecute = {canExecute} (DraftLine = {DraftLine?.Name ?? "null"})");
                return canExecute;
            }
        );

        CommitCmd = new RelayCommand(_ => CommitSale(), _ => Lines.Count > 0);

        LoadRecent();
        System.Diagnostics.Debug.WriteLine("SalesViewModel: Constructor completed");
    }

    /* ---------- helpers ---------- */

    private void AddLine()
    {
        System.Diagnostics.Debug.WriteLine($"AddLine: Starting - DraftLine = {DraftLine?.Name}, Quantity = {DraftQuantity}");

        if (DraftLine is null)
        {
            System.Diagnostics.Debug.WriteLine("AddLine: DraftLine is null, returning");
            return;
        }

        System.Diagnostics.Debug.WriteLine($"AddLine: Adding {DraftLine.Name} (ID: {DraftLine.Id}, IsCombo: {DraftLine.IsCombo}, Price: {DraftLine.Price}) x {DraftQuantity}");

        var existing = Lines.FirstOrDefault(l => l.Lookup == DraftLine);

        if (existing is null)
        {
            var newLine = new CartLine(DraftLine, DraftQuantity);
            Lines.Add(newLine);
            System.Diagnostics.Debug.WriteLine($"AddLine: Added new cart line - {newLine.Lookup.Name} x {newLine.Quantity} = {newLine.LineTotal}");
        }
        else
        {
            existing.Quantity += DraftQuantity;
            System.Diagnostics.Debug.WriteLine($"AddLine: Updated existing line - {existing.Lookup.Name} x {existing.Quantity} = {existing.LineTotal}");
        }

        System.Diagnostics.Debug.WriteLine($"AddLine: Cart now has {Lines.Count} lines, Total = {Total}");

        OnPropertyChanged(nameof(Total));
        ((RelayCommand)CommitCmd).RaiseCanExecuteChanged();

        System.Diagnostics.Debug.WriteLine("AddLine: Completed successfully");
    }

    private void CommitSale()
    {
        System.Diagnostics.Debug.WriteLine($"CommitSale: Starting with {Lines.Count} lines");

        var inputs = Lines.Select(l => {
            System.Diagnostics.Debug.WriteLine($"Processing line: {l.Lookup.Name}, IsCombo: {l.Lookup.IsCombo}, ID: {l.Lookup.Id}");

            var itemId = l.Lookup.IsCombo ? (int?)null : l.Lookup.Id;
            var comboId = l.Lookup.IsCombo ? (int?)l.Lookup.Id : (int?)null;

            System.Diagnostics.Debug.WriteLine($"Calculated: ItemId = {itemId}, ComboId = {comboId}");

            var input = new SaleLineInput
            {
                ItemId = itemId,
                ComboId = comboId,
                Quantity = l.Quantity
            };

            System.Diagnostics.Debug.WriteLine($"SaleLineInput created: ItemId={input.ItemId}, ComboId={input.ComboId}, Qty={input.Quantity}");
            return input;
        }).ToList();

        try
        {
            var saleId = Services.Sales.CreateSale(DateTime.Now, inputs);
            System.Diagnostics.Debug.WriteLine($"CommitSale: Sale created with ID {saleId}");

            Lines.Clear();
            OnPropertyChanged(nameof(Total));
            ((RelayCommand)CommitCmd).RaiseCanExecuteChanged();

            LoadRecent();
            System.Diagnostics.Debug.WriteLine("CommitSale: Completed successfully");

            MessageBox.Show("Sale completed successfully!", "Success",
                           MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"CommitSale: Error - {ex.Message}");
            MessageBox.Show($"Error committing sale:\n{ex.Message}", "Error",
                           MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void LoadRecent()
    {
        System.Diagnostics.Debug.WriteLine("LoadRecent: Starting");
        Recent.Clear();

        var recentSales = Services.Sales.GetRecent().ToList();
        System.Diagnostics.Debug.WriteLine($"LoadRecent: Found {recentSales.Count} recent sales");

        foreach (var s in recentSales)
        {
            System.Diagnostics.Debug.WriteLine($"LoadRecent: Adding sale {s.SaleId} - {s.SaleDate} (total: {s.Total})");
            Recent.Add(s);
        }

        System.Diagnostics.Debug.WriteLine($"LoadRecent: Recent collection now has {Recent.Count} items");
    }

    /* ---------- nested cart line record ---------- */

    public sealed class CartLine : ViewModelBase
    {
        public Lookup Lookup { get; }
        private int _qty;
        public int Quantity
        {
            get => _qty;
            set { _qty = Math.Max(1, value); OnPropertyChanged(); OnPropertyChanged(nameof(LineTotal)); }
        }

        public decimal LineTotal => Lookup.Price * Quantity;

        public CartLine(Lookup lookup, int qty)
        {
            Lookup = lookup;
            _qty = qty;
        }
    }


}
