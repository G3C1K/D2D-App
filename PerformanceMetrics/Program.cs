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
            while (true)
            {
                hwusage.Update();
                hwusage.Output();
            }
           
        }
    }
}
