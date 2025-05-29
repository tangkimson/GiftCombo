using GiftCombo.Common;
using System;
using System.Collections.Generic;

namespace GiftCombo.Models
{
    public enum ItemType { FOOD, DRINK, SERVICE, OTHER }

    public class Item : GiftCombo.Common.ViewModelBase
    {
        private int _itemId;
        private string _name = "";
        private ItemType _itemType;
        private decimal _price;
        private int _stockQty;

        public int ItemId
        {
            get => _itemId;
            set { _itemId = value; OnPropertyChanged(); }
        }

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        public ItemType ItemType
        {
            get => _itemType;
            set { _itemType = value; OnPropertyChanged(); }
        }

        public decimal Price
        {
            get => _price;
            set { _price = value; OnPropertyChanged(); }
        }

        public int StockQty
        {
            get => _stockQty;
            set { _stockQty = value; OnPropertyChanged(); }
        }
    }

    public class Combo : ViewModelBase
    {
        private int _comboId;
        private string _name = "";
        private decimal _price;

        public int ComboId
        {
            get => _comboId;
            set { _comboId = value; OnPropertyChanged(); }
        }

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        public decimal Price
        {
            get => _price;
            set { _price = value; OnPropertyChanged(); }
        }
    }

    public record ComboItemLine(int ItemId, int Quantity, string ItemName);

    public class ComboWithItems : Combo
    {
        public List<ComboItemLine> Items { get; } = new();
    }

    public record Sale
    {
        public int SaleId { get; init; }
        public DateTime SaleDate { get; init; }

        // Add computed properties for binding
        public DateTime Date => SaleDate;  // Alias for XAML binding
        public decimal Total { get; init; }  // We'll calculate this in the repository
    }

    public record SaleLineInput
    {
        // exactly one of the two must be set
        public int? ItemId { get; init; }
        public int? ComboId { get; init; }
        public int Quantity { get; init; }
    }
}
