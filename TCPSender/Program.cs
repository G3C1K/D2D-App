
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
    //public class CommClient
    //{
    //    TcpClient client;
    //    BinaryReader reader;
    //    BinaryWriter writer;
    //    Thread writerThread;
    //    Thread readerThread;
    //    string messageGet;
    //    string MessageSet
    //    {
    //        get { return messageSet_; }
    //        set
    //        {
    //            messageSet_ = value;
    //            waitHandle.Set();
    //        }
    //    }
    //    string messageSet_;
    //    AutoResetEvent waitHandle = new AutoResetEvent(false);

    //    public CommClient(IPAddress _adresIP, bool isServer) //jak isServer true to swoj, jak isServer false to czyjs
    //    {
    //        if(isServer == true)
    //        {
    //            Listen(_adresIP);
    //        }
    //        else
    //        {
    //            Connect(_adresIP);
    //        }
    //        writerThread = new Thread(() => WriteToHost_T(client));
    //        readerThread = new Thread(() => ReadFromHost(client, Console.WriteLine));
    //    }

    //    private bool Listen(IPAddress _adresInterfejsuNasluchu)
    //    {
    //        TcpListener listener = new TcpListener(_adresInterfejsuNasluchu, 40001);
    //        listener.Start();
    //        client = listener.AcceptTcpClient();
    //        return true;
    //    }

    //    private bool Connect(IPAddress _adresIPHosta)
    //    {
    //        client = new TcpClient();   //tworzenie klienta
    //        try
    //        {
    //            client.Connect(_adresIPHosta, 40001); //tworzenie polaczenia
    //        }
    //        catch (Exception e)
    //        {
    //            Console.WriteLine(e.Message);
    //        }
    //        return true;
    //    }

    //    public void ReadFromHost(TcpClient _client, Action<string> _stringDestination)
    //    {
    //        if (_client == null) throw new ArgumentException("Brak połączenia");

    //        string input = null;
    //        BinaryReader reader = new BinaryReader(_client.GetStream());
    //        while (true)
    //        {
    //            input = reader.ReadString();
    //            _stringDestination(input);
    //        }
    //    }

    //    private void WriteToHost_T(TcpClient _client)
    //    {
    //        waitHandle.WaitOne();

    //        if (_client == null) throw new ArgumentException("Brak połączenia");

    //        writer.Write(MessageSet);

    //        waitHandle.
    //    }

    //    public void WriteToHost(string _message)
    //    {
    //        MessageSet = _message;
    //    }

    //}


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
                    string path = @"C:\Users\Czarek\Desktop\tru_haki\so.zip";

                    FileInfo info = new FileInfo(path);
                    long fileSize = info.Length;
                    FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read);
                    int packetCount = (int)Math.Floor((double)fileSize / BUFFER_SIZE);
                    long reszta = fileSize - packetCount * BUFFER_SIZE;
                    byte[] buffer = new byte[BUFFER_SIZE];
                    byte[] lastOne = new byte[reszta];

                    _writer.Write(input);

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
                    byte byteHolder;

                    FileStream fileStream = File.OpenWrite(@"C:\_stop\plik");

                    for (int i = 0; i < size; i++)
                    {
                        fileStream.WriteByte(_reader.ReadByte());
                    }

                    fileStream.Close();
                }
                else if (input == "plik2")
                {
                    int packetCount = int.Parse(_reader.ReadString());
                    byte[] buffer = new byte[BUFFER_SIZE];

                    FileStream fileStream = File.OpenWrite(@"C:\_stop\plik");

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

            TcpClient client = null;   //polaczenie TCP



            Console.WriteLine("wpisz listen/connect");

            string komenda = Console.ReadLine();
            if (komenda == "listen")
            {
                Console.WriteLine("Adres interfejsu do nasluchu: ");
                IPAddress adresInterfejsuDoNasluchu = IPAddress.Parse(Console.ReadLine());  //adres IP interfejsu
                //IPAddress adresInterfejsuDoNasluchu = IPAddress.Parse("192.168.0.105");  //adres IP interfejsu
                Console.WriteLine("Port do nasluchu: ");
                TcpListener listener = new TcpListener(adresInterfejsuDoNasluchu, int.Parse(Console.ReadLine()));   //stworzenie listenera na polaczenia tcp na adresie IP i porcie
                //TcpListener listener = new TcpListener(adresInterfejsuDoNasluchu, 4000);   //stworzenie listenera na polaczenia tcp na adresie IP i porcie
                listener.Start();   //start listenera
                client = listener.AcceptTcpClient();    //gdy listener zaakceptuje polaczenie 
                if (client != null) Console.WriteLine("connected");
            }
            if (komenda == "connect")
            {
                client = new TcpClient();   //tworzenie klienta
                Console.WriteLine("Adres hosta do polaczenia: ");
                IPAddress adresInterfejsuDoPolaczenia = IPAddress.Parse(Console.ReadLine());  //adres IP interfejsu
                Console.WriteLine("Port do polaczenia: ");
                try
                {
                    client.Connect(adresInterfejsuDoPolaczenia, int.Parse(Console.ReadLine())); //tworzenie polaczenia
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());

                }
            }


            if (client != null)
            {
                BinaryReader reader = new BinaryReader(client.GetStream());
                BinaryWriter writer = new BinaryWriter(client.GetStream());



                Thread writerThread = new Thread(() => WriteToWriter_T(writer, "end", client));
                Thread readerThread = new Thread(() => ReadFromWriter_T(reader, "end", client));
                writerThread.Start();
                readerThread.Start();

            }

            return 0;

        }
    }
}

