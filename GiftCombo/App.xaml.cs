using System.Configuration;
using System.Data;
using System.Windows;

namespace GiftCombo
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
#if DEBUG
            // preload Oracle driver and test the credentials
            try
            {
                using var db = Database.OracleContext.GetOpenConnection();
                System.Diagnostics.Debug.WriteLine($"Oracle OK — {db.Database}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Oracle FAIL: {ex.Message}");
            }
#endif
            base.OnStartup(e);
        }
    }

}
