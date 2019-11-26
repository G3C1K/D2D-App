using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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


            Console.ReadLine();

        }
    }
}
