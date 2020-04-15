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
        int ubaPort = 50004;

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


        VolumeMaster volumeMaster;          //audio coreapi
        public VolumeAndroid[] VolumeArrayForAndroid { get; internal set; } //android - tablica instancji w formie tekstowej
        public List<VolumeAndroid> VolumeListForAndroid { get; internal set; }     //android - lista instancji w formie testowej
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
                else if (input == "vIC")    //instancja volume na PC
                {
                    input = reader.ReadString();
                    InstantiateVolumeServer();
                }
                else if (input == "vIS")    //instancja volume na androidzie
                {
                    ReadVolumeClient(reader);
                }
                else if (input == "vIR")    //zmiana volume na PC
                {
                    string type = reader.ReadString();  //typ zmienianej sesji
                    if(type == "id")                    //po processID
                    {
                        int id = int.Parse(reader.ReadString());
                        float volume = float.Parse(reader.ReadString());
                        ChangeVolumeServer(id, volume);
                    }
                    if(type == "pn")                    //po grupie processname
                    {
                        string processName = reader.ReadString();
                        float volume = float.Parse(reader.ReadString());
                        ChangeVolumeServerPN(processName, volume);
                    }
                    if (type == "dn")                   //po grupie displayNAme
                    {
                        string displayName = reader.ReadString();
                        float volume = float.Parse(reader.ReadString());
                        ChangeVolumeServerDN(displayName, volume);
                    }
                }
                else if (input == "ubaS")
                {

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

        public void InstantiateVolumeClient()                   //android rozpoczyna komunikacje uzywajac InstantiateVolumeClient. nastepnie wywolywane jest InstantiateVolumeServer
        {
            writer.Write("vIC");
            writer.Write("hi. placeholder for flags and options");      
        }

        private void InstantiateVolumeServer()      //uruchomienie VolumeMastera na PC
        {
            volumeMaster = new VolumeMaster();
            writer.Write("vIS");        //flaga oznaczajaca gotowosc do wysylania

            writer.Write(volumeMaster.Sessions.Count.ToString());   //wysyla ilosc sesji NA PC. Ilosc sesji jakie zaakceptuje android bedzie inna.

            List<string> list = new List<string>(); //lista z nazwami procesow. uzywana do unikania duplikowania juz istniejacych sesji

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
                }
                else
                {
                    writer.Write("skip");
                }

                
            }
        }

        //po fladze vIS
        private void ReadVolumeClient(BinaryReader reader)
        {
            int procCount = int.Parse(reader.ReadString());
            //VolumeArrayForAndroid = new VolumeAndroid[procCount];
            VolumeListForAndroid = new List<VolumeAndroid>(); 
            for (int i = 0; i < procCount; i++)
            {
                string status = reader.ReadString();
                if(status == "new")
                {
                    string displayName = reader.ReadString();
                    string processName = reader.ReadString();
                    bool mute = bool.Parse(reader.ReadString());
                    double volume = double.Parse(reader.ReadString());
                    int processID = int.Parse(reader.ReadString());
                    VolumeListForAndroid.Add(new VolumeAndroid(displayName, processName, mute, volume, processID));
                }
            }

            volumeReady = true;  //flaga do czekania az wszystkie sesje zostana zaladowane.
        }

        //volume na androidzie sa grupowane
        public void ChangeVolumeClient(int _id, float _volume)
        {
            writer.Write("vIR");
            writer.Write("id");
            writer.Write(_id.ToString());
            writer.Write(_volume.ToString());
        }

        public void ChangeVolumeClientPN(string _processName, float _volume)
        {
            writer.Write("vIR");
            writer.Write("pn");
            writer.Write(_processName);
            writer.Write(_volume.ToString());
        }

        public void ChangeVolumeClientDN(string _displayName, float _volume)
        {
            writer.Write("vIR");
            writer.Write("dn");
            writer.Write(_displayName);
            writer.Write(_volume.ToString());
        }

        //zmienia volume jednego procesu oznaczonego przez ID
        private void ChangeVolumeServer(int _id, float _volume)
        {
            AudioSession session = volumeMaster.GetSessionByProcessID(_id);
            if (session != null)
            {
                session.Volume = _volume;
            }
        }

        //zmienia volume wszystkich sesji z jednym processname
        private void ChangeVolumeServerPN(string _processName, float _volume)
        {
            List<AudioSession> list = volumeMaster.GetSessionByProcessName2(_processName);
            foreach (var session in list)
            {
                session.Volume = _volume;
            }
        }

        //zmienia volume wszystkich sesji z jednym displayname
        private void ChangeVolumeServerDN(string _displayName, float _volume)
        {
            List<AudioSession> list = volumeMaster.GetSessionByDisplayName2(_displayName);
            foreach (var session in list)
            {
                session.Volume = _volume;
            }
        }

        //--------------------------------------------------
        //VOLUME END
        //--------------------------------------------------

        //--------------------------------------------------
        //UBA START
        //--------------------------------------------------

        public void SendSmallUBA(byte[] uba)
        {
            writer.Write("ubaS");
            writer.Write(uba.Length.ToString());
            writer.Write(uba, 0, uba.Length);
        }

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


