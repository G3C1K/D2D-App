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
        TrayIcon trayIcon;
        PasswordForConnection passwordForConnection;
        int password;
        string passwordString;

        AutoConfigPC autoConfigClient;
        bool sendFlag;

        public MainWindow()
        {
            InitializeComponent();


            passwordForConnection = new PasswordForConnection();
            passwordForConnection.SetPasswordAction = SetPasswordDelegate;

            SetRandomPasswordIfFirstLaunch();

            trayIcon = new TrayIcon(this);
        }


        public void InitializeClient()  //odpala sie przy listen
        {
            button_change_password.IsEnabled = false;

            //init kleinta
            adresInterfejsuDoNasluchu = CommClientPC.GetLocalIPAddress();
            textBlock_debugLog.Text = "";
            textBlock_debugLog.Text += "Nasluchiwanie na adresie: " + adresInterfejsuDoNasluchu.ToString();
            textBlock_debugLog.Text += "\n";
            button_listen.Content = "Listening";
            client = new CommClientPC(OutputDelegate, ConnectedDelegate);
            client.DisconnectAction = DisconnectDelegate;
            client.DeviceNameAction = DeviceNameDelegate;
            client.Password = passwordString;
            ClientHolder.Client = client;
            client.Start(adresInterfejsuDoNasluchu);
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
                    trayIcon.ChangeIcon("Ikony/connected.ico", "ready");
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
                    //Zamykanie wszystkich okien oprócz MainWindow
                    for (int i = App.Current.Windows.Count - 1; i > 0; i--)
                        App.Current.Windows[i].Hide();
                    connected_device.Text = "None";
                    if (trayIcon != null)
                    {
                        try
                        {
                            trayIcon.ChangeIcon("Ikony/notconnected.ico", "not ready");
                        }
                        catch (Exception e)
                        {
                            textBlock_debugLog.Text += e.Message + "\n";
                        }
                    }
                    button_listen.Content = "Listen";

                    button_change_password.IsEnabled = true;
                })
                );
        }

        public void DeviceNameDelegate(string name)
        {
            connected_device.Dispatcher.Invoke(
                () =>
                {
                    connected_device.Text = name;
                });
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

        private void Window_Closed(object sender, EventArgs e)
        {

        }

        private void Button_advertise_Click(object sender, RoutedEventArgs e)
        {
            if ((string)button_advertise.Content == "Advertise IP")
            {
                sendFlag = true;
                autoConfigClient = new AutoConfigPC(StillSend);
                autoConfigClient.EndingEvent = () =>
                {
                    button_advertise.Dispatcher.Invoke(
                        () => { button_advertise.IsEnabled = true;
                            button_advertise.Content = "Advertise IP";
                        }
                        );
                };
                autoConfigClient.Advertise();
                //button_stop_advertising.IsEnabled = true;
                //button_advertise.IsEnabled = false;
                // Zmiana w drugi przycisk
                //button_advertise.Click -= Button_advertise_Click;
                //button_advertise.Click += Button_stop_advertising_Click;
                button_advertise.Content = "Stop Advertising";
            }
            else if ((string)button_advertise.Content == "Stop Advertising")
            {
                sendFlag = false;
                button_advertise.IsEnabled = false;
            }
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

        //private void Button_stop_advertising_Click(object sender, RoutedEventArgs e)
        //{
        //    sendFlag = false;
        //    //button_stop_advertising.IsEnabled = false;
        //    //button_advertise.IsEnabled = true;
        //    // Zmiana w drugi przycisk
        //    button_advertise.Click -= Button_stop_advertising_Click;
        //    button_advertise.Click += Button_advertise_Click;
        //    button_advertise.Content = "Advertise IP";
        //}

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            trayIcon.DisposeIcon();
            if (client!= null)
            {
                client.Close();
            }
        }

        private void Button_change_password_Click(object sender, RoutedEventArgs e)
        {
            passwordForConnection.Show();
        }

        public void SetPasswordDelegate(string input)
        {
            textBlock_password.Dispatcher.Invoke(() =>
            {
                textBlock_password.Text = "****";
                passwordString = input;
                button_listen.IsEnabled = true;
            });
        }

        private void SetRandomPasswordIfFirstLaunch()
        {
            bool passwordState = true;
            if (passwordString == null)
                passwordState = false;
            //bool passwordState = int.TryParse(passwordString, out int ret);
            if (passwordState == false)
            {
                Random rng = new Random();
                int pass = rng.Next(1000, 9999);
                SetPasswordDelegate(pass.ToString());
            }
            else
            {

            }
        }

        private void Button_show_password_Click(object sender, RoutedEventArgs e)
        {
            if ((string)button_show_password.Content == "Show")
            {
                textBlock_password.Text = passwordString;
                button_show_password.Content = "Hide";
            }
            else if ((string)button_show_password.Content == "Hide")
            {
                textBlock_password.Text = "****";
                button_show_password.Content = "Show";
            }
        }
    }
}
