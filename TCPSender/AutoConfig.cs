using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TCPSender
{
    public class AutoConfig
    {

        bool StillListen { get; set; }
        bool StillSend { get; set; }

        List<string> listaWykrytychIP = new List<string>();
        Thread listenThread;
        Thread sendThread;

        public AutoConfig()
        {
            StillSend = false;
            StillListen = false;
        }

        public void Send(int _seconds = 10)
        {
            sendThread = new Thread(() => Send_T(_seconds));
            sendThread.Start();
        }

        private void Send_T(int _seconds)
        {
            StillSend = true;
            int i = 0;

            IPAddress localIPAddress = CommClient.GetLocalIPAddress();
            IPEndPoint localNICEndPoint = new IPEndPoint(localIPAddress, 50000);
            UdpClient client = new UdpClient(localNICEndPoint);
            byte[] sendBytes = Encoding.UTF8.GetBytes(localIPAddress.ToString());
            client.EnableBroadcast = true;
            client.MulticastLoopback = false;
            IPEndPoint broadcastIP = new IPEndPoint(IPAddress.Broadcast, 50000);

            while (i < _seconds && StillSend == true)
            {
                client.Send(sendBytes, sendBytes.Length, broadcastIP);
                i++;
                Thread.Sleep(1000);
                //Console.WriteLine("wsylana");
            }
        }

        public void Listen()
        {
            listenThread = new Thread(() => Listen_T());
            listenThread.Start();
        }

        private void Listen_T()
        {
            StillListen = true;
            IPEndPoint receivePoint = new IPEndPoint(IPAddress.Any, 50000);
            UdpClient client = new UdpClient(50000);
            while (StillListen == true)
            {
                string receivedData = Encoding.UTF8.GetString(client.Receive(ref receivePoint));
                //Console.WriteLine("received data: {0}", receivedData);

                int result = listaWykrytychIP.IndexOf(receivedData);

                if (result == -1)
                {
                    listaWykrytychIP.Add(receivedData);
                    Console.WriteLine("Dodano element {0}", receivedData);
                }
                else
                {
                    Console.WriteLine("Element {0} juz zostal dodany", receivedData);
                }
            }
        }

        public IPAddress[] GetIPAddresses()
        {
            string[] holder = listaWykrytychIP.ToArray();
            IPAddress[] ipArray = new IPAddress[holder.Length];

            for (int i = 0; i < holder.Length; i++)
            {
                ipArray[i] = IPAddress.Parse(holder[i]);
            }
            return ipArray;
        }

        public void StopSending()
        {
            StillSend = false;
            if (sendThread != null)
            {
                sendThread.Abort();
            }
        }

        public void StopListening()
        {
            StillListen = false;
            if (listenThread != null)
            {
                listenThread.Abort();
            }
        }

        public void Close()
        {
            StopSending();
            StopListening();
        }

    }
}
