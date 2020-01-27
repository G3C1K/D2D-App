
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



            //Test.CommClient_Test();


            //Test.AutoConfig_Test();

            //Test.LZ4_PerformanceTest();

            //Test.DXCaptureScreenTest();

            //Test.LZ4_Test();

            //Thread.Sleep(5000);

            //ScreenCaptureLibrary.Test.LZ4_DX_PerformanceTest();

            CommClient client = null;

            Console.WriteLine("listen/connect");
            string listenConnect = Console.ReadLine();
            if (listenConnect == "listen")
            {
                IPAddress adresInterfejsuDoNasluchu = CommClient.GetLocalIPAddress();
                Console.WriteLine("Nasluchiwanie na adresie: " + adresInterfejsuDoNasluchu.ToString());

                client = new CommClient(adresInterfejsuDoNasluchu, ConnectionType.Listen, Console.WriteLine);
            }
            if (listenConnect == "connect")
            {
                Console.WriteLine("Adres hosta do polaczenia: ");
                IPAddress adresInterfejsuDoPolaczenia = IPAddress.Parse(Console.ReadLine());  //adres IP interfejsu
                client = new CommClient(adresInterfejsuDoPolaczenia, ConnectionType.Connect, Console.WriteLine);
            }

            if (listenConnect == "listen")
            {
                CompressScreen display = new CompressScreen(false);
                Console.WriteLine("in listen loop");
                Thread IterateWithQueueThread = new Thread(() =>
                {
                    client.SendImageXOR();
                    for (int i = 0; i < 10000; i++)
                    {
                        display.Iterate_with_queue(client.queueXOR);
                    }
                    client.StopImageXOR();
                });
                IterateWithQueueThread.Start();
            }

            string input = null;
            while (client.IsConnected == true)
            {
                input = Console.ReadLine();
                if (client.IsConnected == true)
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

            //Console.ReadLine();

            

            
            
            return 0;
        }
    }
}

