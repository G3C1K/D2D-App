using D2DLibrary;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
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

        public static void LZ4_Test()
        {
            CompressScreen display = new CompressScreen();
            display.Iterate();


            DecompressScreen dcmpDisplay = new DecompressScreen(display.getData());
            int i = 0;
            Bitmap test1 = dcmpDisplay.DAiB(dcmpDisplay.decompressed);

            Console.ReadLine();
        }

        public static void LZ4_Test_DX()
        {
            CompressScreen display = new CompressScreen();
            display.IterateDX();


            DecompressScreen dcmpDisplay = new DecompressScreen(display.getData());
            int i = 0;
            Bitmap test1 = dcmpDisplay.DAiB(dcmpDisplay.decompressed);

            Console.ReadLine();
        }

        public static void LZ4_PerformanceTest()
        {
            CompressScreen display = new CompressScreen();

            for(int i = 0; i<100; i++)
            {
                display.PerformanceTest();
            }

            Console.ReadLine();
        }

        public static void LZ4_DX_PerformanceTest()
        {
            CompressScreen display = new CompressScreen();

            for (int i = 0; i < 100; i++)
            {
                display.PerformanceTestDX();
            }

            Console.ReadLine();
        }

        public static void CaptureScreenTest()
        {
            Stopwatch sw = Stopwatch.StartNew();

            Bitmap bitmapa = ScreenCapture.CaptureScreen();

            TimeSpan timeToScreenCapture = sw.Elapsed;

            Console.WriteLine("Screen: {0}ms", timeToScreenCapture.TotalMilliseconds);

            Console.ReadLine();
        }

        public static void DXCaptureScreenTest()
        {


            List<double> lista = new List<double>();

            var screenStateLogger = new ScreenStateLogger();
            screenStateLogger.ScreenRefreshed += (sender, data) =>
            {
                
            };
            screenStateLogger.Start();
            Thread.Sleep(1000);
            screenStateLogger.Stop();

            //for(int i=0; i < 10; i++)
            //{
            //    Console.WriteLine(lista[i]);
            //}

            //TimeSpan timeToScreenCapture = sw.Elapsed;

            //Console.WriteLine("Screen: {0}ms", timeToScreenCapture.TotalMilliseconds);

            Console.ReadLine();
        }


    }
}
