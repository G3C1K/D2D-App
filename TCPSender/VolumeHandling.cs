using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCPSender
{
    public static class VolumeHandling
    {
        public static void MainHandler()
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

    }
}
