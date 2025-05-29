using GiftCombo.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace GiftCombo;
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // Get the actual UserControl instances from TabItems
        var itemsTab = (TabItem)Tabs.Items[0];
        var combosTab = (TabItem)Tabs.Items[1];
        var salesTab = (TabItem)Tabs.Items[2];

        // Set DataContext on the UserControl content
        if (itemsTab.Content is FrameworkElement itemsView)
            itemsView.DataContext = new ItemsViewModel();

        if (combosTab.Content is FrameworkElement combosView)
            combosView.DataContext = new CombosViewModel();

        if (salesTab.Content is FrameworkElement salesView)
            salesView.DataContext = new SalesViewModel();
    }
}
