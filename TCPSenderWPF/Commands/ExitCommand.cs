using Hardcodet.Wpf.TaskbarNotification;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using TCPSender;
using TCPSenderWPF.TCPSender;

namespace TCPSenderWPF
{
    class ExitCommand : ICommand
    {
        public event EventHandler CanExecuteChanged;
        CommClientPC client;

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            client = ClientHolder.Client;
            if (client != null)
            {
                client.Close();
            }
            Application.Current.Shutdown();
        }
    }
}
