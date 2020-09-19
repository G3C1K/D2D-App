using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using OpenHardwareMonitor.Hardware;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Timers;
using System.Security.Cryptography.X509Certificates;
using System.Runtime.Remoting.Messaging;
using System.Reflection;
using Microsoft.VisualBasic;
using System.Management;
using System.Text.RegularExpressions;
using System.Windows;

namespace TCPSender
{
    //klasa
    //kazde property jako wlasnosc klasy
    //update metoda klasy
    //przetrzymuje computer i properties do kazdej wartosci wypisywanej na ekran
    //desktruktor pc1.Close();
    //otwieranie klasy w nowym watku, otwiera sie klasa, w konstr jest update, mozna wyciagac wartosci
    //metoda update, ktora sie wywoluje recznie, co okreslony interwal w osobnym watku

    public class HWUsage
    {
        public IHardware[] hardwares;
        public Computer pc;

        public bool ReadyFlag { get; internal set; } = false;

        public HWUsage(Action<string> ReadyDelegate)
        {
            pc = new Computer();
            pc.CPUEnabled = true;
            pc.MainboardEnabled = true;
            pc.RAMEnabled = true;
            pc.HDDEnabled = true;
            pc.GPUEnabled = true;
            pc.Open();
            hardwares = pc.Hardware;
            ReadyDelegate("ready");
            GetRAMSize();
            GetHDDInfo();
            GetHDDList();
            Update();
            ReadyFlag = true;
        }

        public void Close()
        {
            pc.Close();
        }

        public string CPUName { get; internal set; } = null;
        public string MOBOName { get; internal set; } = null;
        public string RAMName { get; internal set; } = null;
        public string GPUATIName { get; internal set; } = null;
        public string GPUNVName { get; internal set; } = null;
        public double RAMUsed { get; internal set; }
        public double RAMLeft { get; internal set; }
        public double RAMTotal { get; internal set; }
        public List<string> CPUSensors { get; internal set; } = new List<string>();
        public List<string> MOBOSensors { get; internal set; } = new List<string>();
        public List<string> RAMSensors { get; internal set; } = new List<string>();
        public List<string> GPUATISensors { get; internal set; } = new List<string>();
        public List<string> GPUNVSensors { get; internal set; } = new List<string>();
        public List<string> HDDNames { get; internal set; } = new List<string>();
        public List<ulong> HDDSizes { get; internal set; } = new List<ulong>();
        public List<string> HDDReads { get; internal set; } = new List<string>();
        public List<string> HDDWrites { get; internal set; } = new List<string>();
        public List<string> HDDLetters { get; internal set; } = new List<string>();
        public List<string> HDDRegexed { get; internal set; } = new List<string>();
        public List<PerformanceCounter> HDDReadsC { get; internal set; } = new List<PerformanceCounter>();
        public List<PerformanceCounter> HDDWritesC { get; internal set; } = new List<PerformanceCounter>();
        public List<Tuple<string, string>> DriveIDNames { get; internal set; } = new List<Tuple<string, string>>();

        public void GetInfo()
        {
            ClearAllLists();
            foreach (IHardware hardware in hardwares)
            {
                //hardware.Update();
                if (hardware.HardwareType == HardwareType.CPU)
                {
                    CPUName = "Processor: " + hardware.Name;

                    foreach (ISensor sensor in hardware.Sensors)
                    {
                        if (sensor.SensorType == SensorType.Load) { CPUSensors.Add(sensor.Name + ": " + sensor.Value.GetValueOrDefault().ToString("0.") + "%"); }
                        if (sensor.SensorType == SensorType.Temperature && sensor.Name == "CPU Package") { CPUSensors.Add(sensor.Name + ": " + sensor.Value.GetValueOrDefault().ToString("0.") + "°C"); }
                    }
                }

                if (hardware.HardwareType == HardwareType.Mainboard)
                {
                    MOBOName = "Motherboard: " + hardware.Name;

                    //foreach (ISensor sensor in hardware.Sensors)
                    //{
                    //    if (sensor.SensorType == SensorType.Voltage) { MOBOSensors.Clear(); MOBOSensors.Add(sensor.Name + ": " + sensor.Value.GetValueOrDefault() + "V"); }
                    //    if (sensor.SensorType == SensorType.Temperature) { MOBOSensors.Clear(); MOBOSensors.Add(sensor.Name + ": " + sensor.Value.GetValueOrDefault().ToString("0.") + "°C"); }
                    //}
                }

                if (hardware.HardwareType == HardwareType.RAM)
                {
                    // RAMName = hardware.Name;
                    foreach (ISensor sensor in hardware.Sensors)
                    {
                        //if (sensor.SensorType == SensorType.Load) { RAMSensors.Add(sensor.Name + ": " + sensor.Value.GetValueOrDefault() + "%"); }
                        //if (sensor.SensorType == SensorType.Data) { RAMSensors.Add(sensor.Name + ": " + sensor.Value.GetValueOrDefault() + "GB"); }
                        if (sensor.SensorType == SensorType.Data)
                        {
                            if (sensor.Name == "Used Memory")
                            {
                                RAMUsed = sensor.Value.GetValueOrDefault();
                            }
                            if (sensor.Name == "Available Memory")
                            {
                                RAMLeft = sensor.Value.GetValueOrDefault();
                            }
                            //RAMTotal = RAMUsed + RAMLeft;
                            //RAMUsed = Math.Round(RAMUsed, 2);
                            // RAMTotal = Math.Round(RAMTotal, 2);

                        }
                    }
                    RAMSensors.Add("Memory Used/Total: " + RAMUsed.ToString("0.00") + "GB/" + RAMTotal + "GB");
                }

                //if (hardware.HardwareType == HardwareType.HDD)
                //{
                //    HDDName = hardware.Name;
                //    foreach (ISensor sensor in hardware.Sensors)
                //    {
                //        if (sensor.SensorType == SensorType.Load) { HDDSensors.Add(sensor.Name + ": " + sensor.Value.GetValueOrDefault() + "%"); }//used in %
                //        if (sensor.SensorType == SensorType.Data) HDDSensors.Add(sensor.Name + ": " + sensor.Value.GetValueOrDefault() + " GB"); // data written in GB (SSD)
                //        if (sensor.SensorType == SensorType.Temperature) HDDSensors.Add(sensor.Name + ": " + sensor.Value.GetValueOrDefault() + "°C"); // temp
                //    }
                //}

                if (hardware.HardwareType == HardwareType.GpuAti)
                {
                    GPUATIName = hardware.Name;
                    foreach (ISensor sensor in hardware.Sensors)
                    {
                        if (sensor.SensorType == SensorType.Load) GPUATISensors.Add(sensor.Name + ": " + sensor.Value.GetValueOrDefault().ToString("0.") + "%");
                        if (sensor.SensorType == SensorType.Temperature && (!sensor.Name.Contains("VRM") && (!sensor.Name.Contains("Hot Spot")))) GPUATISensors.Add(sensor.Name + ": " + sensor.Value.GetValueOrDefault() + "°C");
                    }
                }

                if (hardware.HardwareType == HardwareType.GpuNvidia)
                {
                    GPUNVName = hardware.Name;
                    foreach (ISensor sensor in hardware.Sensors)
                    {
                        if (sensor.SensorType == SensorType.Load) GPUNVSensors.Add(sensor.Name + ": " + sensor.Value.GetValueOrDefault() + "%");
                        if (sensor.SensorType == SensorType.Temperature) GPUNVSensors.Add(sensor.Name + ": " + sensor.Value.GetValueOrDefault() + "°C");
                    }
                }
            }
        }

        public void Update()
        {
            //while (true)
            //{
            //    Thread.Sleep(1000);
            foreach (IHardware hardware in hardwares)
            {
                hardware.Update();
            }
            GetInfo();
            //Output();

            //}
        }

        public void ClearAllLists()
        {
            CPUSensors.Clear();
            MOBOSensors.Clear();
            GPUATISensors.Clear();
            GPUNVSensors.Clear();
            //HDDNames.Clear();
            RAMSensors.Clear();
        }
        public void Output()
        {
            //Console.Clear();
            Console.WriteLine(CPUName);
            CPUSensors.ForEach(Console.WriteLine);
            if (MOBOName != null) Console.WriteLine(MOBOName);
            MOBOSensors.ForEach(Console.WriteLine);
            if (RAMName != null) Console.WriteLine(RAMName);
            RAMSensors.ForEach(Console.WriteLine);
            Console.WriteLine(GPUATIName);
            GPUATISensors.ForEach(Console.WriteLine);
            Console.WriteLine(GPUNVName);
            GPUNVSensors.ForEach(Console.WriteLine);
            for (int i = 0; i < HDDReadsC.Count; i++)
            {
                Console.WriteLine(HDDNames[i] + " Size: " + HDDSizes[i] + "GB");
                Console.WriteLine("Read: " + (HDDReadsC[i].NextValue() / 1024 / 1024).ToString("0.00") + "MB/S");
                Console.WriteLine("Write: " + (HDDWritesC[i].NextValue() / 1024 / 1024).ToString("0.00") + "MB/S");
            }
            //HDDNames.ForEach(Console.WriteLine);
            //HDDSizes.ForEach(Console.WriteLine);
            // Console.ReadKey();
        }


        public string OutputString()
        {
            string ret = "";

            //Console.WriteLine(CPUName);
            ret += CPUName + "\n";

            //CPUSensors.ForEach(Console.WriteLine);
            foreach(string item in CPUSensors)
            {
                ret += item + "\n";
            }

            //if (MOBOName != null) Console.WriteLine(MOBOName);
            if(MOBOName != null) ret += MOBOName + "\n";

            //MOBOSensors.ForEach(Console.WriteLine);
            foreach(string item in MOBOSensors)
            {
                ret += item + "\n";
            }

            //if (RAMName != null) Console.WriteLine(RAMName);
            if(RAMName != null) ret += RAMName + "\n";

            //RAMSensors.ForEach(Console.WriteLine);
            foreach(string item in RAMSensors)
            {
                ret += item + "\n";
            }

            //Console.WriteLine(GPUATIName);
            if(!(GPUATIName == null || GPUATIName == ""))
            {
                ret += GPUATIName + "\n";
            }

            //GPUATISensors.ForEach(Console.WriteLine);
            foreach (string item in GPUATISensors)
            {
                ret += item + "\n";
            }

            //Console.WriteLine(GPUNVName);
            if (!(GPUNVName == null || GPUNVName == ""))
            {
                ret += GPUNVName + "\n"; 
            }

            //GPUNVSensors.ForEach(Console.WriteLine);
            foreach(string item in GPUNVSensors)
            {
                ret += item + "\n";
            }

            for (int i = 0; i < HDDReadsC.Count; i++)
            {
                //Console.WriteLine(HDDNames[i] + " Size: " + HDDSizes[i] + "GB");
                ret += HDDNames[i] + " Size: " + HDDSizes[i] + "GB" + "\n";

                //Console.WriteLine("Read: " + (HDDReadsC[i].NextValue() / 1024 / 1024).ToString("0.00") + "MB/S");
                ret += Application.Current.MainWindow.FindResource("Read") + " " + (HDDReadsC[i].NextValue() / 1024 / 1024).ToString("0.00") + "MB / S" + "\n";

                //Console.WriteLine("Write: " + (HDDWritesC[i].NextValue() / 1024 / 1024).ToString("0.00") + "MB/S");
                ret += Application.Current.MainWindow.FindResource("Write") + " " + (HDDWritesC[i].NextValue() / 1024 / 1024).ToString("0.00") + "MB/S" + "\n";
            }

            return ret;
        }

        public string OutputStringV2()
        {
            string ret = "";
            string read = Application.Current.MainWindow.FindResource("Read").ToString();
            string write = Application.Current.MainWindow.FindResource("Write").ToString();

            //Console.WriteLine(CPUName);
            ret += CPUName + "\n";

            //CPUSensors.ForEach(Console.WriteLine);
            foreach (string item in CPUSensors)
            {
                ret += item + "\n";
            }

            //if (MOBOName != null) Console.WriteLine(MOBOName);
            if (MOBOName != null) ret += MOBOName + "\n";

            //MOBOSensors.ForEach(Console.WriteLine);
            foreach (string item in MOBOSensors)
            {
                ret += item + "\n";
            }

            //if (RAMName != null) Console.WriteLine(RAMName);
            if (RAMName != null) ret += RAMName + "\n";

            //RAMSensors.ForEach(Console.WriteLine);
            foreach (string item in RAMSensors)
            {
                ret += item + "\n";
            }

            //Console.WriteLine(GPUATIName);
            if (GPUATIName != null)
            {
                ret += "GPU: " + GPUATIName + "\n";
            }

            //GPUATISensors.ForEach(Console.WriteLine);
            foreach (string item in GPUATISensors)
            {
                ret += item + "\n";
            }

            //Console.WriteLine(GPUNVName);
            if (GPUNVName != null)
            {
                ret += "GPU: " + GPUNVName + "\n";
            }

            //GPUNVSensors.ForEach(Console.WriteLine);
            foreach (string item in GPUNVSensors)
            {
                ret += item + "\n";
            }

            for (int i = 0; i < HDDReadsC.Count; i++)
            {
                //Console.WriteLine(HDDNames[i] + " Size: " + HDDSizes[i] + "GB");
                ret += "Drive: " + HDDNames[i] + "\n";

                //Split volume string
                ret += "Size: " + HDDSizes[i] + "GB" + "\n";

                //Console.WriteLine("Read: " + (HDDReadsC[i].NextValue() / 1024 / 1024).ToString("0.00") + "MB/S");
                ret += read + " " + (HDDReadsC[i].NextValue() / 1024 / 1024).ToString("0.00") + "MB / S" + "\n";

                //Console.WriteLine("Write: " + (HDDWritesC[i].NextValue() / 1024 / 1024).ToString("0.00") + "MB/S");

                if (i< HDDReadsC.Count - 1)
                {
                    ret += write + " " + (HDDWritesC[i].NextValue() / 1024 / 1024).ToString("0.00") + "MB/S" + "\n"; 
                }
                else
                {
                    ret += write + " " + (HDDWritesC[i].NextValue() / 1024 / 1024).ToString("0.00") + "MB/S";
                }
            }

            return ret;
        }

        public void GetRAMSize()
        {
            RAMTotal = Math.Round(Convert.ToDouble(new Microsoft.VisualBasic.Devices.ComputerInfo().TotalPhysicalMemory) / 1024 / 1024 / 1024, 2);
        }

        public void GetHDDInfo()
        {
            PerformanceCounterCategory HDDCat = new PerformanceCounterCategory("PhysicalDisk");
            HDDReads = HDDWrites = HDDCat.GetInstanceNames().ToList();

            foreach (string instance in HDDReads)
            {
                if (instance != "_Total" && !IsStringANumber(instance))
                {
                    PerformanceCounter perfcountR = new PerformanceCounter("PhysicalDisk", "Disk Read Bytes/sec", instance);
                    HDDReadsC.Add(perfcountR);
                    PerformanceCounter perfcountW = new PerformanceCounter("PhysicalDisk", "Disk Write Bytes/sec", instance);
                    HDDWritesC.Add(perfcountW);
                }

            }
            //RegexHDDs(HDDReads);
        }
        public void GetHDDList()
        {
            foreach (ManagementObject device in new ManagementObjectSearcher(@"SELECT * FROM Win32_DiskDrive").Get())
            {
                HDDNames.Add(device["Model"].ToString());
                HDDSizes.Add(Convert.ToUInt64(device["Size"]) / 1024 / 1024 / 1024);
                HDDLetters.Add(device["Name"].ToString());

                foreach (ManagementObject partition in new ManagementObjectSearcher(
                    "ASSOCIATORS OF {Win32_DiskDrive.DeviceID='" + device.Properties["DeviceID"].Value
                    + "'} WHERE AssocClass = Win32_DiskDriveToDiskPartition").Get())
                {
                    foreach (ManagementObject disk in new ManagementObjectSearcher(
                                "ASSOCIATORS OF {Win32_DiskPartition.DeviceID='"
                                    + partition["DeviceID"]
                                    + "'} WHERE AssocClass = Win32_LogicalDiskToPartition").Get())
                    {
                        DriveIDNames.Add(new Tuple<string, string>(device.GetPropertyValue("DeviceID").ToString(), disk["Name"].ToString()));
                    }
                }
            }
        }
        //public void RegexHDDs(List<string> listR)
        //{
        //    for (int i = 0; i < listR.Count; i++)
        //    {
        //        if (listR[i] == "_Total") listR.RemoveAt(i);
        //        listR[i]=Regex.Replace(listR[i], "[0-9] ", string.Empty);
        //    }

        //}

        public bool IsStringANumber(string str)
        {
            return int.TryParse(str, out int n);
        }

        public void GetHDDStats()
        {
            //while (true)
            //{
            // Console.Clear();
            for (int i = 0; i < HDDReadsC.Count; i++)
            {
                Console.WriteLine(HDDNames[i] + " Size: " + HDDSizes[i] + "GB");
                Console.WriteLine("Read: " + (HDDReadsC[i].NextValue() / 1024 / 1024).ToString("0.00") + "MB/S");
                Console.WriteLine("Write: " + (HDDWritesC[i].NextValue() / 1024 / 1024).ToString("0.00") + "MB/S");
            }
            //Thread.Sleep(1000);
            //}

        }
    }
}
