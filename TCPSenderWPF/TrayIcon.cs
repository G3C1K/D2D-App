using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Media;

namespace TCPSenderWPF
{
    public class TrayIcon
    {
        private NotifyIcon notifyIcon;

        public TrayIcon(System.Windows.Window okno)
        {
            string iconName = "Ikony/notconnected.ico";
            string appName = Application.ProductName;
            System.Windows.Resources.StreamResourceInfo sri = System.Windows.Application.GetResourceStream(new Uri(@"/" + appName + ";component/" + iconName, UriKind.RelativeOrAbsolute));
            Icon icon = new Icon(sri.Stream);

            notifyIcon = new NotifyIcon();
            notifyIcon.Icon = icon;
            notifyIcon.Text = "Double click when connected to open the transfer window";
            notifyIcon.Visible = true;
            notifyIcon.Tag = "not ready";

            notifyIcon.DoubleClick += NotifyIcon_DoubleClick;

            
        }
        public static void ChangeIcon(TrayIcon trayIcon,string iconName, string tag)
        {
            string appName = Application.ProductName;
            System.Windows.Resources.StreamResourceInfo sri = System.Windows.Application.GetResourceStream(new Uri(@"/" + appName + ";component/" + iconName, UriKind.RelativeOrAbsolute));
            Icon icon = new Icon(sri.Stream);
            trayIcon.notifyIcon.Icon = icon;
            trayIcon.notifyIcon.Tag = tag;
        }


        private void NotifyIcon_DoubleClick(object sender, EventArgs e)
        {
            if(notifyIcon.Tag as string == "ready")
            {
                TransferWindow transferWindow = new TransferWindow();
                transferWindow.Show();
            }
            else
            {
                //Nothing
            }
        }

        public void DisposeIcon()
        {
            notifyIcon.Visible = false;
            notifyIcon.Dispose();
        }
    }
}
