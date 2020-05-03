using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
using TCPSender;

namespace TCPSenderWPF
{
    /// <summary>
    /// Logika interakcji dla klasy MainWindow.xaml
    /// </summary>
    
    //test
    //test2
    public partial class MainWindow : Window
    {
        CommClientPC client = null;
        IPAddress adresInterfejsuDoNasluchu;

        AutoConfigPC autoConfigClient;
        bool sendFlag;

        public MainWindow()
        {
            InitializeComponent();

            
        }


        public void InitializeClient()
        {
            //init kleinta
            adresInterfejsuDoNasluchu = CommClientPC.GetLocalIPAddress();
            textBlock_debugLog.Text = "";
            textBlock_debugLog.Text += "Nasluchiwanie na adresie: " + adresInterfejsuDoNasluchu.ToString();
            textBlock_debugLog.Text += "\n";
            button_listen.Content = "Listening";
            client = new CommClientPC(adresInterfejsuDoNasluchu, OutputDelegate, ConnectedDelegate);
            client.DisconnectAction = DisconnectDelegate;
        }

        public void OutputDelegate(string input)
        {
            textBlock_debugLog.Dispatcher.Invoke(
                (Action)(() => 
                {
                    textBlock_debugLog.Text += input + "\n";
                })
                );
        }

        public void ConnectedDelegate(string input)
        {
            textBlock_debugLog.Dispatcher.Invoke(
                (Action)(() =>
                {
                    textBlock_debugLog.Text += input + " ConnectedDelegate \n";
                    button_advertise.IsEnabled = false;
                    button_listen.Content = "Disconnect";
                })
                );
        }

        public void DisconnectDelegate(string output)
        {
            textBlock_debugLog.Dispatcher.Invoke(
                (Action)(() =>
                {
                    textBlock_debugLog.Text += output + "\n";
                    button_listen.Content = "Listen";
                })
                );
        }

        private void Button_listen_Click(object sender, RoutedEventArgs e)
        {

            if ((string)button_listen.Content == "Listen")
            {
                InitializeClient();
                autoConfigClient = new AutoConfigPC(StillSend);
                button_advertise.IsEnabled = true;
            }
            else if((string)button_listen.Content == "Listening")
            {
                textBlock_debugLog.Text += "Already listening!\n";
            }
            else if((string)button_listen.Content == "Disconnect")
            {
                client.Close();
                button_listen.Content = "Listen";
            }
        }

        private void Button_advertise_Click(object sender, RoutedEventArgs e)
        {
            sendFlag = true;
            autoConfigClient = new AutoConfigPC(StillSend);
            autoConfigClient.EndingEvent = () =>
            {
                button_advertise.Dispatcher.Invoke(
                    () => { button_advertise.IsEnabled = true; }
                    );
            };
            autoConfigClient.Advertise();
            button_stop_advertising.IsEnabled = true;
            button_advertise.IsEnabled = false;
        }

        private bool StillSend(string we)
        {
            textBlock_debugLog.Dispatcher.Invoke(
                (Action)(() =>
                {
                    textBlock_debugLog.Text += we + "\n";
                })
                );
            return sendFlag;
        }

        private void Button_stop_advertising_Click(object sender, RoutedEventArgs e)
        {
            sendFlag = false;
            button_stop_advertising.IsEnabled = false;
            //button_advertise.IsEnabled = true;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if(client!= null)
            {
                client.Close();
            }
        }
    }
}
