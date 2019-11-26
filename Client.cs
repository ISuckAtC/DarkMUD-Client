using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace DarkMUD_Client
{
    class Client
    {

        static string[] config = File.ReadAllLines("config.txt");
        ulong token;

        static string ipAdress = config[0];
        static int port = int.Parse(config[1]);

        static int bufferSize = 8192;

        static System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();

        static void Main(string[] args)
        {           
            TcpClient client = new TcpClient();
            Console.WriteLine("CLIENT: Connecting on {0}:{1}...", ipAdress, port);
            client.Connect(ipAdress, port);

            NetworkStream stream = client.GetStream();

            Console.WriteLine("CLIENT: Connected!");
            Console.WriteLine();

            Console.WriteLine("CLIENT: Starting Authentication");
            Authenticate(stream);

            Task listener = Task.Run(() => Listener(client, stream));

            MessageLoop(stream).GetAwaiter().GetResult();

            Console.WriteLine("CLIENT: Closing...");
            stream.Close();
            client.Close();
        }

        static async Task MessageLoop(NetworkStream s)
        {
            while(true)
            {
                string read = Console.ReadLine();
                switch(read.ToLower())
                {
                    case "q": 
                    await Send(s, "q");
                    return;
                    
                    case "ping":
                    timer.Start();
                    await Send(s, "ping");
                    break;

                    default:
                    await Send(s, read);
                    break;
                }      
            }
        }

        static async Task Listener(TcpClient c, NetworkStream s)
        {
            while(true)
            {
                string response = await GetResponse(s);
                if (response == "Pong!") 
                {
                    Console.WriteLine("{0} ({1}ms)", response, timer.ElapsedMilliseconds);
                    timer.Reset();
                    continue;
                }
                Console.WriteLine(response);

                if (response == "Your session has timed out!") 
                {
                    Console.WriteLine("CLIENT: Closing...");
                    s.Close();
                    c.Close();
                    Environment.Exit(1);
                }
            }
        }

        static async void Authenticate(NetworkStream s)
        {
            Console.Write(await GetResponse(s));
            await Send(s, Console.ReadLine());
            string nameres = await GetResponse(s);
            Console.Write(nameres);
            if (nameres == "Your username cannot be empty!\n")
            {
                await Send(s, string.Empty);
                Authenticate(s);
            }
            else
            {
                await Send(s, Console.ReadLine());
                string authres = await GetResponse(s);
                Console.WriteLine(authres);
                if (authres != "You have logged in!\n")
                {
                    await Send(s, string.Empty);
                    Authenticate(s);
                }
            }
            return;
        }

        static async Task Send(NetworkStream s, string message) { await s.WriteAsync(Encoding.ASCII.GetBytes(message + "END")); }

        static async Task<string> GetResponse(NetworkStream s)
        {
            Byte[] response = new Byte[bufferSize];
            while(!s.DataAvailable);
            while(s.DataAvailable) await s.ReadAsync(response, 0, response.Length);

            string responseS = Encoding.ASCII.GetString(response, 0, response.Length);

            return responseS.Substring(0, responseS.IndexOf("END"));
        }
    }
}