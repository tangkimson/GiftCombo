using GiftCombo.Common;
using GiftCombo.Models;
using Oracle.ManagedDataAccess.Client;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace GiftCombo.ViewModels;

public sealed class ItemsViewModel : ViewModelBase
{
    public ObservableCollection<Item> Items { get; } = new();

    public RelayCommand RefreshCmd { get; }
    public RelayCommand AddCmd { get; }
    public RelayCommand DeleteCmd { get; }
    public RelayCommand SaveCmd { get; }

    private Item? _selected;
    public Item? Selected
    {
        get => _selected;
        set
        {
            _selected = value;
            OnPropertyChanged();
            DeleteCmd.RaiseCanExecuteChanged();
            SaveCmd.RaiseCanExecuteChanged();
        }
    }


    public ItemsViewModel()
    {
        RefreshCmd = new RelayCommand(_ => Load());
        AddCmd = new RelayCommand(_ => AddNew());
        DeleteCmd = new RelayCommand(_ => Delete(), _ => Selected is not null);
        SaveCmd = new RelayCommand(_ => Save(), _ => Selected is not null);
        Load();
    }

    private void Load()
    {
        Items.Clear();
        foreach (var it in Services.Items.GetAll()) Items.Add(it);
    }

    private void AddNew()
    {
        var draft = new Item { Name = "New item", ItemType = ItemType.OTHER };
        draft.ItemId = Services.Items.Insert(draft);
        Items.Add(draft);
        Selected = draft;
    }

    private async void Delete()
    {
        if (Selected is null) return;

        var toRemove = Selected;     // capture now

        try
        {
            // ① call Oracle on a worker thread
            await Task.Run(() =>
            {
                System.Diagnostics.Debug.WriteLine($"[Delete] Calling DB for item {toRemove.ItemId}");
                Services.Items.Delete(toRemove.ItemId);
                System.Diagnostics.Debug.WriteLine($"[Delete] DB call returned");
            });

            // ② update UI collection
            var removed = Items.Remove(toRemove);
            System.Diagnostics.Debug.WriteLine($"[Delete] Items.Remove returned {removed}");

            Selected = null;   // clear selection so CanExecute updates
        }
        catch (Exception ex)   // catch *all* exceptions for now
        {
            MessageBox.Show(ex.Message, "Delete failed",
                            MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    private void Save()
    {
        if (Selected is null) return;

        try
        {
            Services.Items.Update(Selected);
            MessageBox.Show("Saved.", "Info", MessageBoxButton.OK,
                            MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Save failed",
                            MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
