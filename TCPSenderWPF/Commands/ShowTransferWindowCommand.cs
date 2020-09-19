using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using TCPSender;
using TCPSenderWPF.TCPSender;

namespace TCPSenderWPF
{
    class ShowTransferWindowCommand : ICommand
    {
        CommClientPC client;
        TransferWindow transferWindow;
        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            //if(notifyIcon.Tag as string == "ready")
            transferWindow = TransferWindowHolder.TransferWindow;
            client = ClientHolder.Client;
            if (client != null && client.IsConnected == true)
            {
                //TransferWindow transferWindow = new TransferWindow();
                transferWindow.Show();
            }
            else
            {
                //experimental, w razie problemow usunac
                transferWindow.Show();
            }
        }
    }
}
