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
using System.Windows.Shapes;

namespace TCPSenderWPF
{
    /// <summary>
    /// Logika interakcji dla klasy TransferWindow.xaml
    /// </summary>
    public partial class TransferWindow : Window
    {
        public TransferWindow()
        {
            InitializeComponent();

            this.Loaded += new RoutedEventHandler(Window_Loaded);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var desktopArea = System.Windows.SystemParameters.WorkArea;
            this.Left = desktopArea.Right - this.Width;
            this.Top = desktopArea.Bottom - this.Height;
        }

        private void Space_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                foreach(var file in files)
                {
                    //Sending files function here
                    this.lastSent.Content = file;
                }
            }
            if (e.Data.GetDataPresent(DataFormats.Text))
            {
                string text = (string)e.Data.GetData(DataFormats.Text);
                //Sending text function here if first if doesn't work for text as well
                this.lastSent.Content = text;
            }
            var tsPanel = sender as StackPanel;
            tsPanel.Background = Brushes.Gray;
        }

        private void Space_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop) || e.Data.GetDataPresent(DataFormats.Text))
            {
                e.Effects = DragDropEffects.Copy;
                var tsPanel = sender as StackPanel;
                tsPanel.Background = Brushes.LightGray;
            }
        }

        private void Space_DragLeave(object sender, DragEventArgs e)
        {
            var tsPanel = sender as StackPanel;
            tsPanel.Background = Brushes.Gray;
        }

    }
}
