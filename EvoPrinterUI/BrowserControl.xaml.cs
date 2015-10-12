using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace EvoPrinterUI
{
    [System.Security.Permissions.PermissionSet(System.Security.Permissions.SecurityAction.Demand, Name = "FullTrust")]
    [System.Runtime.InteropServices.ComVisible(true)]
    public sealed class JavaScriptOptions
    {
        public bool nativeCallsAvailable{get{return true;}}

        public String loginToken{get;set;}
        
        public bool showNavBar{get{return false;}}
    }

    /// <summary>
    /// Interaction logic for BrowserControl.xaml
    /// </summary>
    public partial class BrowserControl : UserControl
    {
        public BrowserControl()
        {
            InitializeComponent();
            webControl.FailureEvent += (err) =>
            {
                var dc = DataContext as ProcessorWindow;
                if (null == dc)
                    return;

                dc.OnLoadError(err);
            };

            webControl.getOptionsEvent += () =>
            {
                var dc = DataContext as ProcessorWindow;
                if (null == dc)
                    throw new Exception("Invalid datacontext");

                return new JavaScriptOptions
                {
                    loginToken = dc.loginToken,
                };
            };

            webControl.setOptionsEvent += (setter) =>
                {
                    var dc = DataContext as ProcessorWindow;
                    if (null == dc)
                        throw new Exception("Invalid datacontext");
                    setter.Setoption("nativeCallsAvailable",true);
                    setter.Setoption("loginToken", dc.loginToken);
                    setter.Setoption("showNavBar", false);

                };
        }

    }
}
