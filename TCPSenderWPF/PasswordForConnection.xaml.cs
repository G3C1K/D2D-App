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
using System.Text.RegularExpressions;
using TCPSender;

namespace TCPSenderWPF
{
    /// <summary>
    /// Logika interakcji dla klasy PasswordForConnection.xaml
    /// </summary>
    public partial class PasswordForConnection : Window
    {
        CommClientPC client;
        public Action<string> SetPasswordAction { internal get; set; }

        public PasswordForConnection()
        {
            InitializeComponent();

            client = ClientHolder.Client;
        }

        private void Password_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void Password_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (password.Password.Length == 4)
                acceptPassword.IsEnabled = true;
        }

        private void AcceptPassword_Click(object sender, RoutedEventArgs e)
        {
            // Tutaj funkcje wysłania informacji o oczekiwaniu na hasło
            SetPasswordAction(password.Password);
            this.Hide();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;

            this.Hide();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Keyboard.Focus(password);
        }
    }
}
