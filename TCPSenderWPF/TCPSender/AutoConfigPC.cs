using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TCPSender;

namespace TCPSender
{
    class AutoConfigPC
    {
        Func<string, bool> StillSend;
        public Action EndingEvent { internal get; set; }
        Thread advertiseThread;
        uint delayInMs = 1000;

        public AutoConfigPC(Func<string, bool> _CzyDalejWysylac, uint _delayInMs = 1000)
        {
            StillSend = _CzyDalejWysylac;
            delayInMs = _delayInMs;
        }

        public void Advertise()
        {
            advertiseThread = new Thread(() => Advertise_T());
            advertiseThread.Start();
        }

        private void Advertise_T()
        {

            IPAddress localIPAddress = CommClientPC.GetLocalIPAddress();
            IPEndPoint localNICEndPoint = new IPEndPoint(localIPAddress, 50000);
            UdpClient client = new UdpClient(localNICEndPoint);
            byte[] sendBytes = Encoding.UTF8.GetBytes(localIPAddress.ToString());
            client.EnableBroadcast = true;
            client.MulticastLoopback = false;
            IPEndPoint broadcastIP = new IPEndPoint(IPAddress.Broadcast, 50000);

            while (StillSend("udp ping on IP:" + localIPAddress.ToString()) == true)
            {
                client.Send(sendBytes, sendBytes.Length, broadcastIP);
                Thread.Sleep(1000);
            }

            EndingEvent();

            client.Close();
            
        }
    }

}
