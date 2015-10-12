using System;
using System.Collections.Generic;
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

namespace ieWrapper
{
    /// <summary>
    /// Interaction logic for bizBrowser.xaml
    /// </summary>
    public partial class bizBrowser : UserControl
    {

        public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(
            "Source",typeof(String),typeof(bizBrowser),
            new PropertyMetadata(null, (o, e) =>
            {
                var ctrl = o as bizBrowser;

                if (null != ctrl.getOptionsEvent)
                {
                    var options = ctrl.getOptionsEvent();
                    try
                    {
                        ctrl.browser.ObjectForScripting = options;
                    }
                    catch (Exception ex)
                    {
                        if (null != ctrl.FailureEvent)
                        {
                            ctrl.FailureEvent(ex.Message);
                        }
                    }

                }

                ctrl.browser.Navigate(e.NewValue as String);
            }));

        public String Source
        {
            get { return (String)GetValue(SourceProperty); }
            set { SetValue(SourceProperty, value); }
        }

        public static readonly DependencyProperty IsLoadingProperty = DependencyProperty.Register(
            "IsLoading", typeof(bool), typeof(bizBrowser));

        public bool IsLoading
        {
            get { return (bool)GetValue(IsLoadingProperty); }
            set { SetValue(IsLoadingProperty, value); }
        }

        

        public class OptionsSetter
        {

            public void Setoption(String name, bool value)
            {
            }

            public void Setoption(String name, String value)
            {
            }
        }


        public event Action<OptionsSetter> setOptionsEvent;
        public event Action<String> FailureEvent;

        public event Func<object> getOptionsEvent;


        public bizBrowser()
        {
            InitializeComponent();
        }
    }
}
