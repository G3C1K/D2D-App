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

namespace TCPSenderWPF
{
    /// <summary>
    /// Logika interakcji dla klasy PasswordForConnection.xaml
    /// </summary>
    public partial class PasswordForConnection : Window
    {
        public PasswordForConnection()
        {
            InitializeComponent();
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
            this.Close();
        }

    }
}
