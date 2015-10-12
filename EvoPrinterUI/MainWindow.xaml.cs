using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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

namespace EvoPrinterUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        readonly Processor _processor = new Processor();
        readonly StateHolder _holder;
        public MainWindow()
        {
            _holder = new StateHolder
            {
                currDisplay = _processor
            };
            
            InitializeComponent();
            DataContext = _holder;

            
            _processor.OnCLoseEvent+= ()=>
            {
                Dispatcher.BeginInvoke((Action)(() =>
                    {
                        Application.Current.Shutdown();
                    }));

            };

            this.Closing += (o, e) =>
            {
                _processor.Dispose();
            };
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            _processor.Post();
        }

        private void Button_CloseClick(object sender, RoutedEventArgs e)
        {
           /* _holder.currDisplay = _holder.currDisplay is ProcessorWindow ? (processorBase)_processor:new ProcessorWindow
            {
                indexURL ="http://localhost:8001"
                //indexURL = "http://www.yahoo.com"
            } ;*/
            Application.Current.Shutdown();

        }

    }
}
