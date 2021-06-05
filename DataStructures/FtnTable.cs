using System;
using System.Collections.Generic;
using System.IO;

namespace DataStructures
{
    public class FtnTable
    {
        // Naszym kluczem dla ka¿dej pary w s³owniku jest to, po czym przeszukujemy tablicê,
        // a wartoœci¹ reszta pól (obudowane klas¹ reprezentuj¹c¹ wpis dla odpowiedniej tablicy).
        // Zatem w przypadku tablicy FTN kluczem bêdzie FEC, a wartoœci¹ ID (jedyne pole w klasie FtnEntry).
        public Dictionary<int, FtnEntry> entries = new Dictionary<int, FtnEntry>();
        private string rowName;

        public FtnTable(string routerName)
        {
            rowName = routerName + "_FTN";
        }

        public FtnTable(string configFilePath, string routerName)
        {
            rowName = routerName + "_FTN";
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
                var entry = new FtnEntry(int.Parse(splitRow[2]));
                entries.Add(int.Parse(splitRow[1]), entry);  
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
                var entry = new FtnEntry(int.Parse(splitRow[2]));
                entries.Add(int.Parse(splitRow[1]), entry);
            }
        }

        public void AddRowToTable(string tablePath, string routerName, int fecAdd, int idAdd)
        {
            entries.Add(fecAdd, new FtnEntry(idAdd));
            using (StreamWriter file = new StreamWriter(tablePath, true))
            {
                file.WriteLine(routerName + "_FTN, {0}, {1}", fecAdd, idAdd);
            }

            Console.WriteLine($"\nSaved {routerName} FTN table to file.\n");
        }

        public void DeleteRowFromTable(string row, string tablePath)
        {
            int counter = 1;
            entries.Remove(int.Parse(row));
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
                    Console.WriteLine("Deleted entry from.");
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
            Console.WriteLine("Index, FEC, ID");
            foreach (KeyValuePair<int, FtnEntry> kvp in entries)
            {
                Console.WriteLine(i + ". {0}, {1}", kvp.Key, kvp.Value.id);
                i++;
            }
        }
    }
}
