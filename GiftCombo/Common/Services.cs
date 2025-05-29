using GiftCombo.Database.Repositories;

namespace GiftCombo.Common;

/// <summary>
/// Very small service locator – good enough for a 3-day prototype.
/// </summary>
public static class Services
{
    public static ItemRepository Items { get; } = new();
    public static ComboRepository Combos { get; } = new();
    public static SaleRepository Sales { get; } = new();
}
