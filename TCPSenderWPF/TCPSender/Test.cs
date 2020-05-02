using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TCPSender
{
    public static class Test_internal
    {
        
        //public static void CommClientPC_Test()
        //{
        //    CommClientPC client = null;

        //    IPAddress adresInterfejsuDoNasluchu = CommClientPC.GetLocalIPAddress();
        //    Console.WriteLine("Nasluchiwanie na adresie: " + adresInterfejsuDoNasluchu.ToString());

        //    client = new CommClientPC(adresInterfejsuDoNasluchu, Console.WriteLine);


        //    string input = null;
        //    while (client.IsConnected == true)
        //    {
        //        input = Console.ReadLine();
        //        if (client.IsConnected == true)
        //        {
        //            if (input == "plik")
        //            {
        //                Console.WriteLine("path");
        //                client.SendFile(Console.ReadLine());
        //            }
        //            else
        //            {
        //                client.SendMessage(input);
        //            }
        //            if (input == "x")
        //            {
        //                client.Close();
        //            }
        //        }
        //    }
        //    Console.ReadLine();
        //}



        //public static void AutoConfig_Test()
        //{

        //    AutoConfig auto = new AutoConfig();


        //    string input = Console.ReadLine();
        //    if (input == "send")
        //    {
        //        auto.Send(100); 
        //    }
        //    if(input == "listen")
        //    {
        //        auto.Listen();
        //    }

            

        //    while(input != "x")
        //    {
        //        input = Console.ReadLine();
        //    }
        //    auto.Close();

        //    IPAddress[] tab = auto.GetIPAddresses();
        //    foreach (IPAddress address in tab)
        //    {
        //        Console.WriteLine("adres: " + address);
        //    }

        //    Console.WriteLine("done");
        //    Console.ReadLine();

        //}

    }
}
