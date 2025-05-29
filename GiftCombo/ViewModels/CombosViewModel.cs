using GiftCombo.Common;
using GiftCombo.Models;
using GiftCombo.Views;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using System.Windows;

namespace GiftCombo.ViewModels;

public sealed class CombosViewModel : ViewModelBase
{
    /* ---------- public bindable collections ---------- */

    public ObservableCollection<Combo> Combos { get; } = new();
    public ObservableCollection<ComboItemLine> SelectedItems { get; } = new();

    /* ---------- commands ---------- */

    public ICommand RefreshCmd { get; }
    public ICommand AddCmd { get; }
    public ICommand DeleteCmd { get; }
    public ICommand EditItemsCmd { get; }
    public ICommand SaveCmd { get; }

    /* ---------- selection ---------- */

    private Combo? _selected;
    public Combo? Selected
    {
        get => _selected;
        set
        {
            _selected = value;
            OnPropertyChanged();
            LoadItemsForSelected();
            ((RelayCommand)DeleteCmd).RaiseCanExecuteChanged();
            ((RelayCommand)EditItemsCmd).RaiseCanExecuteChanged();
            ((RelayCommand)SaveCmd).RaiseCanExecuteChanged();
        }
    }

    /* ---------- ctor ---------- */

    public CombosViewModel()
    {
        RefreshCmd = new RelayCommand(_ => Load());
        AddCmd = new RelayCommand(_ => AddNew());
        DeleteCmd = new RelayCommand(_ => Delete(), _ => Selected is not null);
        EditItemsCmd = new RelayCommand(_ => EditItems(), _ => Selected is not null);
        SaveCmd = new RelayCommand(_ => SaveSelected(), _ => Selected is not null);
        Load();
    }

    /* ---------- private helpers ---------- */

    private void Load()
    {
        Combos.Clear();
        foreach (var c in Services.Combos.GetAll())     // repo call
            Combos.Add(c);

        // refresh right-hand grid too
        LoadItemsForSelected();
    }

    private void LoadItemsForSelected()
    {
        SelectedItems.Clear();
        if (Selected is null) return;

        var combo = Services.Combos.GetWithItems(Selected.ComboId);
        if (combo is not null)
            foreach (var ln in combo.Items) SelectedItems.Add(ln);
    }

    private void AddNew()
    {
        var draft = new ComboWithItems
        {
            Name = "New combo",
            Price = 0m
        };
        draft.Items.Clear();                          // start empty

        // Insert into database and get the new ID
        int newComboId = Services.Combos.Insert(draft);

        System.Diagnostics.Debug.WriteLine($"AddNew: Created combo with ID {newComboId}");

        // Create the correct Combo record for the ObservableCollection
        var newCombo = new Combo
        {
            ComboId = newComboId,
            Name = draft.Name,
            Price = draft.Price
        };

        Combos.Add(newCombo);
        Selected = newCombo;

        System.Diagnostics.Debug.WriteLine($"DEBUG: newComboId = {newComboId}, Selected.ComboId = {Selected?.ComboId}");
        System.Diagnostics.Debug.WriteLine($"AddNew: Selected combo ID is now {Selected?.ComboId}");
    }

    private void Delete()
    {
        if (Selected is null) return;
        Services.Combos.Delete(Selected.ComboId);
        Combos.Remove(Selected);
        Selected = null;
    }

    private void EditItems()
    {
        if (Selected is null) return;

        System.Diagnostics.Debug.WriteLine($"EditItems: Selected combo ID = {Selected.ComboId}, Name = {Selected.Name}");

        if (Selected.ComboId <= 0)
        {
            MessageBox.Show($"Invalid combo selected. ComboId = {Selected.ComboId}",
                           "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        // Always pull fresh data from database to ensure we have valid combo
        var combo = Services.Combos.GetWithItems(Selected.ComboId);

        if (combo is null)
        {
            MessageBox.Show($"Combo with ID {Selected.ComboId} not found in database.",
                           "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        System.Diagnostics.Debug.WriteLine($"EditItems: Retrieved combo from DB - ID = {combo.ComboId}, Items count = {combo.Items.Count}");

        var dlg = new EditComboItemsView();
        var vm = new EditComboItemsViewModel(combo, dlg.DialogResultSetter());
        dlg.DataContext = vm;
        dlg.Owner = Application.Current.MainWindow;

        if (dlg.ShowDialog() == true)
            LoadItemsForSelected();   // refresh right-hand grid
    }
    private void SaveSelected()
    {
        if (Selected is null) return;

        try
        {
            System.Diagnostics.Debug.WriteLine($"SaveSelected: Saving combo {Selected.ComboId} - '{Selected.Name}' (price: {Selected.Price})");

            Services.Combos.UpdateCombo(Selected.ComboId, Selected.Name, Selected.Price);

            System.Diagnostics.Debug.WriteLine("SaveSelected: Combo saved successfully");

            MessageBox.Show("Combo saved successfully!", "Info",
                           MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SaveSelected: Error - {ex.Message}");
            MessageBox.Show($"Error saving combo: {ex.Message}", "Error",
                           MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
