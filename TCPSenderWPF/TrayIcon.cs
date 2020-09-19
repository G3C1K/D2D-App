using Hardcodet.Wpf.TaskbarNotification;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows;
using System.Windows.Media;
using TCPSender;

namespace TCPSenderWPF
{
    public class TrayIcon
    {
        private ICommand openTransferWindow;
        public ICommand OpenTransferWindow
        {
            get
            {
                if (openTransferWindow == null)
                {
                    openTransferWindow = new RelayCommand(NotifyIcon_DoubleClick);
                }
                return openTransferWindow;
            }
            set { openTransferWindow = value; }
        }

        CommClientPC client;
        TransferWindow transferWindow;

        //private NotifyIcon notifyIcon;
        private TaskbarIcon taskBarIcon;

        //public TrayIcon(System.Windows.Window okno, TransferWindow transferWindow)
        //{
        //client = ClientHolder.Client;
        //    this.transferWindow = transferWindow;

        //    string iconName = "Ikony/d2dnc.ico";
        //string appName = Application.ProductName;
        //System.Windows.Resources.StreamResourceInfo sri = System.Windows.Application.GetResourceStream(new Uri(@"/" + appName + ";component/" + iconName, UriKind.RelativeOrAbsolute));
        //Icon icon = new Icon(sri.Stream);

        //notifyIcon = new NotifyIcon();
        //notifyIcon.Icon = icon;
        //notifyIcon.Text = "Double click when connected to open the transfer window";
        //notifyIcon.Visible = true;
        //notifyIcon.Tag = "not ready";

        //    //notifyIcon.DoubleClick += NotifyIcon_DoubleClick;


        //    taskBarIcon = new TaskbarIcon();
        //    taskBarIcon.Icon = icon;
        //    taskBarIcon.ToolTipText = "Hello lol";
        //    taskBarIcon.Visibility = System.Windows.Visibility.Visible;
        //    taskBarIcon.Tag = "not ready";
        //    taskBarIcon.ContextMenu = new System.Windows.Controls.ContextMenu();
        //    MenuItem menuItem = new MenuItem("Exit", ExitItem_Click);
        //    //menuItem.Header = "Exit";
        //    //menuItem.Click += ExitItem_Click;
        //    taskBarIcon.ContextMenu.Items.Add(menuItem);
        //  //  taskBarIcon.ContextMenu.Items.Add(menuItem);
        //    //taskBarIcon.ContextMenu.Items.Add("Exit");
        //    taskBarIcon.DoubleClickCommand = openTransferWindow;

        //    //--------------------------------------------------
        //    //DO NAPRAWIENIA
        //    //--------------------------------------------------

        //}

        public TrayIcon(System.Windows.Window okno, TransferWindow transferWindow)
        {
            client = ClientHolder.Client;
            this.transferWindow = transferWindow;

            string iconName = "Ikony/d2dnc.ico";
            string appName = System.Windows.Forms.Application.ProductName;
            System.Windows.Resources.StreamResourceInfo sri = System.Windows.Application.GetResourceStream(new Uri(@"/" + appName + ";component/" + iconName, UriKind.RelativeOrAbsolute));
            Icon icon = new Icon(sri.Stream);

            taskBarIcon = new TaskbarIcon();
            taskBarIcon.Icon = icon;
            taskBarIcon.ToolTipText = "Hello lol";
            taskBarIcon.Visibility = System.Windows.Visibility.Visible;
            taskBarIcon.Tag = "not ready";
            taskBarIcon.ContextMenu = new System.Windows.Controls.ContextMenu();
            MenuItem menuItem = new MenuItem("Exit", ExitItem_Click);
            //menuItem.Header = "Exit";
            //menuItem.Click += ExitItem_Click;
            taskBarIcon.ContextMenu.Items.Add(menuItem);
            //  taskBarIcon.ContextMenu.Items.Add(menuItem);
            //taskBarIcon.ContextMenu.Items.Add("Exit");
            taskBarIcon.DoubleClickCommand = openTransferWindow;
        }

        private void ExitItem_Click(object sender, EventArgs e)
        {
            System.Windows.Forms.Application.Exit();
        }

        public void ChangeIcon(string iconName, string tag)
        {
            string appName = System.Windows.Forms.Application.ProductName;
            System.Windows.Resources.StreamResourceInfo sri = System.Windows.Application.GetResourceStream(new Uri(@"/" + appName + ";component/" + iconName, UriKind.RelativeOrAbsolute));
            Icon icon = new Icon(sri.Stream);
            try
            {
                taskBarIcon.Icon = icon;
                taskBarIcon.Tag = tag;
            }
            catch (Exception)
            {

                
            }
        }


        private void NotifyIcon_DoubleClick(object parameter)
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
            taskBarIcon.Visibility = System.Windows.Visibility.Hidden;
            taskBarIcon.Dispose();
        }
    }
}
