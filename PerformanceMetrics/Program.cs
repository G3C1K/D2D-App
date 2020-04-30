using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PerfMetrics
{
    public class Program
    {
        public static void Main(string[] args)
        {
            HWUsage hwusage = new HWUsage();
            hwusage.GetRAMSize();
            hwusage.GetHDDInfo();
            hwusage.GetHDDList();
            while (true)
            {
                Console.Clear();
                hwusage.Update();
                Thread.Sleep(1000);
              //  hwusage.Output();
               // hwusage.GetHDDStats();
            }

        }
    }
}
