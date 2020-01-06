﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TCPSender
{

    public enum ConnectionType { Connect, Listen };

    public class CommClient
    {
        //KODY KOMEND (na razie)
        //m = message
        //f - plik
        //x - end communication
        //enum Command { Message = 1, File = 2, Exit = 3 };


        //porty polaczen
        int commandPort = 50001;        //port komend/message
        int filePort = 50002;           //port dla plikow. zasada dzialania jak w FTP

        TcpClient client;               //klient tcp dla komend
        IPAddress cIP;                  //adres IP. Zalezy od tego czy instancja jest klientem czy serwerem
        Action<string> outputFunc;      //funkcja ktora jest wywolywana gdy pojawi sie message od hosta
        public bool IsConnected { get; internal set; }

        BinaryWriter writer;            //writer dla SendMessage, tutaj zeby nie tworzyc caly czas nowego. na porcie 50001
        int BUFFER_SIZE = 10000;                       //rozmiar bufora dla danych pliku w bajtach

        public string DownloadPath { get; set; } = "downloads"; //directory w ktorym beda zapisywane pliki. domyslnie relative/downloads

        Thread commandLineThread;

        public CommClient(IPAddress _adresIP, ConnectionType isServer, Action<string> _funkcjaDoPrzekazaniaMessagy) //serwer = listen, client = connect
        {
            cIP = _adresIP;
            if (isServer == ConnectionType.Listen)
            {
                Listen(_adresIP);
            }
            else
            {
                Connect(_adresIP);
            }
            OpenCommandLine();
            outputFunc = _funkcjaDoPrzekazaniaMessagy;
            IsConnected = true;
            SendMessage("Connected!");
        }

        private bool Listen(IPAddress _adresInterfejsuNasluchu) //W serwerze, nasluchuje na polaczenie
        {
            TcpListener listener = new TcpListener(_adresInterfejsuNasluchu, commandPort);
            listener.Start();
            client = listener.AcceptTcpClient();
            listener.Stop();
            return true;
        }

        private bool Connect(IPAddress _adresIPHosta)       //Ustanawia polaczenie
        {
            client = new TcpClient();   //tworzenie klienta
            try
            {
                client.Connect(_adresIPHosta, commandPort); //tworzenie polaczenia
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return true;
        }

        private void OpenCommandLine()
        {
            writer = new BinaryWriter(client.GetStream());
            commandLineThread = new Thread(() => ListenForCommands(outputFunc));
            commandLineThread.Start();
        }

        private void ListenForCommands(Action<string> _outputFunc)            //Uruchamia watek nasluchiwania na wiadomosci. Do przerobienia z uwzglednieniem Action
        {
            BinaryReader reader = new BinaryReader(client.GetStream());
            string input = null;
            while (input != "x")
            {
                try
                {
                    input = reader.ReadString();
                }
                catch
                {
                    Close_Self();
                    return;
                }

                if (input == "m")
                {
                    input = reader.ReadString();
                    outputFunc(input);     
                }
                else if (input == "f")
                {
                    input = reader.ReadString();   
                    Thread rec = new Thread(() => ReceiveFile(input));
                    rec.Start();
                }
                else if (input == "v")
                {
                    input = reader.ReadString();
                    SetVolume(input);
                }
            }
            Close_Self();
        }

        private void SendFile_T(string _path)
        {
            //ustanawianie polaczenia na porcie 50002
            TcpClient fileClient = new TcpClient();
            IPAddress ownIPAddress = GetLocalIPAddress();
            TcpListener fileListener = new TcpListener(ownIPAddress, filePort);
            fileListener.Start();
            writer.Write("f");
            writer.Write(ownIPAddress.ToString());
            fileClient = fileListener.AcceptTcpClient();
            fileListener.Stop();
            //Console.WriteLine("50002 connected");

            //prep do wyslania pliku
            BinaryWriter fileWriter = new BinaryWriter(fileClient.GetStream());
            FileInfo fileInfo = new FileInfo(_path);
            string fileName = fileInfo.Name;
            long fileSize = fileInfo.Length;
            FileStream fileStream = new FileStream(_path, FileMode.Open, FileAccess.Read);
            int packetCount = (int)Math.Floor((double)(fileSize / BUFFER_SIZE));
            int reszta = (int)(fileSize - packetCount * BUFFER_SIZE);
            byte[] buffer = new byte[BUFFER_SIZE];
            byte[] lastPacket = new byte[reszta];

            //wlasciwe wysylanie
            fileWriter.Write(fileName);
            fileWriter.Write(packetCount.ToString());
            fileWriter.Write(reszta.ToString());
            for (int i = 0; i < packetCount; i++)
            {
                fileStream.Read(buffer, 0, BUFFER_SIZE);
                fileWriter.Write(buffer, 0, BUFFER_SIZE);
            }
            fileStream.Read(lastPacket, 0, reszta);
            fileWriter.Write(lastPacket, 0, reszta);

            //konczenie
            fileWriter.Close();
            fileStream.Close();
            fileClient.Close();
        }

        private void ReceiveFile(string _targetIPAddress)
        {
            //ustanawianie polaczenia na porcie 50002
            TcpClient fileClient = new TcpClient();
            fileClient.Connect(IPAddress.Parse(_targetIPAddress), filePort);
            //Console.WriteLine("50002 connected");

            //przygotowanie do odebrania pliku
            BinaryReader fileReader = new BinaryReader(fileClient.GetStream());
            string fileName = fileReader.ReadString();
            int packetCount = int.Parse(fileReader.ReadString());
            int reszta = int.Parse(fileReader.ReadString());
            byte[] buffer = new byte[BUFFER_SIZE];
            byte[] lastPacket = new byte[reszta];
            Directory.CreateDirectory(DownloadPath);
            FileStream fileStream = File.OpenWrite(DownloadPath + @"/" + fileName);

            //wlasciwe pobieranie
            for (int i = 0; i < packetCount; i++)
            {
                buffer = fileReader.ReadBytes(BUFFER_SIZE);
                fileStream.Write(buffer, 0, BUFFER_SIZE);
            }
            lastPacket = fileReader.ReadBytes(reszta);
            fileStream.Write(lastPacket, 0, reszta);

            //konczenie
            fileReader.Close();
            fileStream.Close();
            fileClient.Close();
        }

        public void SendMessage(string _message)    //Wysyla message (type 1) do odbiorcy
        {
            writer.Write("m");
            writer.Write(_message);
        }

        public void SendFile(string _path)
        {
            Thread fileThread = new Thread(() => SendFile_T(_path));
            fileThread.Start();
        }

        public void SendVolume(string _mode)
        {
            writer.Write("v");
            writer.Write(_mode);
        }

        public void SetVolume(string _mode)
        {
            switch (_mode)
            {
                case "mute":
                    VolumeChanger.Mute();
                    break;
                case "up":
                    VolumeChanger.VolumeUp();
                    break;
                case "down":
                    VolumeChanger.VolumeDown();
                    break;
            }

        }

        public void Close()
        {
           // writer.Write("x");
            Close_Self();
        }

        private void Close_Self()
        {
            writer.Close();
            client.Close();
            IsConnected = false;
        }

        public static IPAddress GetLocalIPAddress() //Zwraca adres IP domyslnej sieci. Nie musi byc tutaj, w razie potrzeby mozna przeniesc do jakiejs 100% statycznej klasy
        {
            IPAddress localIP;
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
            {
                socket.Connect("8.8.8.8", 59999);
                IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
                localIP = endPoint.Address;
            }
            return localIP;
        }

    }

   

}

class VolumeChanger
{
    private const byte VK_VOLUME_MUTE = 0xAD;
    private const byte VK_VOLUME_DOWN = 0xAE;
    private const byte VK_VOLUME_UP = 0xAF;
    private const UInt32 KEYEVENTF_EXTENDEDKEY = 0x0001;
    private const UInt32 KEYEVENTF_KEYUP = 0x0002;

    [DllImport("user32.dll")]
    static extern void keybd_event(byte bVk, byte bScan, UInt32 dwFlags, UInt32 dwExtraInfo);

    [DllImport("user32.dll")]
    static extern Byte MapVirtualKey(UInt32 uCode, UInt32 uMapType);

    public static void VolumeUp()
    {
        keybd_event(VK_VOLUME_UP, MapVirtualKey(VK_VOLUME_UP, 0), KEYEVENTF_EXTENDEDKEY, 0);
        keybd_event(VK_VOLUME_UP, MapVirtualKey(VK_VOLUME_UP, 0), KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP, 0);
    }

    public static void VolumeDown()
    {
        keybd_event(VK_VOLUME_DOWN, MapVirtualKey(VK_VOLUME_DOWN, 0), KEYEVENTF_EXTENDEDKEY, 0);
        keybd_event(VK_VOLUME_DOWN, MapVirtualKey(VK_VOLUME_DOWN, 0), KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP, 0);
    }

    public static void Mute()
    {
        keybd_event(VK_VOLUME_MUTE, MapVirtualKey(VK_VOLUME_MUTE, 0), KEYEVENTF_EXTENDEDKEY, 0);
        keybd_event(VK_VOLUME_MUTE, MapVirtualKey(VK_VOLUME_MUTE, 0), KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP, 0);
    }
}