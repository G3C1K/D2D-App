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
    public partial class MainWindow : Window
    {
        CommClientPC client = null;
        IPAddress adresInterfejsuDoNasluchu;
        TrayIcon trayIcon;
        PasswordForConnection passwordForConnection;

        public MainWindow()
        {
            InitializeComponent();

            trayIcon = new TrayIcon(this);
        }


        public void InitializeClient()
        {
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
                    //Okno do wpisania hasła
                    passwordForConnection = new PasswordForConnection();
                    passwordForConnection.Show();
                    //Tu wstawić funkcję ustawiania nazwy urządzenia odebraną od urządzenia:
                    connected_device.Text = "default phone";
                    TrayIcon.ChangeIcon(trayIcon, "Ikony/connected.ico", "ready");
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
                        App.Current.Windows[i].Close();
                    connected_device.Text = "None";
                    TrayIcon.ChangeIcon(trayIcon, "Ikony/notconnected.ico", "not ready");
                    button_listen.Content = "Listen";
                })
                );
        }

        private void Button_listen_Click(object sender, RoutedEventArgs e)
        {

            if ((string)button_listen.Content == "Listen")
            {
                InitializeClient();
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
            trayIcon.DisposeIcon();
        }

    }
}
