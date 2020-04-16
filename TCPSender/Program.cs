
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

            VolumeHandling.MainHandler();
            
            return 0;
        }
    }
}

