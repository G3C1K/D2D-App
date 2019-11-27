using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TCPSender
{
    public static class Test
    {
        public static void TCPSender_Test()
        {

            CommClient client = null;

            Console.WriteLine("listen/connect");
            string listenConnect = Console.ReadLine();
            if (listenConnect == "listen")
            {
                Console.WriteLine("Adres interfejsu do nasluchu: ");
                IPAddress adresInterfejsuDoNasluchu = IPAddress.Parse(Console.ReadLine());  //adres IP interfejsu
                client = new CommClient(adresInterfejsuDoNasluchu, ConnectionType.Listen, Console.WriteLine);
            }
            if (listenConnect == "connect")
            {
                Console.WriteLine("Adres hosta do polaczenia: ");
                IPAddress adresInterfejsuDoPolaczenia = IPAddress.Parse(Console.ReadLine());  //adres IP interfejsu
                client = new CommClient(adresInterfejsuDoPolaczenia, ConnectionType.Connect, Console.WriteLine);
            }


            string input = null;
            while (client.IsConnected == true)
            {
                input = Console.ReadLine();
                if(client.IsConnected == true)
                {
                    if (input == "plik")
                    {
                        client.SendFile(@"C:\Users\Czarek\Desktop\tru_haki\asd.zip");
                    }
                    else
                    {
                        client.SendMessage(input);
                    }
                    if (input == "x")
                    {
                        client.Close();
                    }
                }
            }
            Console.ReadLine();
        }



        public static void AutoconfigTest()
        {

            //string input = Console.ReadLine();
            //if (input == "listen")
            //{
            //    IPEndPoint receivePoint = new IPEndPoint(IPAddress.Any, 50000);
            //    UdpClient client = new UdpClient(50000);
            //    while (true)
            //    {
            //        string receivedData = Encoding.UTF8.GetString(client.Receive(ref receivePoint));
            //        Console.WriteLine("received data: {0}", receivedData);
            //    }
            //}
            //if (input == "send")
            //{
            //    IPEndPoint localNICEndPoint = new IPEndPoint(CommClient.GetLocalIPAddress(), 50000);
            //    UdpClient client = new UdpClient(localNICEndPoint);
            //    byte[] sendBytes = Encoding.UTF8.GetBytes("Is anybody there?");
            //    client.EnableBroadcast = true;
            //    client.MulticastLoopback = false;
            //    IPEndPoint broadcastIP = new IPEndPoint(IPAddress.Broadcast, 50000);
            //    while (true)
            //    {
            //        Console.ReadLine();
            //        client.Send(sendBytes, sendBytes.Length, broadcastIP);
            //    }
            //}

            AutoConfig auto = new AutoConfig();


            string input = Console.ReadLine();
            if (input == "send")
            {
                auto.Send(100); 
            }
            if(input == "listen")
            {
                auto.Listen();
            }

            

            while(input != "x")
            {
                input = Console.ReadLine();
            }
            auto.Close();

            IPAddress[] tab = auto.GetIPAddresses();
            foreach (IPAddress address in tab)
            {
                Console.WriteLine("adres: " + address);
            }

            Console.WriteLine("done");
            Console.ReadLine();

        }
    }
}
