using DataStructures;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Host
{
    public class ObjectState
    {
        public Socket workSocket = null;
        public const int bufferSize = 1024;
        public byte[] buffer = new byte[bufferSize];
        public StringBuilder sb = new StringBuilder();
    }

    class Host
    {
        private Socket cableCloudSocket;
        private string hostName;
        private int hostPort;
        private IPAddress destAddress;
        private IPAddress ipSourceAddress;
        private int h1Port;
        private IPAddress ipAddressH1;
        private int h2Port;
        private IPAddress ipAddressH2;
        private int h3Port;
        private IPAddress ipAddressH3;
        private int h4Port;
        private IPAddress ipAddressH4;
        private int cableCloudPort;
        private int destinationPort;
        private IPAddress cableCloudIpAddress;
        private ManualResetEvent allDone = new ManualResetEvent(false);

        public Host(string filePath)
        {
            LoadPropertiesFromFile(filePath);
            Console.Title = hostName;
            ConnectToCableCloud();
        }

        public void StartHost(Socket cableCloudSocket)
        {
            while (true)
            {
                Task.Run(action: () => ReceiveMessages());
                Console.WriteLine("Write '1'  if you want to send the message to another host ");

                string decision = Console.ReadLine();
                if (decision == "1")
                {
                    try
                    {
                        Console.WriteLine("\nChoose which host you want to send the package to: \n    1. H1 \n    2. H2 \n    3. H3 \n    4. H4");
                        string choice = Console.ReadLine();
                        if (choice == "1")
                        {
                            destinationPort = h1Port;
                            destAddress = ipAddressH1;
                        }
                        else if (choice == "2")
                        {
                            destinationPort = h2Port;
                            destAddress = ipAddressH2;
                        }
                        else if (choice == "3")
                        {
                            destinationPort = h3Port;
                            destAddress = ipAddressH3;
                        }
                        else if (choice == "4")
                        {
                            destinationPort = h4Port;
                            destAddress = ipAddressH4;
                        }
                        else
                        {
                            Console.WriteLine("\nNo such host connected. Try again.");
                            continue;
                        }
                        Console.WriteLine("Write message");
                        string message = Console.ReadLine().ToString();
                        Send(cableCloudSocket, $"{DateTime.Now} :: Message: " + message);
                        allDone.WaitOne();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }
                else
                {
                    Logs.ShowLog(LogType.ERROR, "You wrote something other than '1' ");
                    Console.WriteLine("Please try again.\n");
                }
            }
        }

        private void ConnectToCableCloud()
        {
            Logs.ShowLog(LogType.INFO, "Connecting to cable cloud...");
            while (true)
            {
                cableCloudSocket = new Socket(cableCloudIpAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                try
                {
                    cableCloudSocket.Connect(new IPEndPoint(cableCloudIpAddress, cableCloudPort));

                }
                catch (Exception)
                {
                    Logs.ShowLog(LogType.ERROR, "Couldn't connect to cable cloud.");
                    Console.WriteLine("Reconnecting...");
                    Thread.Sleep(5000);
                    continue;
                }

                try
                {
                    Logs.ShowLog(LogType.INFO, "Sending CONNECTED to cable cloud...");
                    string connectedMessage = "CONNECTED";
                    Package package = new Package(hostName, cableCloudIpAddress.ToString(), cableCloudPort, connectedMessage);
                    cableCloudSocket.Send(Encoding.ASCII.GetBytes(SerializeToJson(package)));

                    byte[] buffer = new byte[1024];
                    int bytes = cableCloudSocket.Receive(buffer);

                    var message = Encoding.ASCII.GetString(buffer, 0, bytes);

                    if (message.Contains("CONNECTED"))
                    {
                        Logs.ShowLog(LogType.CONNECTED, "Connected to cable cloud.");                    
                        StartHost(cableCloudSocket);
                        break;

                    }
                }
                catch (Exception)
                {
                    Logs.ShowLog(LogType.ERROR, "Couldn't send hello to cable cloud.");
                }

            }
        }

        private void ReceiveMessages()
        {
            while (true)
            {
                byte[] buffer = new byte[1024];
                int bytes = cableCloudSocket.Receive(buffer);
                var message = Encoding.ASCII.GetString(buffer, 0, bytes);
                Logs.ShowLog(LogType.INFO, "Received from cable cloud: \n" + message); 
            }
        }

        private void LoadPropertiesFromFile(string filePath)
        {
            var properties = new Dictionary<string, string>();
            foreach (var row in File.ReadAllLines(filePath))
            {
                properties.Add(row.Split('=')[0], row.Split('=')[1]);
            }
            hostName = properties["HOSTNAME"];
            hostPort = int.Parse(properties["HOSTPORT"]);
            ipSourceAddress = IPAddress.Parse(properties["IPSOURCEADDRESS"]);
            cableCloudIpAddress = IPAddress.Parse(properties["CABLECLOUDIPADDRESS"]);
            cableCloudPort = int.Parse(properties["CABLECLOUDPORT"]);

            if (hostName == "H1")
            {
                h1Port = int.Parse(properties["HOSTPORT"]);
                ipAddressH1 = IPAddress.Parse(properties["IPSOURCEADDRESS"]);
                h2Port = int.Parse(properties["H2PORT"]);
                ipAddressH2 = IPAddress.Parse(properties["IPADDRESSH2"]);
                h3Port = int.Parse(properties["H3PORT"]);
                ipAddressH3 = IPAddress.Parse(properties["IPADDRESSH3"]);
                h4Port = int.Parse(properties["H4PORT"]);
                ipAddressH4 = IPAddress.Parse(properties["IPADDRESSH4"]);
            }
            else if (hostName == "H2")
            {
                h2Port = int.Parse(properties["HOSTPORT"]);
                ipAddressH2 = IPAddress.Parse(properties["IPSOURCEADDRESS"]);
                h1Port = int.Parse(properties["H1PORT"]);
                ipAddressH1 = IPAddress.Parse(properties["IPADDRESSH1"]);
                h3Port = int.Parse(properties["H3PORT"]);
                ipAddressH3 = IPAddress.Parse(properties["IPADDRESSH3"]);
                h4Port = int.Parse(properties["H4PORT"]);
                ipAddressH4 = IPAddress.Parse(properties["IPADDRESSH4"]);
            }
            else if (hostName == "H3")
            {
                h3Port = int.Parse(properties["HOSTPORT"]);
                ipAddressH3 = IPAddress.Parse(properties["IPSOURCEADDRESS"]);
                h1Port = int.Parse(properties["H1PORT"]);
                ipAddressH1 = IPAddress.Parse(properties["IPADDRESSH1"]);
                h2Port = int.Parse(properties["H2PORT"]);
                ipAddressH2 = IPAddress.Parse(properties["IPADDRESSH2"]);
                h4Port = int.Parse(properties["H4PORT"]);
                ipAddressH4 = IPAddress.Parse(properties["IPADDRESSH4"]);
            }
            else if (hostName == "H4")
            {
                h4Port = int.Parse(properties["HOSTPORT"]);
                ipAddressH4 = IPAddress.Parse(properties["IPSOURCEADDRESS"]);
                h1Port = int.Parse(properties["H1PORT"]);
                ipAddressH1 = IPAddress.Parse(properties["IPADDRESSH1"]);
                h2Port = int.Parse(properties["H2PORT"]);
                ipAddressH2 = IPAddress.Parse(properties["IPADDRESSH2"]);
                h3Port = int.Parse(properties["H3PORT"]);
                ipAddressH3 = IPAddress.Parse(properties["IPADDRESSH3"]);
            }
        }

        private void Send(Socket hostSocket, string data)
        {
            Package package = new Package(hostName, hostPort, destAddress.ToString(), destinationPort, data);
            string json = SerializeToJson(package);
            Logs.ShowLog(LogType.INFO, ("Sending package to cable cloud."));
            byte[] byteData = Encoding.ASCII.GetBytes(json);
            hostSocket.BeginSend(byteData, 0, byteData.Length, 0,
                new AsyncCallback(SendCallback), hostSocket);
        }

        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                Socket hostSocket = (Socket)ar.AsyncState;
                int byteSent = hostSocket.EndSend(ar);
                Logs.ShowLog(LogType.INFO, $"Sent: {byteSent} bytes to Cable Cloud");
                allDone.Set();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public static string SerializeToJson(Package package)
        {
            string jsonString;
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
            };
            jsonString = JsonSerializer.Serialize(package, options);

            return jsonString;
        }

        public static Package DeserializeFromJson(string serializedString)
        {
            Package package = new Package();
            package = JsonSerializer.Deserialize<Package>(serializedString);
            return package;
        }
    }

}