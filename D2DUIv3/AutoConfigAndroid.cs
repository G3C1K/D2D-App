using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace D2DUIv3
{
    class AutoConfigAndroid
    {
        public bool StillListen { get; set; }
        List<string> listaWykrytychIP = new List<string>();
        Thread listenThread;
        public Action<List<string>> FinishAction { internal get; set; }

        public AutoConfigAndroid()
        {
            StillListen = false;
        }

        public void Listen()
        {
            StillListen = true;
            Thread internalListenThread = new Thread(() =>
            {
                listenThread = new Thread(() => Listen_T());
                listenThread.Start();
                Thread.Sleep(2000);
                StillListen = false;
                FinishAction(listaWykrytychIP);
            });
            internalListenThread.Start();
        }

        private void Listen_T()
        {

            IPEndPoint receivePoint = new IPEndPoint(IPAddress.Any, 50000);
            UdpClient client = new UdpClient(50000);
            while (StillListen == true)
            {
                string receivedData = Encoding.UTF8.GetString(client.Receive(ref receivePoint));

                listaWykrytychIP.Add(receivedData);
            }
            client.Close();
        }


    }
}