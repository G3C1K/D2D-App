using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
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
    public class CommClientPC
    {
       
        Thread mainThread;
        bool started = false;

        //porty polaczen
        int commandPort = 50001;        //port komend/message
        int filePort = 50002;           //port dla plikow. zasada dzialania jak w FTP
        int imageXORPort = 50003;

        TcpListener listener;
        TcpClient client;               //klient tcp dla komend
        IPAddress cIP;                  //adres IP. Zalezy od tego czy instancja jest klientem czy serwerem
        Action<string> DebugLogAction;      //funkcja ktora jest wywolywana gdy pojawi sie message od hosta
        public bool IsConnected { get; internal set; }
        public Action<string> DisconnectAction { internal get; set; }//USTAWIAC DELEGATY!!!
        public Action<string> ConnectedAction { internal get; set; }
        public Action<string> DeviceNameAction { internal get; set; }
        public Action<List<string>> FileInstAction { internal get; set; }   //wypelnia liste plikow plikami
        public Action<string> FileRemoveAction { internal get; set; }   //usuwa plik z listy

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
        bool sendPMetrics = false;

        //pass
        public string Password { get; set; }

        //filetransfer
        public List<string> FileList { get; set; }

        public CommClientPC(Action<string> _funkcjaDoPrzekazaniaMessagy, Action<string> _connectedDelegate) //serwer = listen, client = connect
        {
            DebugLogAction = _funkcjaDoPrzekazaniaMessagy;
            ConnectedAction = _connectedDelegate;
        }

        public void Start(IPAddress ip)
        {
            CheckDelegates();
            if(started == false)
            {
                started = true;

                mainThread = new Thread(() => {
                    bool success = Listen(ip);
                    if (success)
                    {
                        writer = new BinaryWriter(client.GetStream());

                        OpenPasswordLine();
                        OpenCommandLine();
                        IsConnected = true;
                        
                        ConnectedAction("Connected!");
                        SendMessage("Connected!");
                    }
                });
                mainThread.Start();
            }
        }

        private bool Listen(IPAddress _adresInterfejsuNasluchu) //W serwerze, nasluchuje na polaczenie
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
                    DebugLogAction(nextInput);
                }
                else if (input == (int)ClientFlags.File)
                {
                    nextInput = reader.ReadString();
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
                    if(pMetricsClient == null)
                    {
                        InstantiatePMServer();
                    }
                    else
                    {
                        sendPMetrics = true;
                        writer.Write((int)ClientFlags.PM_Ready);
                        writer.Write("ready");

                    }
                }
                else if (input == (int)ClientFlags.PM_Request)
                {
                    if(sendPMetrics == true)
                    {
                        SendPMData();
                    }
                }
                else if (input == (int)ClientFlags.PM_Close)
                {
                    ClosePM();
                }
                else if (input == (int)ClientFlags.Config_DeviceName)
                {
                    nextInput = reader.ReadString();
                    DeviceNameAction?.Invoke(nextInput);
                }
                else if (input == (int)ClientFlags.FT_Instantiate)
                {
                    InstantiateTrasferServer();
                }
                else if (input == (int)ClientFlags.FT_RemoveFileFromList)
                {
                    nextInput = reader.ReadString();
                    RemoveFileInternal(nextInput);
                }
                else if(input == (int)ClientFlags.FT_DownloadFile)
                {
                    nextInput = reader.ReadString();
                    Thread sendFileThread = new Thread(() => SendFileV2(nextInput));
                    sendFileThread.Start();
                }
                else if (input == (int)ClientFlags.Numpad)
                {
                    nextInput = reader.ReadString();
                    InputKey(nextInput);
                }

            }
            Close_Self();
        }

        public void OpenPasswordLine()
        {
            BinaryReader passwordReader = new BinaryReader(client.GetStream());
            string input = "asdf";
            bool continueLoop = true;

            while(continueLoop == true)
            {
                try
                {
                    input = passwordReader.ReadString();
                }
                catch
                {
                    Close();
                    return;
                }

                if(input == Password)
                {
                    writer.Write((int)ClientFlags.Password_Correct);
                    continueLoop = false;
                }
                else
                {
                    writer.Write((int)ClientFlags.Password_Incorrect);
                }
            }
        }


        public void SendMessage(string _message)    //Wysyla message (type 1) do odbiorcy
        {
            writer.Write((int)ClientFlags.Command);
            writer.Write(_message);
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
            pMetricsClient = new HWUsage(HWUsageDelegate);  //pmetrics jest uruchamiane na jakims? watku. do poprawienia
            while (pMetricsClient.ReadyFlag == false)
            {
                Thread.Sleep(25);
            }

            try
            {
                writer.Write((int)ClientFlags.PM_Ready);    //jesli nastapi disconnect przed wyslaniem flagi ready, program sie scrashuje.
                writer.Write("ready");
                sendPMetrics = true;
            }
            catch (Exception e)
            {
                Close();
            }
        }

        private void SendPMData()
        {
            pMetricsClient.Update();
            string outputwe = pMetricsClient.OutputStringV2();

            try
            {
                writer.Write((int)ClientFlags.PM_Data);
                writer.Write(outputwe);
            }
            catch (Exception e)
            {
                DebugLogAction("tried to send PMData to nonexistent stream");
            }
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

        //--------------------------------------------------
        //FILEV2 START
        //--------------------------------------------------

        private void InstantiateTrasferServer()
        {
            FileList = new List<string>();
            FileInstAction(FileList);

            writer.Write((int)ClientFlags.FT_Ready);

            int count = FileList.Count;
            writer.Write(count);
            foreach (string file in FileList)
            {
                writer.Write(file);
            }
        }

        private void RemoveFileInternal(string file)
        {
            FileList.Remove(file);
            FileRemoveAction(file);
        }

        private void SendFileV2(string filePath)
        {
            if (FileList.Contains(filePath))
            {
                DebugLogAction("attempting to send file named: " + filePath);

                TcpClient fileClient = new TcpClient();
                IPAddress ownIPAddress = GetLocalIPAddress();
                TcpListener fileListener = new TcpListener(ownIPAddress, filePort);
                fileListener.Start();

                writer.Write((int)ClientFlags.FT_SendFile);
                writer.Write(ownIPAddress.ToString());
                fileClient = fileListener.AcceptTcpClient();
                fileListener.Stop();

                BinaryWriter fileWriter = new BinaryWriter(fileClient.GetStream());
                FileInfo fileInfo = new FileInfo(filePath);
                string fileName = fileInfo.Name;
                int fileSize = (int)fileInfo.Length;


                FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                int packetCount = (int)Math.Floor((double)(fileSize / BUFFER_SIZE));
                int reszta = (fileSize - packetCount * BUFFER_SIZE);
                byte[] buffer = new byte[BUFFER_SIZE];
                byte[] lastPacket = new byte[reszta];

                //wlasciwe wysylanie
                fileWriter.Write(filePath);
                fileWriter.Write(fileName);
                fileWriter.Write((int)fileSize);
                fileWriter.Write(packetCount);
                fileWriter.Write(reszta);

                int debugSpace = packetCount / 10;
                for (int i = 0; i < packetCount; i++)
                {
                    try
                    {
                        if (i % debugSpace == 0)
                        {
                            DebugLogAction("sending packet " + i + "/" + packetCount);
                        }
                    }
                    catch { }

                    fileStream.Read(buffer, 0, BUFFER_SIZE);
                    fileWriter.Write(buffer, 0, BUFFER_SIZE);
                }
                DebugLogAction("sending last packet...");
                fileStream.Read(lastPacket, 0, lastPacket.Length);
                fileWriter.Write(lastPacket, 0, lastPacket.Length);
                //fileWriter.Write(lastPacket);

                //konczenie
                fileWriter.Close();
                fileStream.Close();
                fileClient.Close();
            }
            else
            {
                DebugLogAction("attempted to download file that does not exist");
            }
        }


        //--------------------------------------------------
        //FILEV2 END
        //--------------------------------------------------

        //--------------------------------------------------
        //NUMPAD START 
        //--------------------------------------------------
        public void InputKey(string klawisz)
        {

            //DebugLogAction(klawisz);

            byte a = InputKeyClass.VK_UNDEFINED;

            switch (klawisz)
            {
                case "1":
                    a = InputKeyClass.VK_NUMPAD1;
                    break;
                case "2":
                    a = InputKeyClass.VK_NUMPAD2;
                    break;
                case "3":
                    a = InputKeyClass.VK_NUMPAD3;
                    break;
                case "4":
                    a = InputKeyClass.VK_NUMPAD4;
                    break;
                case "5":
                    a = InputKeyClass.VK_NUMPAD5;
                    break;
                case "6":
                    a = InputKeyClass.VK_NUMPAD6;
                    break;
                case "7":
                    a = InputKeyClass.VK_NUMPAD7;
                    break;
                case "8":
                    a = InputKeyClass.VK_NUMPAD8;
                    break;
                case "9":
                    a = InputKeyClass.VK_NUMPAD9;
                    break;
                case "0":
                    a = InputKeyClass.VK_NUMPAD0;
                    break;
                case "ADD":
                    a = InputKeyClass.VK_ADD;
                    break;
                case "SUB":
                    a = InputKeyClass.VK_SUBSTRACT;
                    break;
                case "MUL":
                    a = InputKeyClass.VK_MULTIPLY;
                    break;
                case "DIV":
                    a = InputKeyClass.VK_DIVIDE;
                    break;
                case "DEC":
                    a = InputKeyClass.VK_DECIMAL;
                    break;
                case "EQ":
                    a = InputKeyClass.VK_RETURN;
                    break;
                case "PP":
                    a = InputKeyClass.VK_MEDIA_PLAY_PAUSE;
                    break;
                case "NEXT":
                    a = InputKeyClass.VK_NEXT_TRACK;
                    break;
                case "PREV":
                    a = InputKeyClass.VK_PREV_TRACK;
                    break;
                case "NUM":
                    a = InputKeyClass.VK_BACK;
                    break;

            }

            InputKeyClass.InputKeyFromByte(a);
            
        }
        //--------------------------------------------------
        //NUMPAD END 
        //--------------------------------------------------




        public void CheckDelegates()
        {
            bool allDelegatesSet = true;

            
            if(ConnectedAction == null)
            {
                DebugLogAction("ConnectedDelegate missing");
                allDelegatesSet = false;
            }
            if (DeviceNameAction == null)
            {
                DebugLogAction("DeviceNameAction missing");
                allDelegatesSet = false;
            }
            if(DisconnectAction == null)
            {
                DebugLogAction("DisconnectAction missing");
                allDelegatesSet = false;
            }
            if(FileInstAction == null)
            {
                DebugLogAction("FileInstAction missing");
                allDelegatesSet = false;
            }

            if(allDelegatesSet == true)
            {
                //DebugLogAction("All delegates are set and should be working correctly.");
            }
        }

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
                    DebugLogAction(e.Message);
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
            sendPMetrics = false;
            try
            {
                listener.Stop();
                //outputFunc("listener stop");
                if(pMetricsClient != null)
                {
                    pMetricsClient.Close();
                    //outputFunc("pmetrics close");
                }
                writer.Close();
                //outputFunc("writer close");

                client.Close();
                //outputFunc("client close");

            }
            catch (Exception e)
            {
                DebugLogAction(e.Message);
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

        static class InputKeyClass
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

            public const byte VK_NUMPAD0 = 0x60;
            public const byte VK_NUMPAD1 = 0x61;
            public const byte VK_NUMPAD2 = 0x62;
            public const byte VK_NUMPAD3 = 0x63;
            public const byte VK_NUMPAD4 = 0x64;
            public const byte VK_NUMPAD5 = 0x65;
            public const byte VK_NUMPAD6 = 0x66;
            public const byte VK_NUMPAD7 = 0x67;
            public const byte VK_NUMPAD8 = 0x68;
            public const byte VK_NUMPAD9 = 0x69;
            public const byte VK_MULTIPLY = 0x6A;
            public const byte VK_ADD = 0x6B;
            public const byte VK_SEPARATOR = 0x6C;
            public const byte VK_SUBSTRACT = 0x6D;
            public const byte VK_DECIMAL = 0x6E;
            public const byte VK_DIVIDE = 0x6F;
            public const byte VK_RETURN = 0x0D;
            public const byte VK_UNDEFINED = 0x07;
            public const byte VK_MEDIA_PLAY_PAUSE = 0xB3;
            public const byte VK_NEXT_TRACK = 0xB0;
            public const byte VK_PREV_TRACK = 0xB1;
            public const byte VK_BACK = 0x08;

            public static void InputKeyFromByte(byte input)
            {
                keybd_event(input, MapVirtualKey(input, 0), KEYEVENTF_EXTENDEDKEY, 0);
                keybd_event(input, MapVirtualKey(input, 0), KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP, 0);
            }


            //public static void VolumeUp()
            //{
            //    keybd_event(VK_VOLUME_UP, MapVirtualKey(VK_VOLUME_UP, 0), KEYEVENTF_EXTENDEDKEY, 0);
            //    keybd_event(VK_VOLUME_UP, MapVirtualKey(VK_VOLUME_UP, 0), KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP, 0);
            //}

            //public static void VolumeDown()
            //{
            //    keybd_event(VK_VOLUME_DOWN, MapVirtualKey(VK_VOLUME_DOWN, 0), KEYEVENTF_EXTENDEDKEY, 0);
            //    keybd_event(VK_VOLUME_DOWN, MapVirtualKey(VK_VOLUME_DOWN, 0), KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP, 0);
            //}

            //public static void Mute()
            //{
            //    keybd_event(VK_VOLUME_MUTE, MapVirtualKey(VK_VOLUME_MUTE, 0), KEYEVENTF_EXTENDEDKEY, 0);
            //    keybd_event(VK_VOLUME_MUTE, MapVirtualKey(VK_VOLUME_MUTE, 0), KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP, 0);
            //}
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
        FT_SendFile,
        Numpad
    }
}
