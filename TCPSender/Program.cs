﻿
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
            //Test_internal.CommClientPC_Test();
            
            return 0;

            //do zrobienia:
            //globalvolume
            //refresh volume
            //autorefresh?
            //dll as systemsounds
            //styl
            //pc ui
            //autoconfig

        }
    }
}

