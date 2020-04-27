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

namespace PerfMetrics
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
        public HWUsage()
        {
            pc = new Computer();
            pc.CPUEnabled = true;
            pc.MainboardEnabled = true;
            pc.RAMEnabled = true;
            pc.HDDEnabled = true;
            pc.GPUEnabled = true;
            pc.Open();
            hardwares = pc.Hardware;
        }

        ~HWUsage()
        {
            pc.Close();
        }
#nullable enable
        public string? CPUName { get; internal set; }
        public string? MOBOName { get; internal set; }
        public string? RAMName { get; internal set; }
        public string? HDDName { get; internal set; }
        public string? GPUATIName { get; internal set; }
        public string? GPUNVName { get; internal set; }
        public double RAMUsed { get; internal set; }
        public double RAMLeft { get; internal set; }
        public double RAMTotal { get; internal set; }

        private List<string> cpuSensors = new List<string>();
        private List<string> moboSensors = new List<string>();
        private List<string> ramSensors = new List<string>();
        private List<string> gpuatiSensors = new List<string>();
        private List<string> gpunvSensors = new List<string>();
        private List<string> hddSensors = new List<string>();
        public List<string> CPUSensors { get { return cpuSensors; } internal set { cpuSensors = value; } }
        public List<string> MOBOSensors { get { return moboSensors; } internal set { moboSensors = value; } }
        public List<string> RAMSensors { get { return ramSensors; } internal set { ramSensors = value; } }
        public List<string> GPUATISensors { get { return gpuatiSensors; } internal set { gpuatiSensors = value; } }
        public List<string> GPUNVSensors { get { return gpunvSensors; } internal set { gpunvSensors = value; } }
        public List<string> HDDSensors { get { return hddSensors; } internal set { hddSensors = value; } }
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
                        if (sensor.SensorType == SensorType.Load) { CPUSensors.Add(sensor.Name + ": " + sensor.Value.GetValueOrDefault() + "%"); }
                        if (sensor.SensorType == SensorType.Temperature && sensor.Name == "CPU Package") { CPUSensors.Add(sensor.Name + ": " + sensor.Value.GetValueOrDefault() + "°C"); }
                    }
                }

                if (hardware.HardwareType == HardwareType.Mainboard)
                {
                    MOBOName = "Motherboard: " + hardware.Name;

                    foreach (ISensor sensor in hardware.Sensors)
                    {
                        if (sensor.SensorType == SensorType.Voltage) { MOBOSensors.Clear(); MOBOSensors.Add(sensor.Name + ": " + sensor.Value.GetValueOrDefault() + "V"); }
                        if (sensor.SensorType == SensorType.Temperature) { MOBOSensors.Clear(); MOBOSensors.Add(sensor.Name + ": " + sensor.Value.GetValueOrDefault() + "°C"); }
                    }
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
                            RAMTotal = RAMUsed + RAMLeft;
                            RAMUsed = Math.Round(RAMUsed, 2);
                            RAMTotal = Math.Round(RAMTotal, 2);
                            
                         }
                    }
                    ramSensors.Add("Memory Used/Memory Total " + RAMUsed + "GB/" + RAMTotal + "GB");
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
                        if (sensor.SensorType == SensorType.Load) GPUATISensors.Add(sensor.Name + ": " + sensor.Value.GetValueOrDefault().ToString("0") + "%");
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
            while (true)
            {
                Thread.Sleep(1000);
                foreach (IHardware hardware in hardwares)
                {
                    hardware.Update();
                }
                GetInfo();
                Output();

            }
        }

        public void ClearAllLists()
        {
            CPUSensors.Clear();
            MOBOSensors.Clear();
            GPUATISensors.Clear();
            GPUNVSensors.Clear();
            HDDSensors.Clear();
            RAMSensors.Clear();
        }
        public void Output()
        {
            Console.Clear();
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
            if (HDDName != null) Console.WriteLine(HDDName);
            HDDSensors.ForEach(Console.WriteLine);
           // Console.ReadKey();
        }
    }
    public class PM
    { 
        
    }
}
