
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
            foreach (AudioSession session in AudioSession.GetAllSessions2())
            {
                if (session.Process != null)
                {
                    // only the one associated with a defined process
                    Console.WriteLine(session.Process.ProcessName);
                    Console.WriteLine(session.DisplayName);
                    Console.WriteLine(session.ProcessId);
                    Console.WriteLine(session.Volume);

                    //session.Mute = true;
                    session.Volume = 10;

                    Console.WriteLine("\n");


                }
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

