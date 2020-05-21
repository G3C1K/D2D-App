using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Android.App;
using Android.Content;
using Android.Content.Res;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;

namespace D2DUIv3
{
    public class CommClientAndroid
    {
        Thread mainThread;

        //porty polaczen
        int commandPort = 50001;        //port komend/message
        int filePort = 50002;           //port dla plikow. zasada dzialania jak w FTP
        int imageXORPort = 50003;
        int ubaPort = 50004;
        public bool IsConnected { get; internal set; }

        TcpClient client;               //klient tcp dla komend
        IPAddress cIP;                  //adres IP. Zalezy od tego czy instancja jest klientem czy serwerem
        string deviceName = "unknown device";

        //delegaty sa potrzebne aby przekazywac infromacje miedzy watkami
        //= (x) => { }; - po to aby ustawic domyslny delegat ktory nic nie robi oprocz unikania wyjatku
        public Action<string> DebugLogAction { internal get; set; } = (x) => { };     //funkcja ktora jest wywolywana gdy pojawi sie message od hosta       
        public Action<string> DisconnectAction { internal get; set; } = (x) => { };//USTAWIAC DELEGATY
        public Action<string> ConnectedAction { internal get; set; } = (x) => { };
        public Action<string> OpenPasswordInputDialogAction { internal get; set; } = (x) => { };

        BinaryWriter writer;            //writer dla SendMessage, tutaj zeby nie tworzyc caly czas nowego. na porcie 50001
        int BUFFER_SIZE = 10000;                       //rozmiar bufora dla danych pliku w bajtach

        public string DownloadPath { get; set; } = "downloads"; //directory w ktorym beda zapisywane pliki. domyslnie relative/downloads

        Thread commandLineThread;

        Thread imageXORThreadSend;
        Thread imageXORThreadRec;
        bool stillSend = false;
        public BlockingCollection<byte[]> queueXOR { get; internal set; }


        public VolumeAndroid[] VolumeArrayForAndroid { get; internal set; } //android - tablica instancji w formie tekstowej
        public List<VolumeAndroid> VolumeListForAndroid { get; internal set; }     //android - lista instancji w formie testowej
        public bool volumeReady = false;

        public Action<string> PMReadyAction { internal get; set; }
        public Action<string> PMDataReceivedAction { internal get; set; }



        //pliki
        List<string> fileList = new List<string>();

        public Action<List<string>> FileListReceivedAction { internal get; set; }

        public Action<string, string, string, int> FileReceivedAction { internal get; set; }
        public Action<string> BrokenFileAction { internal get; set; }


        public CommClientAndroid(IPAddress _adresIP, Action<string> _connectedDelegate) //serwer = listen, client = connect
        {
            cIP = _adresIP;
            ConnectedAction = _connectedDelegate;
            //tworzenie klienta
            client = new TcpClient();
            Thread connectThread = new Thread(() =>
            {
                Connect(_adresIP);  //connect 
                writer = new BinaryWriter(client.GetStream());

                OpenPasswordLine();

                OpenCommandLine();
                IsConnected = true;
                ConnectedAction("Connected!");
            });
            connectThread.Start();
        }


        private bool Connect(IPAddress _adresIPHosta)       //Ustanawia polaczenie
        {
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
            commandLineThread = new Thread(() => ListenForCommands(DebugLogAction));
            commandLineThread.Start();
        }

        private void ListenForCommands(Action<string> _outputFunc)            //Uruchamia watek nasluchiwania na wiadomosci. Do przerobienia z uwzglednieniem Action
        {
            BinaryReader reader = new BinaryReader(client.GetStream());
            int input = -1;
            string nextInput = null;
            while (input != (int)ClientFlags.Close)
            {
                try
                {
                    input = reader.ReadInt32();
                }
                catch
                {
                    Close();
                    return;
                }

                if (input == (int)ClientFlags.Command)
                {
                    nextInput = reader.ReadString();
                    DebugLogAction(nextInput);
                }
                else if (input == (int)ClientFlags.File)
                {
                    nextInput = reader.ReadString();
                    Thread rec = new Thread(() => ReceiveFile(nextInput));
                    rec.Start();
                }
                else if (input == (int)ClientFlags.XOR)
                {
                    //input = reader.ReadString();
                    //imageXORThreadRec = new Thread(() => ReceiveImageXOR(input));
                    //imageXORThreadRec.Start();
                }
                else if (input == (int)ClientFlags.Volume_ServerReady)    //instancja volume na androidzie
                {
                    ReadVolumeClient(reader);
                } 
                else if (input == (int)ClientFlags.ByteArray)
                {
                    //temporaryImageHolder = ReadSmallUBA(reader);
                }
                else if (input == (int)ClientFlags.PM_Ready)
                {
                    nextInput = reader.ReadString();
                    PMReadyAction(nextInput);
                }
                else if (input == (int)ClientFlags.PM_Data)
                {
                    nextInput = reader.ReadString();
                    PMDataReceivedAction(nextInput);
                }
                else if(input == (int)ClientFlags.FT_Ready)
                {
                    ReceiveFilesInfo(reader);
                }
                else if(input == (int)ClientFlags.FT_SendFile)
                {
                    nextInput = reader.ReadString();
                    Thread recThread = new Thread(() => ReceiveFileV2(nextInput));
                    recThread.Start();
                }
            }
            Close_Self();
        }

        private void OpenPasswordLine()
        {
            OpenPasswordInputDialogAction("Password required");
            BinaryReader passwordReader = new BinaryReader(client.GetStream());

            bool continueLoop = true;
            int input;
            while(continueLoop == true)
            {
                try
                {
                    input = passwordReader.ReadInt32();
                }
                catch
                {
                    Close();
                    return;
                }

                if(input == (int)ClientFlags.Password_Correct)
                {
                    //wylacz okno delegat
                    continueLoop = false;
                }
                else if(input == (int)ClientFlags.Password_Incorrect)
                {
                    OpenPasswordInputDialogAction("Incorrect password");

                }

            }
        }

        public void SendPassword(string password)
        {
            writer.Write(password);
        }


        private void SendFile_T(string _path)
        {
            //ustanawianie polaczenia na porcie 50002
            TcpClient fileClient = new TcpClient();
            IPAddress ownIPAddress = GetLocalIPAddress();
            TcpListener fileListener = new TcpListener(ownIPAddress, filePort);
            fileListener.Start();
            writer.Write((int)ClientFlags.File);
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
            writer.Write((int)ClientFlags.Command);
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

        //public void SendImageXOR()
        //{
        //    queueXOR = new BlockingCollection<byte[]>();
        //    stillSend = true;
        //    Console.WriteLine("send image xor");
        //    imageXORThreadSend = new Thread(() => SendImageXOR_T());
        //    imageXORThreadSend.Start();
        //}

        //public void StopImageXOR()
        //{
        //    stillSend = false;
        //    if (imageXORThreadSend != null)
        //    {
        //        imageXORThreadSend.Abort();
        //    }
        //    if (imageXORThreadRec != null)
        //    {
        //        imageXORThreadRec.Abort();
        //    }
        //}

        //private void SendImageXOR_T()
        //{
        //    Console.WriteLine("send image xor T");
        //    TcpClient imageClient = new TcpClient();
        //    IPAddress ownIPAddress = GetLocalIPAddress();
        //    TcpListener imageListener = new TcpListener(ownIPAddress, imageXORPort);
        //    imageListener.Start();
        //    writer.Write("i");
        //    Console.WriteLine("listening for connection reply");
        //    writer.Write(ownIPAddress.ToString());
        //    imageClient = imageListener.AcceptTcpClient();
        //    imageListener.Stop();

        //    BinaryWriter imageWriter = new BinaryWriter(imageClient.GetStream());

        //    while (stillSend == true)
        //    {
        //        byte[] data = null;

        //        try
        //        {
        //            data = queueXOR.Take();
        //        }
        //        catch (InvalidOperationException) { }

        //        if (data != null)
        //        {
        //            int datasize = data.Length;
        //            imageWriter.Write(datasize.ToString());
        //            imageWriter.Write(data, 0, datasize);
        //        }
        //    }
        //}


        //private void ReceiveImageXOR(string _targetIPAddress)
        //{
        //    TcpClient imageClient = new TcpClient();
        //    imageClient.Connect(IPAddress.Parse(_targetIPAddress), imageXORPort);
        //    Console.WriteLine("connected image");
        //    BinaryReader imageReader = new BinaryReader(imageClient.GetStream());

        //    stillSend = true;

        //    int datasize;

        //    while (stillSend == true)
        //    {
        //        datasize = int.Parse(imageReader.ReadString());
        //        byte[] data = new byte[datasize];
        //        data = imageReader.ReadBytes(datasize);


        //        Console.WriteLine("image got " + data.Length.ToString());
        //        //function_PassByteArrayTo(data);
        //    }
        //}

        //--------------------------------------------------
        //IMAGES END
        //--------------------------------------------------


        //--------------------------------------------------
        //VOLUME START
        //--------------------------------------------------

        public void InstantiateVolumeClient()                   //android rozpoczyna komunikacje uzywajac InstantiateVolumeClient. nastepnie wywolywane jest InstantiateVolumeServer
        {
            writer.Write((int)ClientFlags.Volume_RequestConnection);
            writer.Write("hi. placeholder for flags and options");
        }       

        //po fladze vIS
        private void ReadVolumeClient(BinaryReader reader)
        {
            //OpenPasswordInputDialogAction("open up");

            VolumeListForAndroid = new List<VolumeAndroid>();

            float systemVolume = float.Parse(reader.ReadString());
            int systemVolumeIconLen = reader.ReadInt32();
            byte[] systemVolumeIconBytes = reader.ReadBytes(systemVolumeIconLen);
            VolumeListForAndroid.Add(new VolumeAndroid("System Volume", "System Volume", false, systemVolume, int.MaxValue, systemVolumeIconBytes));

            int procCount = int.Parse(reader.ReadString());


            for (int i = 0; i < procCount; i++)
            {
                string status = reader.ReadString();
                if (status == "new")
                {
                    string displayName = reader.ReadString();
                    string processName = reader.ReadString();
                    bool mute = bool.Parse(reader.ReadString());
                    double volume = double.Parse(reader.ReadString());
                    int processID = int.Parse(reader.ReadString());
                    int bitmapBytesLength = reader.ReadInt32();
                    byte[] bitmapBytes = reader.ReadBytes(bitmapBytesLength);
                    VolumeListForAndroid.Add(new VolumeAndroid(displayName, processName, mute, volume, processID, bitmapBytes));
                }
            }

            volumeReady = true;  //flaga do czekania az wszystkie sesje zostana zaladowane.
        }

        //volume na androidzie sa grupowane
        public void ChangeVolumeClient(int _id, float _volume)
        {
            writer.Write((int)ClientFlags.Volume_ChangeVolume);
            writer.Write((int)ClientFlags.Volume_ProcessID);
            writer.Write(_id.ToString());
            writer.Write(_volume.ToString());
        }

        public void ChangeVolumeClientPN(string _processName, float _volume)
        {
            writer.Write((int)ClientFlags.Volume_ChangeVolume);
            writer.Write((int)ClientFlags.Volume_ProcessName);
            writer.Write(_processName);
            writer.Write(_volume.ToString());
        }

        public void ChangeVolumeClientDN(string _displayName, float _volume)
        {
            writer.Write((int)ClientFlags.Volume_ChangeVolume);
            writer.Write((int)ClientFlags.Volume_DisplayName);
            writer.Write(_displayName);
            writer.Write(_volume.ToString());
        }

        public void ChangeVolumeMaster(float _volume)
        {
            writer.Write((int)ClientFlags.Volume_ChangeMasterVolume);
            writer.Write(_volume.ToString());
        }


        //--------------------------------------------------
        //VOLUME END
        //--------------------------------------------------

        //--------------------------------------------------
        //UBA START
        //--------------------------------------------------

        private byte[] ReadSmallUBA(BinaryReader reader)
        {
            int length = int.Parse(reader.ReadString());
            byte[] ret;
            ret = reader.ReadBytes(length);
            return ret;
        }

        //--------------------------------------------------
        //UBA END
        //--------------------------------------------------

        //--------------------------------------------------
        //METRICS START
        //--------------------------------------------------

        public void InstantiatePMClient()
        {
            writer.Write((int)ClientFlags.PM_Instantiate);
        }

        public void AskForPM()
        {
            try
            {
                writer.Write((int)ClientFlags.PM_Request);
            }
            catch (Exception e)
            {
                Close();
            }
        }

        public void ClosePM()
        {
            writer.Write((int)ClientFlags.PM_Close);
        }

        //--------------------------------------------------
        //METRICS END
        //--------------------------------------------------

        //--------------------------------------------------
        //FILEV2 START
        //--------------------------------------------------

        public void InstantiateTransfer()
        {
            writer.Write((int)ClientFlags.FT_Instantiate);
        }

        private void ReceiveFilesInfo(BinaryReader reader) //odpala sie po fladze ready
        {
            fileList.Clear();       //czysci liste plikow
            int count = reader.ReadInt32();
            for (int i = 0; i < count; i++) //czyta liczbe plikow, dodaje je po kolei do listy
            {
                string input = reader.ReadString();
                fileList.Add(input);
            }
            FileListReceivedAction(fileList);   //konczy delegatem ktory przekazuje zawartosc listy do activity, nastepnie activity umieszcze je jako textview na ekranie
        }

        public void RemoveFile(string file)
        {
            writer.Write((int)ClientFlags.FT_RemoveFileFromList);
            writer.Write(file);
        }

        public void DownloadFile(string file)
        {
            writer.Write((int)ClientFlags.FT_DownloadFile);
            writer.Write(file);
        }

        private void ReceiveFileV2(string ip)
        {
            TcpClient fileClient = new TcpClient();
            fileClient.Connect(IPAddress.Parse(ip), filePort);

            BinaryReader fileReader = new BinaryReader(fileClient.GetStream());
            string fileName = fileReader.ReadString();
            int fileLength = fileReader.ReadInt32();
            int packetCount = fileReader.ReadInt32();
            int reszta = fileReader.ReadInt32();
            byte[] buffer = new byte[BUFFER_SIZE];
            byte[] lastPacket = new byte[reszta];

            string fullFilePath = System.IO.Path.Combine(DownloadPath, fileName);
            FileStream fileStream = File.OpenWrite(fullFilePath);

            for(int i = 0; i<packetCount; i++)
            {
                buffer = fileReader.ReadBytes(BUFFER_SIZE);
                fileStream.Write(buffer, 0, BUFFER_SIZE);
            }
            lastPacket = fileReader.ReadBytes(lastPacket.Length);
            if(lastPacket.Length != reszta)
            {
                BrokenFileAction("Corrupted file!");
            }
            else
            {
                BrokenFileAction("File was downloaded correctly...");
            }
            fileStream.Write(lastPacket, 0, lastPacket.Length);


            fileReader.Close();
            fileStream.Close();
            fileClient.Close();

            FileReceivedAction(fileName, fileName, fullFilePath, fileLength);
        }


        //--------------------------------------------------
        //FILEV2 END
        //--------------------------------------------------

        public void SendDeviceName(string name)
        {
            writer.Write((int)ClientFlags.Config_DeviceName);
            writer.Write(name);
        }

        public void Close()
        {
            // writer.Write("x");
            if(IsConnected == true)
            {
                DisconnectAction?.Invoke("Disconnect happened");

                Close_Self();
            }
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
        public byte[] IconByteArray { get; }

        public VolumeAndroid(string _displayName, string _processName, bool _mute, double _volume, int _processID, byte[] _iconByteArray)
        {
            DisplayName = _displayName;
            ProcessName = _processName;
            Mute = _mute;
            Volume = _volume;
            ProcessID = _processID;
            IconByteArray = new byte[_iconByteArray.Length];
            Array.Copy(_iconByteArray, IconByteArray, _iconByteArray.Length);
        }
    }

    public enum ClientFlags
    {
        Close,
        Command,
        File,
        XOR,
        Volume_RequestConnection,
        Volume_ServerReady,
        Volume_ChangeVolume,
        Volume_ChangeMasterVolume,
        Volume_ProcessID,
        Volume_ProcessName,
        Volume_DisplayName,
        ByteArray,
        PM_Instantiate,
        PM_Ready,
        PM_Request,
        PM_Data,
        PM_Close,
        Config_DeviceName,
        Password_Correct,
        Password_Incorrect,
        FT_Instantiate,
        FT_Ready,
        FT_RemoveFileFromList,
        FT_DownloadFile,
        FT_SendFile
    }

    public static class ClientUtilities
    {
        public static bool IsValidIPV4Address(string ipString)
        {
            IPAddress outIP;

            //czy sparsuje
            if(IPAddress.TryParse(ipString, out outIP) == false)
            {
                //jesli nie spelnia wymagan
                return false;
            }

            //czy ma 4 kropki
            int count = 0;
            foreach(char c in ipString)
            {
                if (c == '.') count++;
            }
            if(count != 3)
            {
                return false;
            }

            //zbanowane adresy
            if(ipString == "127.0.0.1" || ipString == "0.0.0.0" || ipString == "255.255.255.255")
            {
                return false;
            }

            return true;
        }

        public static int ConvertPixelsToDP(float px, Context context)
        {
            Resources resources = context.Resources;
            DisplayMetrics metrics = resources.DisplayMetrics;
            var smth = (float)metrics.DensityDpi / 160f;
            float dp = px / smth;
            return (int)dp;
        }
    }
}