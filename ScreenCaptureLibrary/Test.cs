using D2DLibrary;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TCPSender;

namespace ScreenCaptureLibrary
{
    public static class Test
    {


        public static void LZ4_Test()
        {
            CompressScreen display = new CompressScreen(false);
            display.Iterate();


            DecompressScreen dcmpDisplay = new DecompressScreen(display.getData());
            int i = 0;
            Bitmap test1 = dcmpDisplay.DAiB(dcmpDisplay.decompressed);

            Console.ReadLine();
        }

        public static void LZ4_Test_DX()
        {
            CompressScreen display = new CompressScreen(true);
            display.IterateDX();


            DecompressScreen dcmpDisplay = new DecompressScreen(display.getData());
            int i = 0;
            Bitmap test1 = dcmpDisplay.DAiB(dcmpDisplay.decompressed);

            Console.ReadLine();
        }

        public static void LZ4_PerformanceTest()
        {
            CompressScreen display = new CompressScreen(false);

            for (int i = 0; i < 100; i++)
            {
                display.PerformanceTest();
            }

            Console.ReadLine();
        }

        public static void LZ4_DX_PerformanceTest()
        {
            CompressScreen display = new CompressScreen(true);

            for (int i = 0; i < 100; i++)
            {
                display.PerformanceTestDX();
            }

            Console.ReadLine();
        }

        public static void CaptureScreenTest()
        {
            Stopwatch sw = Stopwatch.StartNew();

            //Bitmap bitmapa = ScreenCapture.CaptureScreen();

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
