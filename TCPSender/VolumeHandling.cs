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
                    Console.WriteLine("ProcessName: " + session.Process.ProcessName);
                }
                Console.WriteLine("DisplayName: " + session.DisplayName);
                Console.WriteLine("Volume: " + session.Volume);
                Console.WriteLine("Muted: " + session.Mute);
                Console.WriteLine("State: " + session.State.ToString());

                Console.WriteLine();

                //session.Volume = 100;
            }

            Console.ReadLine();
        }

    }
}
