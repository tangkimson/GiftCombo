// ViewModels/EditComboItemsViewModel.cs
using GiftCombo.Models;
using GiftCombo.Common;
using System.Collections.ObjectModel;
using System.Linq;

public sealed class EditComboItemsViewModel : ViewModelBase
{
    public ObservableCollection<Item> AllItems { get; }
    public ObservableCollection<ComboItemLine> Lines { get; }

    public RelayCommand AddCmd { get; }
    public RelayCommand RemoveCmd { get; }
    public RelayCommand OkCmd { get; }
    public RelayCommand CancelCmd { get; }

    private ComboItemLine? _selected;
    public ComboItemLine? Selected
    {
        get => _selected;
        set
        {
            _selected = value;
            OnPropertyChanged();
            RemoveCmd.RaiseCanExecuteChanged();
        }
    }
    public Item? SelectedItem { get; set; }
    public int DraftQty { get; set; } = 1;

    private readonly int _comboId;
    private readonly Action<bool> _close;

    public EditComboItemsViewModel(ComboWithItems combo, Action<bool> close)
    {
        _comboId = combo.ComboId;
        _close = close;

        // Debug output
        System.Diagnostics.Debug.WriteLine($"EditComboItemsViewModel: ComboId = {_comboId}, Name = {combo.Name}");

        if (_comboId <= 0)
        {
            throw new ArgumentException($"Invalid combo ID: {_comboId}");
        }

        AllItems = new ObservableCollection<Item>(Services.Items.GetAll());
        Lines = new ObservableCollection<ComboItemLine>(combo.Items);

        AddCmd = new RelayCommand(_ => Add());
        RemoveCmd = new RelayCommand(_ => Remove(), _ => Selected is not null);
        OkCmd = new RelayCommand(_ => Save());
        CancelCmd = new RelayCommand(_ => _close(false));
    }

    private void Add()
    {
        if (SelectedItem is null || DraftQty <= 0)    // nothing chosen
            return;

        var existing = Lines.FirstOrDefault(l => l.ItemId == SelectedItem.ItemId);

        if (existing is null)
            Lines.Add(new ComboItemLine(SelectedItem.ItemId,
                                        DraftQty,
                                        SelectedItem.Name));
        else
            Lines[Lines.IndexOf(existing)] =
                existing with { Quantity = existing.Quantity + DraftQty };
    }



    private void Remove()
    {
        if (Selected is null) return;
        Lines.Remove(Selected);
    }

    private void Save()
    {
        System.Diagnostics.Debug.WriteLine($"Save: Starting save for combo {_comboId}");
        System.Diagnostics.Debug.WriteLine($"Save: Lines collection has {Lines.Count} items");

        foreach (var line in Lines)
        {
            System.Diagnostics.Debug.WriteLine($"Save: Line - ItemId={line?.ItemId}, Qty={line?.Quantity}, Name='{line?.ItemName}'");
        }

        var validLines = Lines.Where(l => l != null && l.ItemId > 0 && l.Quantity > 0).ToList();

        System.Diagnostics.Debug.WriteLine($"Save: After filtering, {validLines.Count} valid lines remain");

        foreach (var line in validLines)
        {
            System.Diagnostics.Debug.WriteLine($"Save: Valid line - ItemId={line.ItemId}, Qty={line.Quantity}, Name='{line.ItemName}'");
        }

        try
        {
            Services.Combos.UpdateItems(_comboId, validLines);
            System.Diagnostics.Debug.WriteLine("Save: UpdateItems completed successfully");
            _close(true);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Save: Error in UpdateItems - {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Save: Stack trace - {ex.StackTrace}");
            throw;
        }
    }

}
