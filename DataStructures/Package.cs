using System.Collections.Generic;

namespace DataStructures
{
    public class Package
    {
        public string sourceName { get; set; }
        public string destAddress { get; set; }
        public int incomingPort { get; set; }
        public int destPort { get; set; }
        public List<int> labels { get; set; }
        public string message { get; set; }
        public List<string> tablesFile { get; set; }

        public Package()
        {
            sourceName = "";
            destAddress = "";
            incomingPort = 0;
            destPort = 0;
            labels = new List<int>();
            message = "";
            tablesFile = new List<string>();
        }

        public Package(string sourceName, string destAddress, int destPort, string message)
        {
            this.sourceName = sourceName;
            this.destAddress = destAddress;
            incomingPort = 0;
            this.destPort = destPort;
            labels = new List<int>();
            this.message = message;
            tablesFile = new List<string>();
        }

        public Package(string sourceName, int incomingPort, string destAddress, int destPort, string message)
        {
            this.sourceName = sourceName;
            this.incomingPort = incomingPort;
            this.destAddress = destAddress;
            this.destPort = destPort;
            labels = new List<int>();
            this.message = message;
            tablesFile = new List<string>();
        }

        public Package(string sourceName, string message, List<string> tablesFile)
        {
            this.sourceName = sourceName;
            this.message = message;
            this.tablesFile = tablesFile;
        }
    }
}
