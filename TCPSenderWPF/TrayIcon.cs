using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Media;
using TCPSender;

namespace TCPSenderWPF
{
    public class TrayIcon
    {
        CommClientPC client;
        TransferWindow transferWindow;

        private NotifyIcon notifyIcon;

        public TrayIcon(System.Windows.Window okno, TransferWindow transferWindow)
        {
            client = ClientHolder.Client;
            this.transferWindow = transferWindow;

            string iconName = "Ikony/d2dnc.ico";
            string appName = Application.ProductName;
            System.Windows.Resources.StreamResourceInfo sri = System.Windows.Application.GetResourceStream(new Uri(@"/" + appName + ";component/" + iconName, UriKind.RelativeOrAbsolute));
            Icon icon = new Icon(sri.Stream);

            notifyIcon = new NotifyIcon();
            notifyIcon.Icon = icon;
            notifyIcon.Text = "Double click when connected to open the transfer window";
            notifyIcon.Visible = true;
            notifyIcon.Tag = "not ready";

            notifyIcon.DoubleClick += NotifyIcon_DoubleClick;

            //--------------------------------------------------
            //DO NAPRAWIENIA
            //--------------------------------------------------
            
        }
        public void ChangeIcon(string iconName, string tag)
        {
            string appName = Application.ProductName;
            System.Windows.Resources.StreamResourceInfo sri = System.Windows.Application.GetResourceStream(new Uri(@"/" + appName + ";component/" + iconName, UriKind.RelativeOrAbsolute));
            Icon icon = new Icon(sri.Stream);
            try
            {
                notifyIcon.Icon = icon;
                notifyIcon.Tag = tag;
            }
            catch (Exception)
            {

                
            }
        }


        private void NotifyIcon_DoubleClick(object sender, EventArgs e)
        {
            //if(notifyIcon.Tag as string == "ready")
            client = ClientHolder.Client;
            if(client != null && client.IsConnected == true)
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

        public void DisposeIcon()
        {
            notifyIcon.Visible = false;
            notifyIcon.Dispose();
        }
    }
}
