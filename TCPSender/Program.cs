
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.IO;
using System.Threading;

namespace TCPSender
{

    public enum ConnectionType { Client, Server };

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
        
        BinaryWriter writer;            //writer dla SendMessage, tutaj zeby nie tworzyc caly czas nowego. na porcie 50001
        int BUFFER_SIZE = 10000;                       //rozmiar bufora dla danych pliku w bajtach

        public string DownloadPath { get; set; } = "downloads"; //directory w ktorym beda zapisywane pliki. domyslnie relative/downloads

        public CommClient(IPAddress _adresIP, ConnectionType isServer) //serwer = listen, client = connect
        {
            cIP = _adresIP;
            if (isServer == ConnectionType.Server)
            {
                Listen(_adresIP);
            }
            else
            {
                Connect(_adresIP);
            }
            OpenCommandLine();
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
            Thread commandLineThread = new Thread(() => ListenForCommands());
            commandLineThread.Start();
        }

        private void ListenForCommands()            //Uruchamia watek nasluchiwania na wiadomosci. Do przerobienia z uwzglednieniem Action
        {
            BinaryReader reader = new BinaryReader(client.GetStream());
            string input = null;
            while (input != "x")
            {
                input = reader.ReadString();
                if (input == "m")
                {
                    input = reader.ReadString();
                    Console.WriteLine(input);       //tu musi wystapic zamiana na Action
                }
                else if (input == "f")
                {
                    input = reader.ReadString();    //placeholder
                    ReceiveFile(input);
                }
            }
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
            for(int i = 0; i<packetCount; i++)
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
            for(int i = 0; i < packetCount; i++)
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


    class Program
    {

        public static int BUFFER_SIZE = 10000;

        public static void WriteToWriter_T(BinaryWriter _writer, string _endMessage, TcpClient _client)    //stara wersja
        {
            string input = null;
            while (input != _endMessage)
            {
                input = Console.ReadLine(); //czytanie stringa z konsoli
                if (input == "plik")
                {
                    string path = @"C:\Users\Czarek\Desktop\tru_haki\asd.zip";

                    FileInfo info = new FileInfo(path);
                    long fileSize = info.Length;
                    _writer.Write(input);
                    _writer.Write(fileSize.ToString());
                    FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read);
                    for (int i = 0; i < fileSize; i++)
                    {
                        _writer.Write(fileStream.ReadByte());
                    }

                    fileStream.Close();
                }
                else if (input == "plik2")
                {
                    string path = @"C:\Users\Czarek\Desktop\pobrane\jajko.exe";

                    FileInfo info = new FileInfo(path);
                    string extension = info.Extension;
                    long fileSize = info.Length;
                    FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read);
                    int packetCount = (int)Math.Floor((double)fileSize / BUFFER_SIZE);
                    long reszta = fileSize - packetCount * BUFFER_SIZE;
                    byte[] buffer = new byte[BUFFER_SIZE];
                    byte[] lastOne = new byte[reszta];

                    _writer.Write(input);
                    _writer.Write(extension);

                    _writer.Write(packetCount.ToString());
                    for (int i = 0; i < packetCount; i++)
                    {
                        fileStream.Read(buffer, 0, BUFFER_SIZE);
                        _client.GetStream().Write(buffer, 0, BUFFER_SIZE);
                        //_writer.Write(buffer);
                    }
                    _writer.Write(reszta.ToString());
                    fileStream.Read(lastOne, 0, (int)reszta);
                    //_writer.Write(lastOne);
                    _client.GetStream().Write(lastOne, 0, (int)reszta);
                    fileStream.Close();
                }
                else
                {
                    _writer.Write(input);   //wysylanie do polaczenia
                }
            }
        }



        public static void ReadFromWriter_T(BinaryReader _reader, string _endMessage, TcpClient _client)
        {
            string input = null;

            while (input != _endMessage)
            {
                input = _reader.ReadString();   //wczytywanie stringa z polaczenia
                if (input == "plik")
                {
                    long size = (long.Parse(_reader.ReadString()));

                    FileStream fileStream = File.OpenWrite(@"C:\_stop\plik");

                    for (int i = 0; i < size; i++)
                    {
                        fileStream.WriteByte(_reader.ReadByte());
                    }

                    fileStream.Close();
                }
                else if (input == "plik2")
                {

                    string extension = _reader.ReadString();
                    int packetCount = int.Parse(_reader.ReadString());
                    byte[] buffer = new byte[BUFFER_SIZE];

                    FileStream fileStream = File.OpenWrite(@"C:\_stop\plik"+extension);

                    for (int i = 0; i < packetCount; i++)
                    {
                        buffer = _reader.ReadBytes(BUFFER_SIZE);
                        fileStream.Write(buffer, 0, BUFFER_SIZE);
                    }

                    int reszta = int.Parse(_reader.ReadString());
                    byte[] lastOne = new byte[reszta];

                    lastOne = _reader.ReadBytes(reszta);
                    fileStream.Write(lastOne, 0, reszta);
                    fileStream.Close();
                }
                else
                {
                    Console.WriteLine(input);   //wypisywanie na ekran
                }

            }
        }



        static int Main(string[] args)
        {

            //TcpClient client = null;   //polaczenie TCP



            //Console.WriteLine("wpisz listen/connect");

            //string komenda = Console.ReadLine();
            //if (komenda == "listen")
            //{
            //    Console.WriteLine("Adres interfejsu do nasluchu: ");
            //    IPAddress adresInterfejsuDoNasluchu = IPAddress.Parse(Console.ReadLine());  //adres IP interfejsu
            //    //IPAddress adresInterfejsuDoNasluchu = IPAddress.Parse("192.168.0.105");  //adres IP interfejsu
            //    Console.WriteLine("Port do nasluchu: ");
            //    TcpListener listener = new TcpListener(adresInterfejsuDoNasluchu, int.Parse(Console.ReadLine()));   //stworzenie listenera na polaczenia tcp na adresie IP i porcie
            //    //TcpListener listener = new TcpListener(adresInterfejsuDoNasluchu, 4000);   //stworzenie listenera na polaczenia tcp na adresie IP i porcie
            //    listener.Start();   //start listenera
            //    client = listener.AcceptTcpClient();    //gdy listener zaakceptuje polaczenie 
            //    if (client != null) Console.WriteLine("connected");
            //}
            //if (komenda == "connect")
            //{
            //    client = new TcpClient();   //tworzenie klienta
            //    Console.WriteLine("Adres hosta do polaczenia: ");
            //    IPAddress adresInterfejsuDoPolaczenia = IPAddress.Parse(Console.ReadLine());  //adres IP interfejsu
            //    Console.WriteLine("Port do polaczenia: ");
            //    try
            //    {
            //        client.Connect(adresInterfejsuDoPolaczenia, int.Parse(Console.ReadLine())); //tworzenie polaczenia
            //    }
            //    catch (Exception e)
            //    {
            //        Console.WriteLine(e.ToString());

            //    }
            //}


            //if (client != null)
            //{
            //    BinaryReader reader = new BinaryReader(client.GetStream());
            //    BinaryWriter writer = new BinaryWriter(client.GetStream());



            //    Thread writerThread = new Thread(() => WriteToWriter_T(writer, "end", client));
            //    Thread readerThread = new Thread(() => ReadFromWriter_T(reader, "end", client));
            //    writerThread.Start();
            //    readerThread.Start();

            //}




            CommClient client = null;

            Console.WriteLine("listen/connect");
            string listenConnect = Console.ReadLine();
            if (listenConnect == "listen")
            {
                Console.WriteLine("Adres interfejsu do nasluchu: ");
                IPAddress adresInterfejsuDoNasluchu = IPAddress.Parse(Console.ReadLine());  //adres IP interfejsu
                client = new CommClient(adresInterfejsuDoNasluchu, ConnectionType.Server);
            }
            if (listenConnect == "connect")
            {
                Console.WriteLine("Adres hosta do polaczenia: ");
                IPAddress adresInterfejsuDoPolaczenia = IPAddress.Parse(Console.ReadLine());  //adres IP interfejsu
                client = new CommClient(adresInterfejsuDoPolaczenia, ConnectionType.Client);
            }

            string input = null;
            while (input != "x")
            {
                input = Console.ReadLine();
                if(input == "plik")
                {
                    client.SendFile(@"C:\Users\Czarek\Desktop\tru_haki\asd.zip");
                }
                else
                {
                    client.SendMessage(input);
                }
            }


            Console.ReadLine();
            return 0;

        }
    }
}

