using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DataStructures;

namespace Router
{
    class Router
    {
        private Socket managementSystemSocket;
        private Socket cableCloudSocket;

        public string routerName;
        private IPAddress cloudAddress;
        private int cloudPort;
        private IPAddress managementSystemAddress;
        private int managementSystemPort;

        private MplsFibTable mplsFibTable;
        private IpFibTable ipFibTable;
        private IlmTable ilmTable;
        private FtnTable ftnTable;
        private NhlfeTable nhlfeTable;

        private int poppedLabel = 0;

        public Router(string routerConfigFilePath)
        {
            LoadPropertiesFromFile(routerConfigFilePath);
            Console.Title = $"{routerName}";
        }

        public void Start()
        {
            Task.Run(action: () => ConnectToManagementSystem());
            ConnectToCloud();
        }

        private void ConnectToCloud()
        {
            while (true)
            {
                Logs.ShowLog(LogType.INFO, "Connecting to cable cloud...");
                cableCloudSocket = new Socket(cloudAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                try
                {
                    cableCloudSocket.Connect(new IPEndPoint(cloudAddress, cloudPort));

                }
                catch (Exception)
                {
                    Logs.ShowLog(LogType.ERROR, "Couldn't connect to cable cloud.");
                    Logs.ShowLog(LogType.INFO, "Retrying...");
                    Thread.Sleep(5000);
                    continue;
                }

                try
                {
                    Logs.ShowLog(LogType.INFO, "Sending CONNECTED to cable cloud...");
                    Logs.ShowLog(LogType.INFO, "Sending CONNECTED to cable cloud...");
                    string connectedMessage = "CONNECTED";
                    Package connectedCheckPackage = new Package(routerName, cloudAddress.ToString(), cloudPort, connectedMessage);
                    cableCloudSocket.Send(Encoding.ASCII.GetBytes(SerializeToJson(connectedCheckPackage)));
                    while (true)
                    {
                        HandleMessageFromCloud();
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                    Logs.ShowLog(LogType.INFO, "Connection to cable cloud lost.");
                }
            }
        }

        private void ConnectToManagementSystem()
        {
            while (true)
            {
                Logs.ShowLog(LogType.INFO, "Connecting to management system...");
                managementSystemSocket = new Socket(managementSystemAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                try
                {
                    managementSystemSocket.Connect(new IPEndPoint(managementSystemAddress, managementSystemPort));
                   
                }
                catch (Exception)
                {
                    Logs.ShowLog(LogType.ERROR, "Couldn't connect to management system.");
                    Logs.ShowLog(LogType.INFO, "Retrying...");
                    Thread.Sleep(5000);
                    continue;
                }

                try
                {
                    Logs.ShowLog(LogType.INFO, "Sending CONNECTED to management system...");
                    string connectedMessage = "CONNECTED";
                    Package connectedCheckPackage = new Package(routerName, managementSystemAddress.ToString(), managementSystemPort, connectedMessage);
                    managementSystemSocket.Send(Encoding.ASCII.GetBytes(SerializeToJson(connectedCheckPackage)));
                    while (true)
                    {
                        HandleResponseFromMS();
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                    Logs.ShowLog(LogType.INFO, "Connection to management system lost.");
                }
            }
        }

        private void HandleMessageFromCloud()
        {
            Package receivedPackage = ReceiveMessageFrom(cableCloudSocket);

            if (receivedPackage.message == "CONNECTED")
            {
                Logs.ShowLog(LogType.CONNECTED, "Connected to cable cloud.");
            }
            else
            {
                try
                {
                    Logs.ShowLog(LogType.INFO, "Received a package from cable cloud:");
                    Console.WriteLine(SerializeToJson(receivedPackage));
                    Route(receivedPackage);
                    SendPackageToCloud(receivedPackage);
                    poppedLabel = 0;
                    Logs.ShowLog(LogType.INFO, "Sent routed package to cable cloud.");
                }
                catch (Exception)
                { 
                    Logs.ShowLog(LogType.ERROR, "Couldn't perform routing.");
                }
            }

        }

        private void HandleResponseFromMS()
        {
            Package receivedPackage = ReceiveMessageFrom(managementSystemSocket);

            if (receivedPackage.message == "CONNECTED")
            {
                Logs.ShowLog(LogType.CONNECTED, "Connected to management system.");
            }
            else if (receivedPackage.message == "SENDING-TABLES")
            {
                Logs.ShowLog(LogType.INFO, "Received tables from MS.");
                LoadTables(receivedPackage.tablesFile);
            }
            else
            {
                Logs.ShowLog(LogType.ERROR, "Received unknown command from MS.");
            }
        }

        private Package ReceiveMessageFrom(Socket socket)
        {
            byte[] buffer = new byte[5120];
            int bytes = socket.Receive(buffer);
            var message = Encoding.ASCII.GetString(buffer, 0, bytes);
            Package receivedPackage = DeserializeFromJson(message);
            return receivedPackage;
        }

        private void LoadPropertiesFromFile(string configFilePath)
        {
            var properties = new Dictionary<string, string>();
            foreach (var row in File.ReadAllLines(configFilePath))
            {
                properties.Add(row.Split('=')[0], row.Split('=')[1]);
            }
            routerName = properties["ROUTERNAME"];
            managementSystemAddress = IPAddress.Parse(properties["MANAGEMENTSYSTEMADDRESS"]);
            cloudAddress = IPAddress.Parse(properties["CLOUDADDRESS"]);
            managementSystemPort = int.Parse(properties["MANAGEMENTSYSTEMPORT"]);
            cloudPort = int.Parse(properties["CLOUDPORT"]);
        }

        private void LoadTables(List<string> tablesFile)
        {
            // Router LSR nie potrzebuje wszystkich tablic. W naszej topologii tylko R2 jest LSR.
            if (routerName != "R2")
            {
                mplsFibTable = new MplsFibTable(routerName);
                mplsFibTable.LoadTable(tablesFile);
                ftnTable = new FtnTable(routerName);
                ftnTable.LoadTable(tablesFile);
                ilmTable = new IlmTable(routerName);
                ilmTable.LoadTable(tablesFile);
                ipFibTable = new IpFibTable(routerName);
                ipFibTable.LoadTable(tablesFile);
                nhlfeTable = new NhlfeTable(routerName);
                nhlfeTable.LoadTable(tablesFile);
            }
            else
            {
                ilmTable = new IlmTable(routerName);
                ilmTable.LoadTable(tablesFile);
                nhlfeTable = new NhlfeTable(routerName);
                nhlfeTable.LoadTable(tablesFile);
            }
        }

        private string SerializeToJson(Package package)
        {
            string jsonString;
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
            };
            jsonString = JsonSerializer.Serialize(package, options);

            return jsonString;
        }

        private Package DeserializeFromJson(string serializedString)
        {
            Package package = JsonSerializer.Deserialize<Package>(serializedString);
            return package;
        }

        private void PushLabel(Package package, NhlfeEntry nhlfeEntry)
        {
            if (nhlfeEntry.outLabel == null)
            {
                Logs.ShowLog(LogType.ERROR, $"Invalid NHLFE entry for router {routerName}. outLabel null.");
                return;
            }
            else if (nhlfeEntry.outPort == null)
            {
                if (nhlfeEntry.nextId == null)
                {
                    Logs.ShowLog(LogType.ERROR, $"Invalid NHLFE entry for router {routerName}. outPort and nextId null.");
                    return;
                }
                package.labels.Add((int)nhlfeEntry.outLabel);
                var nextNhlfeEntry = nhlfeTable.entries[(int)nhlfeEntry.nextId];
                PushLabel(package, nextNhlfeEntry);
            }
            else if (nhlfeEntry.nextId == null)
            {
                package.labels.Add((int)nhlfeEntry.outLabel);
                package.incomingPort = (int)nhlfeEntry.outPort;
                // send to cloud
            }
            else
            {
                Logs.ShowLog(LogType.ERROR, $"Invalid NHLFE entry for router {routerName}. All 3 values not null.");
                return;
            }
        }

        private void PopLabel(Package package, NhlfeEntry nhlfeEntry)
        {
            if (nhlfeEntry.outLabel != null && nhlfeEntry.outPort != null && nhlfeEntry.nextId != null)
            {
                Logs.ShowLog(LogType.ERROR, $"Invalid NHLFE entry for router {routerName}. outLabel, outPort or nextId  NOT null.");
                return;
            }
            if (package.labels.Any())
            {
                poppedLabel = package.labels[package.labels.Count - 1];
                package.labels.RemoveAt(package.labels.Count - 1);
                if (package.labels.Any())
                {
                    
                    IlmEntry ilmEntry;
                    ilmEntry = ilmTable.entries[$"{package.labels.Last()}, {package.incomingPort}, {poppedLabel}"];
                    var newNhlfeEntry = nhlfeTable.entries[ilmEntry.id];
                    switch (newNhlfeEntry.operation)
                    {
                        case "PUSH":
                            PushLabel(package, newNhlfeEntry);
                            break;
                        case "POP":
                            PopLabel(package, newNhlfeEntry);
                            break;
                        case "SWAP":
                            SwapLabel(package, newNhlfeEntry);
                            break;
                    }
                }
                else
                {
                    var mplsEntry = mplsFibTable.entries[package.destPort];
                    if (mplsEntry.fec == 0)
                    {
                        var ipFibEntry = ipFibTable.entries[package.destAddress];
                        package.incomingPort = ipFibEntry.outPort;
                        // send package to cloud
                    }
                }
            }
            else
            {
                Logs.ShowLog(LogType.ERROR, "No label to pop found.");
            }
        }

        private void SwapLabel(Package package, NhlfeEntry nhlfeEntry)
        {
            package.labels[package.labels.Count - 1] = (int)nhlfeEntry.outLabel;
            package.incomingPort = (int)nhlfeEntry.outPort;
            // send package to cloud
        }

        public void Route(Package package)
        {
            if (routerName == "R2")
            {
                RouteLSR(package);
            }
            else
            {
                RouteLER(package);
            }
        }

        private void RouteLSR(Package package)
        {
            if (!package.labels.Any())
            {
                Logs.ShowLog(LogType.ERROR, "No label found.");
                return;
            }
            IlmEntry ilmEntry;
            if (poppedLabel == 0)
            {
                ilmEntry = ilmTable.entries[$"{package.labels.Last()}, {package.incomingPort}, -"];
            }
            else
            {
                ilmEntry = ilmTable.entries[$"{package.labels.Last()}, {package.incomingPort}, {poppedLabel}"];
            }
            var nhlfeEntry = nhlfeTable.entries[ilmEntry.id];
            if (nhlfeEntry.operation != "SWAP")
            {
                Logs.ShowLog(LogType.ERROR, $"Invalid NHLFE entry for router {routerName}. Different operation than SWAP found.");
                return;
            }
            else if (nhlfeEntry.outLabel == null || nhlfeEntry.outPort == null || nhlfeEntry.nextId != null)
            {
                Logs.ShowLog(LogType.ERROR, $"Invalid NHLFE entry for router {routerName}.");
                return;
            }
            else
            {
                SwapLabel(package, nhlfeEntry);
            }
        }

        private void RouteLER(Package package)
        {
            if (!package.labels.Any())
            {
                var mplsEntry = mplsFibTable.entries[package.destPort];
                if (mplsEntry.fec == 0)
                {
                    var ipFibEntry = ipFibTable.entries[package.destAddress];
                    package.incomingPort = ipFibEntry.outPort;
                    // send package to cloud
                }
                else
                {
                    var ftnEntry = ftnTable.entries[mplsEntry.fec];
                    var nhlfeEntry = nhlfeTable.entries[ftnEntry.id];
                    if (nhlfeEntry.operation != "PUSH")
                    {
                        Logs.ShowLog(LogType.ERROR, $"Invalid NHLFE entry for router {routerName}.");
                        return;
                    }
                    PushLabel(package, nhlfeEntry);
                }
            }
            else
            {
                IlmEntry ilmEntry;
                if (poppedLabel == 0)
                {
                    ilmEntry = ilmTable.entries[$"{package.labels.Last()}, {package.incomingPort}, -"];
                }
                else
                {
                    ilmEntry = ilmTable.entries[$"{package.labels.Last()}, {package.incomingPort}, {poppedLabel}"];
                }
                var nhlfeEntry = nhlfeTable.entries[ilmEntry.id];
                switch(nhlfeEntry.operation)
                {
                    case "PUSH":
                        PushLabel(package, nhlfeEntry);
                        break;
                    case "POP":
                        PopLabel(package, nhlfeEntry);
                        break;
                    case "SWAP":
                        SwapLabel(package, nhlfeEntry);
                        break;
                }
            }
        }

        private void SendPackageToCloud(Package package)
        {
            try
            {
                cableCloudSocket.Send(Encoding.ASCII.GetBytes(SerializeToJson(package)));
            }
            catch (Exception)
            {
                Logs.ShowLog(LogType.ERROR, "Couldn't send package to cable cloud.");
            }
        }
    }
}