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
        public static void CommClient_Test()
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
                        Console.WriteLine("path");
                        client.SendFile(Console.ReadLine());
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



        public static void AutoConfig_Test()
        {

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
