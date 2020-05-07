using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TCPSender
{
    public class CommClientPC
    {
       
        Thread mainThread;

        //porty polaczen
        int commandPort = 50001;        //port komend/message
        int filePort = 50002;           //port dla plikow. zasada dzialania jak w FTP
        int imageXORPort = 50003;

        TcpListener listener;
        TcpClient client;               //klient tcp dla komend
        IPAddress cIP;                  //adres IP. Zalezy od tego czy instancja jest klientem czy serwerem
        Action<string> outputFunc;      //funkcja ktora jest wywolywana gdy pojawi sie message od hosta
        public bool IsConnected { get; internal set; }
        public Action<string> DisconnectAction { internal get; set; }//USTAWIAC DELEGATY!!!

        BinaryWriter writer;            //writer dla SendMessage, tutaj zeby nie tworzyc caly czas nowego. na porcie 50001
        int BUFFER_SIZE = 10000;                       //rozmiar bufora dla danych pliku w bajtach

        public string DownloadPath { get; set; } = "downloads"; //directory w ktorym beda zapisywane pliki. domyslnie relative/downloads

        Thread commandLineThread;

        Thread imageXORThreadSend;
        Thread imageXORThreadRec;
        bool stillSend = false;
        public BlockingCollection<byte[]> queueXOR { get; internal set; }

 
        VolumeMaster volumeMaster;          //audio coreapi


        //pmetrics
        HWUsage pMetricsClient = null;
        

        public CommClientPC(IPAddress _adresIP, Action<string> _funkcjaDoPrzekazaniaMessagy, Action<string> _connectedDelegate) //serwer = listen, client = connect
        {
            cIP = _adresIP;
            mainThread = new Thread(() => {
                bool success = Listen(_adresIP, _connectedDelegate);
                if (success)
                {
                    OpenCommandLine();
                    outputFunc = _funkcjaDoPrzekazaniaMessagy;
                    IsConnected = true;
                    SendMessage("Connected!"); 
                }
            });
            mainThread.Start();
        }

        private bool Listen(IPAddress _adresInterfejsuNasluchu, Action<string> _connectedDelegate) //W serwerze, nasluchuje na polaczenie
        {
            listener = new TcpListener(_adresInterfejsuNasluchu, commandPort);
            listener.Start();
            try
            {
                client = listener.AcceptTcpClient();
            }
            catch
            {
                Close();
                return false;
            }
            listener.Stop();
            _connectedDelegate("Connected!");
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
            int input = -1;
            //string input = null;
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
                    outputFunc(nextInput);
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
                else if (input == (int)ClientFlags.Volume_RequestConnection)    //instancja volume na PC
                {
                    nextInput = reader.ReadString();
                    InstantiateVolumeServer();
                }
                else if (input == (int)ClientFlags.Volume_ChangeVolume)    //zmiana volume na PC
                {
                    int type = reader.ReadInt32();  //typ zmienianej sesji
                    if (type == (int)ClientFlags.Volume_ProcessID)                    //po processID
                    {
                        int id = int.Parse(reader.ReadString());
                        float volume = float.Parse(reader.ReadString());
                        SetVolumeByID(id, volume);
                    }
                    if (type == (int)ClientFlags.Volume_ProcessName)                    //po grupie processname
                    {
                        string processName = reader.ReadString();
                        float volume = float.Parse(reader.ReadString());
                        SetVolumeByPN(processName, volume);
                    }
                    if (type == (int)ClientFlags.Volume_DisplayName)                   //po grupie displayNAme
                    {
                        string displayName = reader.ReadString();
                        float volume = float.Parse(reader.ReadString());
                        SetVolumeByDN(displayName, volume);
                    }
                }
                else if (input == (int)ClientFlags.Volume_ChangeMasterVolume)
                {
                    float volume = float.Parse(reader.ReadString());
                    SetMasterVolume(volume);
                }
                else if (input == (int)ClientFlags.PM_Instantiate)
                {
                    InstantiatePMServer();
                }
                else if (input == (int)ClientFlags.PM_Request)
                {
                    SendPMData();
                }
                else if (input == (int)ClientFlags.PM_Close)
                {
                    ClosePM();
                }
            }
            Close_Self();
        }


        private void SendFile_T(string _path)   //deprecated
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

        private void ReceiveFile(string _targetIPAddress)   //deprecated
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

        /// <summary>
        /// deprecated
        /// </summary>
        /// <param name="_path">
        /// sciezka do pliku
        /// </param>
        public void SendFile(string _path)  
        {
            Thread fileThread = new Thread(() => SendFile_T(_path));
            fileThread.Start();
        }

        //--------------------------------------------------
        //IMAGES START  (do przerobienia)
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


        private void InstantiateVolumeServer()      //uruchomienie VolumeMastera na PC
        {
            volumeMaster = new VolumeMaster();

            List<string> list = new List<string>(); //lista z nazwami procesow. uzywana do unikania duplikowania juz istniejacych sesji
            Icon icon;  //ikona do wyslana;
            Bitmap bitmap; //bitmapa ikony
            byte[] bitmapBytes;
            MemoryStream ms;

            writer.Write((int)ClientFlags.Volume_ServerReady);        //flaga oznaczajaca gotowosc do wysylania

            writer.Write(volumeMaster.MasterVolumeLevel.ToString());    //najpierw wysyla gloscnosc systemu. klient wie, ze to jest glosnosc systemowa i
            //nie potrzebuje reszty danych
            icon = SystemIcons.Shield;
            ms = new MemoryStream();
            bitmap = icon.ToBitmap();
            bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);        //wysylanie ikonki systemowej. placeholder, wlasciwa ikonak bedzie juz na androidzie
            bitmapBytes = ms.ToArray();

            writer.Write(bitmapBytes.Length);
            writer.Write(bitmapBytes, 0, bitmapBytes.Length);


            writer.Write(volumeMaster.Sessions.Count.ToString());   //wysyla ilosc sesji NA PC. Ilosc sesji jakie zaakceptuje android bedzie inna.

            

            for (int i = 0; i < volumeMaster.Sessions.Count; i++)   //po wszystkich sesjach
            {
                string displayName;
                string processName;
                string mute;
                string volume;
                string processID;

                //jesli istnieje displayname procesu. niektore procesy nie maja swojego displayname
                if (volumeMaster.Sessions[i].DisplayName != null && volumeMaster.Sessions[i].DisplayName != "")
                {
                    displayName = volumeMaster.Sessions[i].DisplayName;
                    //writer.Write(volumeMaster.Sessions[i].DisplayName);
                }
                else
                {
                    displayName = "null";
                }

                //jesli istnieje proces. uslugi systemu nie musza miec instancji procesu aby korzystac z audioapi. jednak wiekszosc sesji ma swoj proces.
                if (volumeMaster.Sessions[i].Process != null)
                {
                    processName = volumeMaster.Sessions[i].Process.ProcessName;
                    processID = volumeMaster.Sessions[i].Process.Id.ToString();
                    //writer.Write(volumeMaster.Sessions[i].Process.ProcessName);

                }
                else
                {
                    processName = "null";
                    processID = "0";
                }

                //mutestatus - czy sesja jest zmutowana
                mute = volumeMaster.Sessions[i].Mute.ToString();
                //volume
                volume = volumeMaster.Sessions[i].Volume.ToString();

                //poniewaz android nie wie ile PC bedzie wysylal sesji, wie tylko ile bedzie maksymalnie wysylal, wysyla sie tag skip gdy sesja jest pomijana
                //new lub skip

                //PC nie wysyla duplikatów sesji. jest to zwiazane z tym, ze jeden program moze miec wiele wywolan audioapi. sesje sa grupowane po processname
                if (!list.Contains(processName) || processName == "null")
                {
                    list.Add(processName);
                    writer.Write("new");
                    writer.Write(displayName);
                    writer.Write(processName);
                    writer.Write(mute);
                    writer.Write(volume);
                    writer.Write(processID);

                    ms = new MemoryStream();
                    icon = volumeMaster.Sessions[i].GetIcon32x32();
                    bitmap = icon.ToBitmap();
                    bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                    bitmapBytes = ms.ToArray();
                    

                    writer.Write(bitmapBytes.Length);
                    writer.Write(bitmapBytes, 0, bitmapBytes.Length);
                }
                else
                {
                    writer.Write("skip");
                }
            }


        }


        //zmienia volume jednego procesu oznaczonego przez ID
        private void SetVolumeByID(int _id, float _volume)
        {
            AudioSession session = volumeMaster.GetSessionByProcessID(_id);
            if (session != null)
            {
                session.Volume = _volume;
            }
        }

        //zmienia volume wszystkich sesji z jednym processname
        private void SetVolumeByPN(string _processName, float _volume)
        {
            List<AudioSession> list = volumeMaster.GetSessionByProcessName2(_processName);
            foreach (var session in list)
            {
                session.Volume = _volume;
            }
        }

        //zmienia volume wszystkich sesji z jednym displayname
        private void SetVolumeByDN(string _displayName, float _volume)
        {
            List<AudioSession> list = volumeMaster.GetSessionByDisplayName2(_displayName);
            foreach (var session in list)
            {
                session.Volume = _volume;
            }
        }

        private void SetMasterVolume(float _volume)
        {
            volumeMaster.MasterVolumeLevel = _volume;
        }

        //--------------------------------------------------
        //VOLUME END
        //--------------------------------------------------

        //--------------------------------------------------
        //UBA START
        //--------------------------------------------------

        public void SendSmallUBA(byte[] uba)
        {
            writer.Write(uba.Length.ToString());
            writer.Write(uba, 0, uba.Length);
        }

        //--------------------------------------------------
        //UBA END
        //--------------------------------------------------

        //--------------------------------------------------
        //METRICS START
        //--------------------------------------------------

        private void HWUsageDelegate(string we)
        {
            //placeholder
        }

        private void InstantiatePMServer()
        {
            //Thread externalThreadForConst = new Thread(() =>
            //{
            //    pMetricsClient = new HWUsage(HWUsageDelegate);
            //    while (pMetricsClient.ReadyFlag == false)
            //    {
            //        Thread.Sleep(25);
            //    }
            //    writer.Write((int)ClientFlags.PM_Ready);
            //    writer.Write("ready");
            //});
            //externalThreadForConst.Start();

            pMetricsClient = new HWUsage(HWUsageDelegate);
            while (pMetricsClient.ReadyFlag == false)
            {
                Thread.Sleep(25);
            }
            writer.Write((int)ClientFlags.PM_Ready);
            writer.Write("ready");
        }

        private void SendPMData()
        {
            pMetricsClient.Update();
            string outputwe = pMetricsClient.OutputString();
            writer.Write((int)ClientFlags.PM_Data);
            writer.Write(outputwe);
        }

        private void ClosePM()
        {
            if (pMetricsClient != null)
            {
                pMetricsClient.Close();
            }
        }

        //--------------------------------------------------
        //METRICS END
        //--------------------------------------------------

        public void Close()
        {
            if (listener != null)
            {
                try
                {
                    listener.Stop();
                }
                catch (Exception e)
                {
                    outputFunc(e.Message);
                }
            }

            if (IsConnected == true)
            {
                DisconnectAction("inner disconnect delegate");
                Close_Self();
            }
            else DisconnectAction("already disconnected!");
        }

        private void Close_Self()
        {
            try
            {
                listener.Stop();
                if(pMetricsClient != null)
                {
                    pMetricsClient.Close();
                }
                writer.Close();
                client.Close();
            }
            catch (Exception e)
            {
                outputFunc(e.Message);
            }
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
        PM_Close
    }
}
