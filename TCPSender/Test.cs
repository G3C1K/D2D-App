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
            UdpClient client = new UdpClient();
            //client.Connect(IPAddress.Broadcast, 50001);
            byte[] sendBytes = Encoding.ASCII.GetBytes("Is anybody there?");
            string input = Console.ReadLine();
            if(input == "listen")
            {
                IPEndPoint iPEndPoint = new IPEndPoint(IPAddress.Any, 50001);
                client.Receive(ref iPEndPoint);
            }
            if(input == "send")
            {
                client.EnableBroadcast = true;
                client.MulticastLoopback = true;

                IPEndPoint ip = new IPEndPoint(IPAddress.Parse("255.255.255.255"), 50001);
                while (true)
                {
                    Console.ReadLine();
                    client.Send(sendBytes, sendBytes.Length, ip);
                }
            }
        }
    }
}
