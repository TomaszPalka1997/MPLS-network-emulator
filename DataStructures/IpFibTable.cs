using System;
using System.Collections.Generic;
using System.IO;

namespace DataStructures
{
    public class IpFibTable
    {
        // Naszym kluczem dla każdej pary w słowniku jest to, po czym przeszukujemy tablicę, a wartością reszta pól (obudowane klasą reprezentującą wpis dla odpowiedniej tablicy).
        public Dictionary<string, IpFibEntry> entries = new Dictionary<string, IpFibEntry>();
        private string rowName;

        public IpFibTable(string routerName)
        {
            rowName = routerName + "_IPFIB";
        }

        public IpFibTable(string configFilePath, string routerName)
        {
            rowName = routerName + "_IPFIB";
            LoadTableFromFile(configFilePath);
        }

        private void LoadTableFromFile(string configFilePath)
        {
            foreach (var row in File.ReadAllLines(configFilePath))
            {
                var splitRow = row.Split(", ");
                if (splitRow[0] != rowName)
                {
                    continue;
                }
                var entry = new IpFibEntry(int.Parse(splitRow[2]));
                entries.Add(splitRow[1], entry);
            }
        }

        public void LoadTable(List<string> tables)
        {
            foreach (var row in tables)
            {
                var splitRow = row.Split(", ");
                if (splitRow[0] != rowName)
                {
                    continue;
                }
                var entry = new IpFibEntry(int.Parse(splitRow[2]));
                entries.Add(splitRow[1], entry);
            }
        }

        public void AddRowToTable(string tablePath, string routerName, string destAddressAdd, int outPortAdd)
        {
            entries.Add(destAddressAdd, new IpFibEntry(outPortAdd));
            using (StreamWriter file = new StreamWriter(tablePath, true))
            {
                file.WriteLine(routerName + "_IPFIB, {0}, {1}", destAddressAdd, outPortAdd);
            }

            Console.WriteLine($"\nSaved {routerName} IP-FIB table to file.\n");
        }

        public void DeleteRowFromTable(string row, string tablePath)
        {
            int counter = 1;
            entries.Remove(row);
            try
            {
                string[] lines = File.ReadAllLines(tablePath);
                foreach (var entry in lines)
                {
                    var splitRow = entry.Split(", ");

                    if (splitRow.Length == 1)
                    {
                        counter++;
                        continue;
                    }

                    if (splitRow[1] == row)
                    {
                        break;
                    }
                    else
                    {
                        counter++;
                    }
                }
                using (StreamWriter writer = new StreamWriter(tablePath))
                {
                    for (int currentLine = 1; currentLine <= lines.Length; ++currentLine)
                    {
                        if (currentLine == counter)
                        {
                            continue;

                        }
                        else
                        {
                            writer.WriteLine(lines[currentLine - 1]);
                        }
                    }
                    Console.WriteLine("Deleted entry.");
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        public void PrintEntries()
        {
            int i = 1;
            Console.WriteLine("Index, DestinationAddress, OutPort");
            foreach (KeyValuePair<string, IpFibEntry> kvp in entries)
            {
                Console.WriteLine(i + ". {0}, {1}", kvp.Key, kvp.Value.outPort);
                i++;
            }
        }
    }
}
