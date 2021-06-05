using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DataStructures;

namespace ManagementSystem
{
    class StateObject
    {
        public Socket workSocket = null;
        public const int BufferSize = 2048;
        public byte[] buffer = new byte[BufferSize];
        public StringBuilder sb = new StringBuilder();
    }

    class ManagementSystem
    {
        private Socket msSocket;
        private int port = 5000;
        private IPAddress ipAddress = IPAddress.Parse("127.0.0.1");
        private ManualResetEvent allDone = new ManualResetEvent(false);
        private Dictionary<string, Socket> connectedSockets = new Dictionary<string, Socket>();
        private MplsFibTable mplsFibTable;
        private IpFibTable ipFibTable;
        private IlmTable ilmTable;
        private FtnTable ftnTable;
        private NhlfeTable nhlfeTable;

        private string R1TablesFilePath;
        private string R2TablesFilePath;
        private string R3TablesFilePath;
        private string R4TablesFilePath;

        public ManagementSystem(string r1Path, string r2Path, string r3Path, string r4Path)
        {
            Console.Title = "Management System";
            R1TablesFilePath = r1Path;
            R2TablesFilePath = r2Path;
            R3TablesFilePath = r3Path;
            R4TablesFilePath = r4Path;
        }

        public void Start()
        {
            var t = Task.Run(action: () => ListenForConnections());
            Thread.Sleep(1000);
            HandleInput();
            Console.ReadLine();
        }

        private void HandleInput()
        {
            while (true)
            {
                if (connectedSockets.Count == 0)
                {
                    continue;
                }

                // Clear the keyboard buffer.
                while (Console.KeyAvailable)
                {
                    Console.ReadKey(false);
                }

                Console.WriteLine("\nChoose which router you want to manage: \n1. R1 \n2. R2 \n3. R3 \n4. R4");
                var choice = Console.ReadLine();

                if (choice == "1")
                {
                    string routerName = $"R{choice}";
                    mplsFibTable = new MplsFibTable(R1TablesFilePath, routerName);
                    ipFibTable = new IpFibTable(R1TablesFilePath, routerName);
                    ilmTable = new IlmTable(R1TablesFilePath, routerName);
                    ftnTable = new FtnTable(R1TablesFilePath, routerName);
                    nhlfeTable = new NhlfeTable(R1TablesFilePath, routerName);
                    ManageLER(routerName, R1TablesFilePath);
                }
                else if (choice == "2")
                {
                    string routerName = $"R{choice}";
                    ilmTable = new IlmTable(R2TablesFilePath, routerName);
                    nhlfeTable = new NhlfeTable(R2TablesFilePath, routerName);
                    ManageLSR(routerName, R2TablesFilePath);
                }
                else if (choice == "3")
                {
                    string routerName = $"R{choice}";
                    mplsFibTable = new MplsFibTable(R3TablesFilePath, routerName);
                    ipFibTable = new IpFibTable(R3TablesFilePath, routerName);
                    ilmTable = new IlmTable(R3TablesFilePath, routerName);
                    ftnTable = new FtnTable(R3TablesFilePath, routerName);
                    nhlfeTable = new NhlfeTable(R3TablesFilePath, routerName);
                    ManageLER(routerName, R3TablesFilePath);
                }
                else if (choice == "4")
                {
                    string routerName = $"R{choice}";
                    mplsFibTable = new MplsFibTable(R4TablesFilePath, routerName);
                    ipFibTable = new IpFibTable(R4TablesFilePath, routerName);
                    ilmTable = new IlmTable(R4TablesFilePath, routerName);
                    ftnTable = new FtnTable(R4TablesFilePath, routerName);
                    nhlfeTable = new NhlfeTable(R4TablesFilePath, routerName);
                    ManageLER(routerName, R4TablesFilePath);
                }
                else
                {
                    Console.WriteLine("\nNo such router connected. Try again.");
                    continue;
                }
            }
        }

        private void ManageLER(string routerName, string tablesFilePath)
        {
            Console.WriteLine("\nWhich table would you like to edit?");
            Console.WriteLine("1. MPLS-FIB");
            Console.WriteLine("2. IP-FIB");
            Console.WriteLine("3. FTN");
            Console.WriteLine("4. NHLFE");
            Console.WriteLine("5. ILM");
            Console.WriteLine("\nType a number from '1' to '5' to choose a table, '0' to go back:");
            string choice = Console.ReadLine();
            Console.WriteLine();
            if (choice == "1")
            {
                ManageMPLSFIBTable(routerName, tablesFilePath);

            }
            else if (choice == "2")
            {
                ManageIPFIBTable(routerName, tablesFilePath);
            }
            else if (choice == "3")
            {
                ManageFTNTable(routerName, tablesFilePath);
            }
            else if (choice == "4")
            {
                ManageNHLFETable(routerName, tablesFilePath);
            }
            else if (choice == "5")
            {
                ManageILMTable(routerName, tablesFilePath);
            }
            else if (choice == "0")
            {
                HandleInput();
            }
            else
            {
                Console.WriteLine("There is no such table. Try again.");
                ManageLER(routerName, tablesFilePath);
            }
        }

        private void ManageLSR(string routerName, string tablesFilePath)
        {
            Console.WriteLine("\nWhich table would you like to edit?");
            Console.WriteLine("1. NHLFE");
            Console.WriteLine("2. ILM");
            Console.WriteLine("Type a number from '1' to '2' to choose a table, '0' to go back:");
            string choice = Console.ReadLine();
            Console.WriteLine();
            if (choice == "1")
            {
                ManageNHLFETable(routerName, tablesFilePath);
            }
            else if (choice == "2")
            {
                ManageILMTable(routerName, tablesFilePath);
            }
            else if (choice == "0")
            {
                HandleInput();
            }
            else
            {
                Console.WriteLine("\nThere is no such table. Try again.");
                ManageLSR(routerName, tablesFilePath);
            }
        }

        private void ManageMPLSFIBTable(string routerName, string tablesFilePath)
        {
            mplsFibTable.PrintEntries();
            Console.WriteLine("\nType '1' to add or '2' to delete, '0' to go back:");
            string choice = Console.ReadLine();
            if (choice == "1")
            {
                Console.WriteLine("Enter new parameters seperating them with a comma and a whitespace (destPort, FEC):");
                string input = Console.ReadLine();
                var splitInput = input.Split(", ");
                if (splitInput.Length != 2 && routerName == "R2")
                {
                    Console.WriteLine("Invalid number of arguments entered.");
                    ManageLSR(routerName, tablesFilePath);
                }
                else if (splitInput.Length != 2 && routerName != "R2")
                {
                    Console.WriteLine("Invalid number of arguments entered.");
                    ManageLER(routerName, tablesFilePath);
                }
                mplsFibTable.AddRowToTable(tablesFilePath, routerName, int.Parse(splitInput[0]), int.Parse(splitInput[1]));
                mplsFibTable.PrintEntries();
                SendTables(routerName);
            }
            else if (choice == "2")
            {
                Console.WriteLine("\nWhich key in dictionary would you like to remove?");
                string row = Console.ReadLine();
                mplsFibTable.DeleteRowFromTable(row, tablesFilePath);
                mplsFibTable.PrintEntries();
                SendTables(routerName);
            }
            else if (choice == "0")
            {
                if (routerName == "R2")
                {
                    ManageLSR(routerName, tablesFilePath);
                }
                else
                {
                    ManageLER(routerName, tablesFilePath);
                }
            }
            else
            {
                Console.WriteLine("Wrong command number.");
                if (routerName == "R2")
                {
                    ManageLSR(routerName, tablesFilePath);
                }
                else
                {
                    ManageLER(routerName, tablesFilePath);
                }
            }
        }

        private void ManageIPFIBTable(string routerName, string tablesFilePath)
        {
            ipFibTable.PrintEntries();
            Console.WriteLine("\nType '1' to add or '2' to delete, '0' to go back:");
            string choice = Console.ReadLine();
            if (choice == "1")
            {
                Console.WriteLine("Enter new parameters seperating them with a comma and a whitespace (destAddress, outPort):");
                string input = Console.ReadLine();
                var splitInput = input.Split(", ");
                if (splitInput.Length != 2 && routerName == "R2")
                {
                    Console.WriteLine("Invalid number of arguments entered.");
                    ManageLSR(routerName, tablesFilePath);
                }
                else if (splitInput.Length != 2 && routerName != "R2")
                {
                    Console.WriteLine("Invalid number of arguments entered.");
                    ManageLER(routerName, tablesFilePath);
                }
                ipFibTable.AddRowToTable(tablesFilePath, routerName, splitInput[0], int.Parse(splitInput[1]));
                ipFibTable.PrintEntries();
                SendTables(routerName);
            }
            else if (choice == "2")
            {
                Console.WriteLine("\nWhich key in dictionary would you like to remove?");
                string row = Console.ReadLine();
                ipFibTable.DeleteRowFromTable(row, tablesFilePath);
                ipFibTable.PrintEntries();
                SendTables(routerName);
            }
            else if (choice == "0")
            {
                if (routerName == "R2")
                {
                    ManageLSR(routerName, tablesFilePath);
                }
                else
                {
                    ManageLER(routerName, tablesFilePath);
                }
            }
            else
            {
                Console.WriteLine("Wrong command number.");
                if (routerName == "R2")
                {
                    ManageLSR(routerName, tablesFilePath);
                }
                else
                {
                    ManageLER(routerName, tablesFilePath);
                }
            }
        }

        private void ManageFTNTable(string routerName, string tablesFilePath)
        {
            ftnTable.PrintEntries();
            Console.WriteLine("\nType '1' to add or '2' to delete, '0' to go back:");
            string choice = Console.ReadLine();
            if (choice == "1")
            {
                Console.WriteLine("Enter new parameters seperating them with a comma and a whitespace (FEC, ID):");
                string input = Console.ReadLine();
                var splitInput = input.Split(", ");
                if (splitInput.Length != 2 && routerName == "R2")
                {
                    Console.WriteLine("Invalid number of arguments entered.");
                    ManageLSR(routerName, tablesFilePath);
                }
                else if (splitInput.Length != 2 && routerName != "R2")
                {
                    Console.WriteLine("Invalid number of arguments entered.");
                    ManageLER(routerName, tablesFilePath);
                }
                ftnTable.AddRowToTable(tablesFilePath, routerName, int.Parse(splitInput[0]), int.Parse(splitInput[1]));
                ftnTable.PrintEntries();
                SendTables(routerName);
            }
            else if (choice == "2")
            {
                Console.WriteLine("\nWhich key in dictionary would you like to remove?");
                string row = Console.ReadLine();
                ftnTable.DeleteRowFromTable(row, tablesFilePath);
                ftnTable.PrintEntries();
                SendTables(routerName);
            }
            else if (choice == "0")
            {
                if (routerName == "R2")
                {
                    ManageLSR(routerName, tablesFilePath);
                }
                else
                {
                    ManageLER(routerName, tablesFilePath);
                }
            }
            else
            {
                Console.WriteLine("Wrong command number.");
                if (routerName == "R2")
                {
                    ManageLSR(routerName, tablesFilePath);
                }
                else
                {
                    ManageLER(routerName, tablesFilePath);
                }
            }
        }

        private void ManageILMTable(string routerName, string tablesFilePath)
        {
            ilmTable.PrintEntries();
            Console.WriteLine("\nType '1' to add or '2' to delete, '0' to go back:");
            string choice = Console.ReadLine();
            if (choice == "1")
            {
                Console.WriteLine("Enter new parameters seperating them with a comma and a whitespace (inLabel, inPort, poppedLabel, ID):");
                string input = Console.ReadLine();
                var splitInput = input.Split(", ");
                if (splitInput.Length != 4 && routerName == "R2")
                {
                    Console.WriteLine("Invalid number of arguments entered.");
                    ManageLSR(routerName, tablesFilePath);
                }
                else if (splitInput.Length != 4 && routerName != "R2")
                {
                    Console.WriteLine("Invalid number of arguments entered.");
                    ManageLER(routerName, tablesFilePath);
                }
                if (splitInput[2] == "-")
                {
                    ilmTable.AddRowToTable(tablesFilePath, routerName, int.Parse(splitInput[0]), int.Parse(splitInput[1]), null, int.Parse(splitInput[3]));
                    ilmTable.PrintEntries();
                    SendTables(routerName);
                }
                else
                {
                    ilmTable.AddRowToTable(tablesFilePath, routerName, int.Parse(splitInput[0]), int.Parse(splitInput[1]), int.Parse(splitInput[2]), int.Parse(splitInput[3]));
                    ilmTable.PrintEntries();
                    SendTables(routerName);
                }

            }
            else if (choice == "2")
            {
                Console.WriteLine("\nWhich key in dictionary would you like to remove? The key format is as follows: inLabel, inPort, poppedLabel");
                string row = Console.ReadLine();
                ilmTable.DeleteRowFromTable(row, tablesFilePath);
                ilmTable.PrintEntries();
                SendTables(routerName);
            }
            else if (choice == "0")
            {
                if (routerName == "R2")
                {
                    ManageLSR(routerName, tablesFilePath);
                }
                else
                {
                    ManageLER(routerName, tablesFilePath);
                }
            }
            else
            {
                Console.WriteLine("Wrong command number.");
                if (routerName == "R2")
                {
                    ManageLSR(routerName, tablesFilePath);
                }
                else
                {
                    ManageLER(routerName, tablesFilePath);
                }
            }
        }

        private void ManageNHLFETable(string routerName, string tablesFilePath)
        {
            nhlfeTable.PrintEntries();
            Console.WriteLine("\nType '1' to add or '2' to delete, '0' to go back:");
            string choice = Console.ReadLine();
            if (choice == "1")
            {
                Console.WriteLine("Enter new parameters seperating them with a comma and a whitespace (ID, Operation, OutLabel, OutPort, NextID):");
                string input = Console.ReadLine();
                var splitInput = input.Split(", ");
                if (splitInput.Length != 5 && routerName == "R2")
                {
                    Console.WriteLine("Invalid number of arguments entered.");
                    ManageLSR(routerName, tablesFilePath);
                }
                else if (splitInput.Length != 5 && routerName != "R2")
                {
                    Console.WriteLine("Invalid number of arguments entered.");
                    ManageLER(routerName, tablesFilePath);
                }
                if (splitInput[2] == "-" && splitInput[3] == "-" && splitInput[4] == "-")
                {
                    nhlfeTable.AddRowToTable(tablesFilePath, routerName, int.Parse(splitInput[0]), splitInput[1], null, null, null);
                    nhlfeTable.PrintEntries();
                    SendTables(routerName);
                }
                else if (splitInput[2] == "-")
                {
                    nhlfeTable.AddRowToTable(tablesFilePath, routerName, int.Parse(splitInput[0]), splitInput[1], null, int.Parse(splitInput[3]), int.Parse(splitInput[4]));
                    nhlfeTable.PrintEntries();
                    SendTables(routerName);
                }
                else if (splitInput[3] == "-")
                {
                    nhlfeTable.AddRowToTable(tablesFilePath, routerName, int.Parse(splitInput[0]), splitInput[1], int.Parse(splitInput[2]), null, int.Parse(splitInput[4]));
                    nhlfeTable.PrintEntries();
                    SendTables(routerName);
                }
                else if (splitInput[4] == "-")
                {
                    nhlfeTable.AddRowToTable(tablesFilePath, routerName, int.Parse(splitInput[0]), splitInput[1], int.Parse(splitInput[2]), int.Parse(splitInput[3]), null);
                    nhlfeTable.PrintEntries();
                    SendTables(routerName);
                }
                else
                {
                    Console.WriteLine("Invalid NHLFE entry.");
                    if (routerName == "R2")
                    {
                        ManageLSR(routerName, tablesFilePath);
                    }
                    else
                    {
                        ManageLER(routerName, tablesFilePath);
                    }
                }

            }
            else if (choice == "2")
            {
                Console.WriteLine("\nWhich key in dictionary would you like to remove?");
                string row = Console.ReadLine();
                nhlfeTable.DeleteRowFromTable(row, tablesFilePath);
                nhlfeTable.PrintEntries();
                SendTables(routerName);
            }
            else if (choice == "0")
            {
                if (routerName == "R2")
                {
                    ManageLSR(routerName, tablesFilePath);
                }
                else
                {
                    ManageLER(routerName, tablesFilePath);
                }
            }
            else
            {
                Console.WriteLine("Wrong command number.");
                if (routerName == "R2")
                {
                    ManageLSR(routerName, tablesFilePath);
                }
                else
                {
                    ManageLER(routerName, tablesFilePath);
                }
            }

        }

        private void ListenForConnections()
        {
            Logs.ShowLog(LogType.INFO, "Awaiting connection...");
            msSocket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                msSocket.Bind(new IPEndPoint(ipAddress, port));
                msSocket.Listen(100);
                while (true)
                {
                    allDone.Reset();
                    msSocket.BeginAccept(new AsyncCallback(AcceptCallback), msSocket);
                    allDone.WaitOne();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private void AcceptCallback(IAsyncResult ar)
        {
            // Signal the main thread to continue.  
            allDone.Set();

            // Get the socket that handles the client request.  
            Socket listener = (Socket)ar.AsyncState;
            Socket handler = listener.EndAccept(ar);

            // Create the state object.  
            StateObject state = new StateObject
            {
                workSocket = handler
            };
            handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                new AsyncCallback(ReadCallback), state);
        }

        private void ReadCallback(IAsyncResult ar)
        {
            StateObject state = (StateObject)ar.AsyncState;
            Socket handler = null;
            handler = state.workSocket;
            Package receivedPackage = new Package();
            try
            {
                // Read data from the client socket.
                int bytesRead = handler.EndReceive(ar);

                state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0, bytesRead));
                var content = state.sb.ToString();
                receivedPackage = DeserializeFromJson(content);
                if (receivedPackage.message == "CONNECTED")
                {
                    // Send response message.
                    Logs.ShowLog(LogType.CONNECTED, $"Connection with {receivedPackage.sourceName} established.");
                    try
                    {
                        connectedSockets.Add(receivedPackage.sourceName, handler);
                    }
                    catch(Exception e)
                    {
                        Logs.ShowLog(LogType.CONNECTED, $"Router {receivedPackage.sourceName} reconnected.");
                    }
                    SendResponse(handler, content);
                    SendTables(receivedPackage.sourceName);
                }
                else
                {
                    Logs.ShowLog(LogType.ERROR, $"Unknown message received: {content}");
                }
                state.sb.Clear();
                handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                    new AsyncCallback(ReadCallback), state);
            }
            catch (Exception)
            {
                var myKey = connectedSockets.FirstOrDefault(x => x.Value == handler).Key;
                Logs.ShowLog(LogType.ERROR, $"Connection with {myKey} lost.");
                handler.Shutdown(SocketShutdown.Both);
                handler.Close();
            }
        }

        private void SendResponse(Socket handler, string data)
        {
            byte[] responseMessage = Encoding.ASCII.GetBytes(data);
            var myKey = connectedSockets.FirstOrDefault(x => x.Value == handler).Key;
            Logs.ShowLog(LogType.INFO, $"Sending {responseMessage.Length} bytes to {myKey}.");
            handler.Send(responseMessage);
        }

        private void SendMessage(string routerName, Package package)
        {
            try
            {
                var handler = connectedSockets[routerName];
                string json = SerializeToJson(package);
                byte[] byteData = Encoding.ASCII.GetBytes(json);
                Logs.ShowLog(LogType.INFO, $"Sending {byteData.Length} bytes to {routerName}.");
                Console.WriteLine(json);
                handler.Send(byteData);
            }
            catch (KeyNotFoundException)
            {
                Logs.ShowLog(LogType.ERROR, $"Router {routerName} is not connected.");
            }
            catch
            {
                Logs.ShowLog(LogType.ERROR, $"Couldn't send message.");
            }
        }

        public string SerializeToJson(Package package)
        {
            string jsonString;
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
            };
            jsonString = JsonSerializer.Serialize(package, options);

            return jsonString;
        }

        public Package DeserializeFromJson(string serializedString)
        {
            Package package = JsonSerializer.Deserialize<Package>(serializedString);
            return package;
        }

        private void SendTables(string destination)
        {
            var tablesFile = new List<string>();
            switch (destination)
            {
                case "R1":
                    foreach (var row in File.ReadAllLines(R1TablesFilePath))
                    {
                        tablesFile.Add(row);
                    }
                    Package package1 = new Package("Management System", "SENDING-TABLES", tablesFile);
                    SendMessage("R1", package1);
                    break;
                case "R2":
                    foreach (var row in File.ReadAllLines(R2TablesFilePath))
                    {
                        tablesFile.Add(row);
                    }
                    Package package2 = new Package("Management System", "SENDING-TABLES", tablesFile);
                    SendMessage("R2", package2);
                    break;
                case "R3":
                    foreach (var row in File.ReadAllLines(R3TablesFilePath))
                    {
                        tablesFile.Add(row);
                    }
                    Package package3 = new Package("Management System", "SENDING-TABLES", tablesFile);
                    SendMessage("R3", package3);
                    break;
                case "R4":
                    foreach (var row in File.ReadAllLines(R4TablesFilePath))
                    {
                        tablesFile.Add(row);
                    }
                    Package package4 = new Package("Management System", "SENDING-TABLES", tablesFile);
                    SendMessage("R4", package4);
                    break;

            }
        }
    }
}
