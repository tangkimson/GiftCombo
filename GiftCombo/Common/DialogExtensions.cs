using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace GiftCombo.Common
{
    public static class DialogExtensions
    {
        public static Action<bool> DialogResultSetter(this Window w) =>
            ok => w.Dispatcher.Invoke(() => w.DialogResult = ok);
    }
}
