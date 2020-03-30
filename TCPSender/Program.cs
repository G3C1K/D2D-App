
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
        static void Main_VolumeMixer()
        {
            VolumeMaster master = new VolumeMaster();
            foreach (AudioSession session in master.Sessions)
            {
                if (session.Process != null)
                {
                    Console.WriteLine(session.Process.ProcessName);
                }
                Console.WriteLine(session.DisplayName);
                Console.WriteLine(session.Volume);
                Console.WriteLine(session.Mute + "\n");

                session.Volume = 100;
            }

            Console.ReadLine();
        }

        static int Main(string[] args)
        {

            Main_VolumeMixer();

            //Test.CommClient_Test();



         

            
            
            return 0;
        }
    }
}

