using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;

namespace PerfMetrics
{
    public static class PM
    {
        public static void CPUUsage()
        {
            PerformanceCounter CPU = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            PerformanceCounter Mem = new PerformanceCounter("Memory", "% Committed Bytes in Use");
            PerformanceCounter DiskCRead = new PerformanceCounter("LogicalDisk", "Disk Read Bytes/sec", "C:");
            PerformanceCounter DiskCWrite = new PerformanceCounter("LogicalDisk", "Disk Write Bytes/sec", "C:");
            PerformanceCounter DiskCFree = new PerformanceCounter("LogicalDisk", "Free Megabytes", "C:");
            PerformanceCounter DiskCFreePer = new PerformanceCounter("LogicalDisk", "% Free Space", "C:");
           // DriveInfo[] drives = DriveInfo.GetDrives();
            //string[] DiskNames = new string[16];
            //int iterator = 0;
            //int iterator2 = 0;
            double CPUValue = 0;
            double MemValue = 0;
            double DiskCRValue = 0;
            double DiskCWValue = 0;
            double DiskCFreeValue=0;
            double DiskCFreePerValue=0;
            //double[] DiskRead = new double[32];
            //double[] DiskWrite = new double[32];
            //foreach (DriveInfo drive in drives)
            //{
              //  DiskNames[iterator] = drive.Name.Remove(2, 1);
                //iterator++;
            //}
            //PerformanceCounter[] Disks = new PerformanceCounter[32];
            //Disks.
            //for (int i = 0; i < 2*iterator; i=i+2)
            //{
             //   Disks[i] = new PerformanceCounter("LogicalDisk", "Avg. Disk Bytes/Read", DiskNames[i]);
              //  Disks[i+1] = new PerformanceCounter("LogicalDisk", "Avg. Disk Bytes/Write", DiskNames[i]);
          //  }    
            //for (int i = 0; i < iterator; i++)
            //{
            //    Console.WriteLine(DiskNames[i]);
                
            //}
            //System.Threading.Thread.Sleep(10000);
            //  PerformanceCounter GPU = new PerformanceCounter("GPU Engine", "Utilization Percentage", "pid_10248_luid_0x00000000_0x0000DC78_phys_0_eng_0_engtype_3D");
            while (true)
            {
                //CPUValue = CPU.NextValue();
                MemValue = Mem.NextValue();///(1024*1024*1024);
                DiskCRValue = DiskCRead.NextValue()/1024;
                DiskCWValue = DiskCWrite.NextValue()/1024;
                DiskCFreeValue = DiskCFree.NextValue()/1024;
                DiskCFreePerValue = DiskCFreePer.NextValue();
                //for (int i = 0; i < iterator; i++)
                //{
                 //   DiskRead[i] = Math.Round(Disks[i].NextValue() / 1024, 0);
                  //  DiskWrite[i] = Math.Round(Disks[i].NextValue() / 1024, 0);
                //}
                //float GPUValue = GPU.NextValue();
                Console.Clear();
                //CPUValue = CPU.NextValue();

                Console.WriteLine("CPU Usage " +Math.Round(CPUValue,2) + " %");
                Console.WriteLine("Memory Usage " +Math.Round(MemValue,2) + " %");
                //foreach()
                Console.WriteLine("C:// Free Space in GB " + Math.Round(DiskCFreeValue,2) + " GB");
                Console.WriteLine("C:// Free Space in % " + Math.Round(DiskCFreePerValue,2) + " %");
                Console.WriteLine("C:// Disk Read " + Math.Round(DiskCRValue,2) +" KB/s");
                Console.WriteLine("C:// Disk Write " + Math.Round(DiskCWValue,2) + " KB/s");

                // Console.WriteLine("C:\\ " + "Read " + Math.Round(DiskRead[0]));
                // Console.WriteLine("C:\\ " + "Write " + Math.Round(DiskWrite[0]));

                //foreach (DriveInfo drive in drives)
                //{
                //  Console.WriteLine(drive.VolumeLabel + " " + drive.Name + " " + drive.AvailableFreeSpace / 1024 / 1024 / 1024 + " GB free " + 
                //    " Disk Read " + DiskRead[iterator2] +" KB/s " + " Disk Write " + DiskWrite[iterator2+1] + " KB/s ") ;
                // iterator2 = iterator2 + 2;
                //  }

                //Console.WriteLine(GPUValue);
                CPUValue = CPU.NextValue();

                System.Threading.Thread.Sleep(200);

                CPUValue = CPU.NextValue();

            }




        }
    }
}
