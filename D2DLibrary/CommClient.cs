using System;
using System.Collections.Concurrent;
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
        int imageXORPort = 50003;

        TcpClient client;               //klient tcp dla komend
        IPAddress cIP;                  //adres IP. Zalezy od tego czy instancja jest klientem czy serwerem
        Action<string> outputFunc;      //funkcja ktora jest wywolywana gdy pojawi sie message od hosta
        public bool IsConnected { get; internal set; }

        BinaryWriter writer;            //writer dla SendMessage, tutaj zeby nie tworzyc caly czas nowego. na porcie 50001
        int BUFFER_SIZE = 10000;                       //rozmiar bufora dla danych pliku w bajtach

        public string DownloadPath { get; set; } = "downloads"; //directory w ktorym beda zapisywane pliki. domyslnie relative/downloads

        Thread commandLineThread;

        Thread imageXORThreadSend;
        Thread imageXORThreadRec;
        bool stillSend = false;
        public BlockingCollection<byte[]> queueXOR { get; internal set; }


        VolumeMaster volumeMaster;
        public VolumeAndroid[] VolumeArrayForAndroid { get; internal set; }
        public bool volumeReady = false;

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
                else if (input == "i")
                {
                    input = reader.ReadString();
                    imageXORThreadRec = new Thread(() => ReceiveImageXOR(input));
                    imageXORThreadRec.Start();
                }
                else if (input == "vIC")
                {
                    input = reader.ReadString();
                    InstantiateVolumeServer();
                }
                else if (input == "vIS")
                {
                    ReadVolumeClient(reader);
                }
                else if (input == "vIR")
                {
                    int id = int.Parse(reader.ReadString());
                    float volume = float.Parse(reader.ReadString());
                    ChangeVolumeServer(id, volume);
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

        //--------------------------------------------------
        //IMAGES START
        //--------------------------------------------------

        public void SendImageXOR()
        {
            queueXOR = new BlockingCollection<byte[]>();
            stillSend = true;
            Console.WriteLine("send image xor");
            imageXORThreadSend = new Thread(() => SendImageXOR_T());
            imageXORThreadSend.Start();
        }

        public void StopImageXOR()
        {
            stillSend = false;
            if (imageXORThreadSend != null)
            {
                imageXORThreadSend.Abort();
            }
            if(imageXORThreadRec != null)
            {
                imageXORThreadRec.Abort();
            }
        }

        private void SendImageXOR_T()
        {
            Console.WriteLine("send image xor T");
            TcpClient imageClient = new TcpClient();
            IPAddress ownIPAddress = GetLocalIPAddress();
            TcpListener imageListener = new TcpListener(ownIPAddress, imageXORPort);
            imageListener.Start();
            writer.Write("i");
            Console.WriteLine("listening for connection reply");
            writer.Write(ownIPAddress.ToString());
            imageClient = imageListener.AcceptTcpClient();
            imageListener.Stop();

            BinaryWriter imageWriter = new BinaryWriter(imageClient.GetStream());

            while(stillSend == true)
            {
                byte[] data = null;

                try
                {
                    data = queueXOR.Take();
                }
                catch (InvalidOperationException) { }

                if(data != null)
                {
                    int datasize = data.Length;
                    imageWriter.Write(datasize.ToString());
                    imageWriter.Write(data, 0, datasize);
                }
            }
        }


        private void ReceiveImageXOR(string _targetIPAddress)
        {
            TcpClient imageClient = new TcpClient();
            imageClient.Connect(IPAddress.Parse(_targetIPAddress), imageXORPort);
            Console.WriteLine("connected image");
            BinaryReader imageReader = new BinaryReader(imageClient.GetStream());

            stillSend = true;

            int datasize;

            while (stillSend == true)
            {
                datasize = int.Parse(imageReader.ReadString());
                byte[] data = new byte[datasize];
                data = imageReader.ReadBytes(datasize);


                Console.WriteLine("image got " + data.Length.ToString());
                //function_PassByteArrayTo(data);
            }
        }

        //--------------------------------------------------
        //IMAGES END
        //--------------------------------------------------


        //--------------------------------------------------
        //VOLUME START
        //--------------------------------------------------

        public void InstantiateVolumeClient()
        {
            writer.Write("vIC");
            writer.Write("hi. placeholder for flags and options");
        }

        private void InstantiateVolumeServer()
        {
            volumeMaster = new VolumeMaster();
            writer.Write("vIS");

            writer.Write(volumeMaster.Sessions.Count.ToString());

            for (int i = 0; i < volumeMaster.Sessions.Count; i++)
            {
                if(volumeMaster.Sessions[i].DisplayName == null || volumeMaster.Sessions[i].DisplayName != "")
                {
                    writer.Write(volumeMaster.Sessions[i].DisplayName);
                }
                else
                {
                    writer.Write("null");
                }

                if (volumeMaster.Sessions[i].Process!= null)
                {
                    writer.Write(volumeMaster.Sessions[i].Process.ProcessName);
                }
                else
                {
                    writer.Write("null");
                }

                writer.Write(volumeMaster.Sessions[i].Mute.ToString());
                writer.Write(volumeMaster.Sessions[i].Volume.ToString());

                if (volumeMaster.Sessions[i].Process != null)
                {
                    writer.Write(volumeMaster.Sessions[i].Process.Id.ToString()); 
                }
                else
                {
                    writer.Write("0");
                }
            }
        }

        private void ReadVolumeClient(BinaryReader reader)
        {
            int procCount = int.Parse(reader.ReadString());
            VolumeArrayForAndroid = new VolumeAndroid[procCount];

            for (int i = 0; i < procCount; i++)
            {
                string displayName = reader.ReadString();
                string processName = reader.ReadString();
                bool mute = bool.Parse(reader.ReadString());
                double volume = double.Parse(reader.ReadString());
                int processID = int.Parse(reader.ReadString());
                VolumeArrayForAndroid[i] = new VolumeAndroid(displayName, processName, mute, volume, processID);
            }

            volumeReady = true;  
        }

        public void ChangeVolumeClient(int _id, float _volume)
        {
            writer.Write("vIR");
            writer.Write(_id.ToString());
            writer.Write(_volume.ToString());
        }

        private void ChangeVolumeServer(int _id, float _volume)
        {
            AudioSession session = volumeMaster.GetSessionByProcessID(_id);
            if (session != null)
            {
                session.Volume = _volume;
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

   public class VolumeAndroid
    {
        public string DisplayName { get; }
        public string ProcessName { get; }
        public bool Mute { get; }
        public double Volume { get; }
        public int ProcessID { get; }

        public VolumeAndroid(string _displayName, string _processName, bool _mute, double _volume, int _processID)
        {
            DisplayName = _displayName;
            ProcessName = _processName;
            Mute = _mute;
            Volume = _volume;
            ProcessID = _processID;
        }
    }

}


