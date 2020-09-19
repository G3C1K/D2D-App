using Hardcodet.Wpf.TaskbarNotification;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TCPSender;
using TCPSenderWPF.TCPSender;

namespace TCPSenderWPF
{
    /// <summary>
    /// Logika interakcji dla klasy MainWindow.xaml
    /// </summary>
    
    //test
    //test2
    public partial class MainWindow
    {
        CommClientPC client = null;
        IPAddress adresInterfejsuDoNasluchu;
        // TrayIcon trayIcon;
       // TaskbarIcon taskbarIcon;
        PasswordForConnection passwordForConnection;
        TransferWindow transferWindow;
        int password;
        string passwordString;
        AutoConfigPC autoConfigClient;
        bool sendFlag;
        //Ikony do paska okna
        //BitmapFrame connectedIcon = BitmapFrame.Create(new Uri("pack://application:,,,/Ikony/d2dc.ico", UriKind.RelativeOrAbsolute));
        //BitmapFrame notconnectedIcon = BitmapFrame.Create(new Uri("pack://application:,,,/Ikony/d2dnc.ico", UriKind.RelativeOrAbsolute));
        Icon connectedIcon = new Icon("Ikony/d2dc.ico");
        Icon notconnectedIcon = new Icon("Ikony/d2dnc.ico");
        OpenFileDialog openFileDialog;

        //listy plikow
        List<string> fileList_internal = new List<string>();
        List<string> stringList = new List<string>();

        public MainWindow()
        {
            InitializeComponent();
            this.Hide();

            CultureInfo ci = CultureInfo.InstalledUICulture;
            Properties.Settings.Default.SystemLanguage = ci.TwoLetterISOLanguageName;
            ChooseLanguage();
            passwordString = Properties.Settings.Default.Password;

            passwordForConnection = new PasswordForConnection();
            passwordForConnection.SetPasswordAction = SetPasswordDelegate;

            TransferWindowHolder.TransferWindow = new TransferWindow();
            transferWindow = TransferWindowHolder.TransferWindow;
            transferWindow.TransferAction = TransferHistoryDelegate;
            transferWindow.FinishAction = FinishDelegate_TransferWindow;

            SetRandomPasswordIfFirstLaunch();

            openFileDialog = new OpenFileDialog();
            openFileDialog.Multiselect = true;

            //trayIcon = new TrayIcon(this, transferWindow);
            taskbarIcon.ToolTipText = "IP: " + CommClientPC.GetLocalIPAddress().ToString() + "\n" + (string)System.Windows.Application.Current.MainWindow.FindResource("ConnectedDevice");
            taskbarIcon.Icon = notconnectedIcon;
        }

        //--------------------------------------------------
        //CLIENT START
        //--------------------------------------------------

        public void InitializeClient()  //odpala sie przy listen
        {
            button_change_password.IsEnabled = false;

            //init kleinta
            adresInterfejsuDoNasluchu = CommClientPC.GetLocalIPAddress();
            textBlock_debugLog.Text = "";
            textBlock_debugLog.Text += "Nasluchiwanie na adresie: " + adresInterfejsuDoNasluchu.ToString();
            textBlock_debugLog.Text += "\n";
            if ((string)button_listen.Content == "Listen")
                button_listen.Content = "Listening";
            else if((string)button_listen.Content == "Nasłuchuj")
                button_listen.Content = "Nasłuchiwanie";
            client = new CommClientPC(OutputDelegate, ConnectedDelegate);

            client.DisconnectAction = DisconnectDelegate;
            client.DeviceNameAction = DeviceNameDelegate;
            client.FileInstAction = FileInstDelegate;
            client.FileRemoveAction = FileRemoveDelegate;


            client.Password = passwordString;
            ClientHolder.Client = client;
            client.Start(adresInterfejsuDoNasluchu);
        }

        public void OutputDelegate(string input)    //delegat = wypis do loga
        {
            textBlock_debugLog.Dispatcher.Invoke(
                (Action)(() => 
                {
                    textBlock_debugLog.Text += input + "\n";
                })
                );
        }

        public void ConnectedDelegate(string input) //delegat - po polaczeniu
        {
            textBlock_debugLog.Dispatcher.Invoke(
                (Action)(() =>
                {
                    sendFlag = false;
                    button_advertise.IsEnabled = false;


                    textBlock_debugLog.Text += input + " ConnectedDelegate \n";
                  //  trayIcon.ChangeIcon("Ikony/d2dc.ico", "ready");
                    taskbarIcon.Icon = connectedIcon;
                    taskbarIcon.Tag = "ready";
                   // taskbarIcon.ToolTipText = "IP: " + GetIPAdress().ToString() + "\n" + (string)System.Windows.Application.Current.MainWindow.FindResource("ConnectedDevice") + nazwa_urzadzenia;
                    // this.Icon = connectedIcon;
                    button_advertise.IsEnabled = false;
                    if ((string)button_listen.Content == "Listening")
                        button_listen.Content = "Disconnect";
                    else if ((string)button_listen.Content == "Nasłuchiwanie")
                        button_listen.Content = "Rozłącz";
                })
                );
        }

        public void DisconnectDelegate(string output)   //delegat - po rozlaczeniu
        {
            textBlock_debugLog.Dispatcher.Invoke(
                (Action)(() =>
                {
                    textBlock_debugLog.Text += output + "\n";
                    //Zamykanie wszystkich okien oprócz MainWindow
                    for (int i = App.Current.Windows.Count - 1; i > 0; i--)
                        App.Current.Windows[i].Hide();
                    connected_device.Text = "";
                    taskbarIcon.ToolTipText = "IP: " + CommClientPC.GetLocalIPAddress().ToString() + "\n" + (string)System.Windows.Application.Current.MainWindow.FindResource("ConnectedDevice") + connected_device.Text;
                    // if (trayIcon != null)
                    if (taskbarIcon != null)
                    {
                        try
                        {
                            //trayIcon.ChangeIcon("Ikony/d2dnc.ico", "not ready");
                            //this.Icon = notconnectedIcon;
                            taskbarIcon.Icon = notconnectedIcon;
                            taskbarIcon.Tag = "not ready";
                        }
                        catch (Exception e)
                        {
                            textBlock_debugLog.Text += e.Message + "\n";
                        }
                    }
                    if ((string)button_listen.Content == "Disconnect")
                        button_listen.Content = "Listen";
                    else if ((string)button_listen.Content == "Rozłącz")
                        button_listen.Content = "Nasłuchuj";

                    button_change_password.IsEnabled = true;
                })
                );
        }

        public void DeviceNameDelegate(string name) //delegat - po polaczeniu przychodzi nazwa urzadzenia android
        {
            connected_device.Dispatcher.Invoke(
                () =>
                {
                    connected_device.Text = name;
                    taskbarIcon.ToolTipText = "IP: " + CommClientPC.GetLocalIPAddress().ToString() + "\n" + (string)System.Windows.Application.Current.MainWindow.FindResource("ConnectedDevice") + connected_device.Text;
                });
        }

        public void FileInstDelegate(List<string> fileList) //delegat - po uruchomieniu filetransferactivity wysyla flage instant, uruchamia sie ten delegat
        {
            if (fileList != null)
            {
                foreach (string item in fileList_internal)
                {
                    fileList.Add(item);
                }
            }
        }

        public void FileRemoveDelegate(string file) //delegat - po przyjsciu flagi remove usuwa plik z listy
        {
            fileList_internal.Remove(file);
            FinishDelegate_TransferWindow("removed item from android: " + file);
        }

        public void TransferHistoryDelegate(string obj) //delegat - po 
        {
            textBlock_transferHistory.Dispatcher.Invoke(
                (Action)(() =>
                {
                    fileList_internal.Add(obj);
                })
                );
        }

        //resetuje wyswietlana liste plikow, main lista jest List<>
        public void FinishDelegate_TransferWindow(string obj)   //PODWOJNY DELEGAT - po usunieciu i dodaniu pliku
        {
            textBlock_transferHistory.Dispatcher.Invoke(
                (Action)(() =>
                {
                    textBlock_transferHistory.Text = "";

                    foreach(string item in fileList_internal)
                    {
                        textBlock_transferHistory.Text += item + "\n";
                    }

                    textBlock_debugLog.Text += obj + "\n";
                })
                );
        }

        

        

        private void Button_listen_Click(object sender, RoutedEventArgs e)
        {

            if ((string)button_listen.Content == "Listen" || (string)button_listen.Content == "Nasłuchuj")
            {
                InitializeClient();
                autoConfigClient = new AutoConfigPC(StillSendDelegate);
                button_advertise.IsEnabled = true;
            }
            else if((string)button_listen.Content == "Listening" || (string)button_listen.Content == "Nasłuchiwanie")
            {
                textBlock_debugLog.Text += "Already listening!\n";
            }
            else if((string)button_listen.Content == "Disconnect" || (string)button_listen.Content == "Rozłącz")
            {
                client.Close();
                if((string)button_listen.Content == "Disconnect")
                    button_listen.Content = "Listen";
                else if((string)button_listen.Content == "Rozłącz")
                    button_listen.Content = "Nasłuchuj";
                button_change_password.IsEnabled = true;
            }
        }

        private void Button_advertise_Click(object sender, RoutedEventArgs e)
        {
            if ((string)button_advertise.Content == "Advertise IP" || (string)button_advertise.Content == "Ogłaszaj IP")
            {
                sendFlag = true;
                autoConfigClient = new AutoConfigPC(StillSendDelegate);
                autoConfigClient.EndingEvent = () =>
                {
                    button_advertise.Dispatcher.Invoke(
                        () => { button_advertise.IsEnabled = true;

                            if ((string)button_advertise.Content == "Stop Advertising")
                                button_advertise.Content = "Advertise IP";
                            else if ((string)button_advertise.Content == "Przestań ogłaszać")
                                button_advertise.Content = "Ogłaszaj IP";
                        }
                        );
                };
                autoConfigClient.Advertise();
                //button_stop_advertising.IsEnabled = true;
                //button_advertise.IsEnabled = false;
                // Zmiana w drugi przycisk
                //button_advertise.Click -= Button_advertise_Click;
                //button_advertise.Click += Button_stop_advertising_Click;
                if((string)button_advertise.Content == "Advertise IP")
                    button_advertise.Content = "Stop Advertising";
                else if((string)button_advertise.Content == "Ogłaszaj IP")
                    button_advertise.Content = "Przestań ogłaszać";
            }
            else if ((string)button_advertise.Content == "Stop Advertising" || (string)button_advertise.Content == "Przestań ogłaszać")
            {
                sendFlag = false;
                button_advertise.IsEnabled = false;
            }
        }

        private bool StillSendDelegate(string we)
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
            // trayIcon.DisposeIcon();
            taskbarIcon.Dispose();
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
                //textBlock_password.Text = "****";
                if((string)button_show_password.Content == "Hide" || (string)button_show_password.Content == "Ukryj")
                {
                    textBlock_password.Text = input;
                }
                Properties.Settings.Default.Password = input;
                Properties.Settings.Default.Save();
                passwordString = input;
                button_listen.IsEnabled = true;
            });
        }

        private void SetRandomPasswordIfFirstLaunch()
        {
            bool passwordState = true;
            //if (passwordString == null)
            //    passwordState = false;
            passwordState = int.TryParse(passwordString, out int ret);
            if (passwordState == false)
            {
                Random rng = new Random();
                int pass = rng.Next(1000, 9999);
                SetPasswordDelegate(pass.ToString());
            }
            else
            {
                textBlock_password.Text = "****";
                button_listen.IsEnabled = true;
            }
        }

        private void Button_show_password_Click(object sender, RoutedEventArgs e)
        {
            if ((string)button_show_password.Content == "Show")
            {
                textBlock_password.Text = passwordString;
                button_show_password.Content = "Hide";
            }
            else if ((string)button_show_password.Content == "Pokaż")
            {
                textBlock_password.Text = passwordString;
                button_show_password.Content = "Ukryj";
            }
            else if ((string)button_show_password.Content == "Hide")
            {
                textBlock_password.Text = "****";
                button_show_password.Content = "Show";
            }
            else if ((string)button_show_password.Content == "Ukryj")
            {
                textBlock_password.Text = "****";
                button_show_password.Content = "Pokaż";
            }
        }

        private void Button_Browse_Click(object sender, RoutedEventArgs e)
        {
            DialogResult dr = this.openFileDialog.ShowDialog();
            List<string> listaPlikow = new List<string>();

            if(dr == System.Windows.Forms.DialogResult.OK)
            {
                foreach (string file in openFileDialog.FileNames)
                {
                    listaPlikow.Add(file);
                }

                transferWindow.AddFiles(listaPlikow);
            }
        }


        private void ChooseLanguage()
        {
            if(Properties.Settings.Default.SystemLanguage == "pl")
            {
                var languageDictionary = new ResourceDictionary();
                string directory = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                languageDictionary.Source = new Uri("\\LanguageResources\\MainWindow.pl-PL.xaml", UriKind.Relative);

                this.Resources.MergedDictionaries.Add(languageDictionary);
            }
            else if(Properties.Settings.Default.SystemLanguage == "en")
            {
                var languageDictionary = new ResourceDictionary();
                string directory = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                languageDictionary.Source = new Uri("\\LanguageResources\\MainWindow.en-EN.xaml", UriKind.Relative);

                this.Resources.MergedDictionaries.Add(languageDictionary);
            }
        }

        private void AcrylicWindow_StateChanged(object sender, EventArgs e)
        {
            if (WindowState is WindowState.Minimized)
            {
                this.Hide();
            }
        }
    }
}
