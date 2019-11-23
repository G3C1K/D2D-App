
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.IO;
using System.Threading;

namespace TCPSender
{


    class Program
    {
        static int Main(string[] args)
        {

            CommClient client = null;

            Console.WriteLine("listen/connect");
            string listenConnect = Console.ReadLine();
            if (listenConnect == "listen")
            {
                Console.WriteLine("Adres interfejsu do nasluchu: ");
                IPAddress adresInterfejsuDoNasluchu = IPAddress.Parse(Console.ReadLine());  //adres IP interfejsu
                client = new CommClient(adresInterfejsuDoNasluchu, ConnectionType.Server);
            }
            if (listenConnect == "connect")
            {
                Console.WriteLine("Adres hosta do polaczenia: ");
                IPAddress adresInterfejsuDoPolaczenia = IPAddress.Parse(Console.ReadLine());  //adres IP interfejsu
                client = new CommClient(adresInterfejsuDoPolaczenia, ConnectionType.Client);
            }

            string input = null;
            while (input != "x")
            {
                input = Console.ReadLine();
                if(input == "plik")
                {
                    client.SendFile(@"C:\Users\Czarek\Desktop\tru_haki\asd.zip");
                }
                else
                {
                    client.SendMessage(input);
                }
            }


            Console.ReadLine();
            return 0;

        }
    }
}

