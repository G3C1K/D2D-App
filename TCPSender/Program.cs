
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

            Thread.Sleep(5000);

            ScreenCaptureLibrary.Test.LZ4_DX_PerformanceTest();

            return 0;
        }
    }
}

