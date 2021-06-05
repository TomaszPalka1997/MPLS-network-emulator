using System;
using System.Collections.Generic;
using System.IO;

namespace DataStructures
{
    public class IlmTable
    {
        // Naszym kluczem dla każdej pary w słowniku jest to, po czym przeszukujemy tablicę,
        // a wartością reszta pól (obudowane klasą reprezentującą wpis dla odpowiedniej tablicy).
        // Kluczem jest string złożony z inLabel, inPort i poppedLabel.
        public Dictionary<string, IlmEntry> entries = new Dictionary<string, IlmEntry>();
        private string rowName;

        public IlmTable(string routerName)
        {
            rowName = routerName + "_ILM";
        }

        public IlmTable(string configFilePath, string routerName)
        {
            rowName = routerName + "_ILM";
            LoadTableFromFile(configFilePath);
        }

        private void LoadTableFromFile(string configFilePath)
        {
            foreach (var row in File.ReadAllLines(configFilePath))
            {
                var splitRow = row.Split(", ");
                if (splitRow[0] != rowName || splitRow.Length<4)
                {
                    continue;
                }
                var entry = new IlmEntry(int.Parse(splitRow[4]));
                var key = splitRow[1] + ", " + splitRow[2] + ", " + splitRow[3];
                entries.Add(key, entry);
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
                var entry = new IlmEntry(int.Parse(splitRow[4]));
                var key = splitRow[1] + ", " + splitRow[2] + ", " + splitRow[3];
                entries.Add(key, entry);
            }
        }

        public void AddRowToTable(string tablePath, string routerName, int inLabelAdd, int inPortAdd, int? poppedLabelAdd, int idAdd)
        {
            string key;
            if (poppedLabelAdd == null)
            {
                key = $"{inLabelAdd}, {inPortAdd}, -";
            }
            else
            {
                key = $"{inLabelAdd}, {inPortAdd}, {poppedLabelAdd}";
            }
            entries.Add(key, new IlmEntry(idAdd));
            using (StreamWriter file = new StreamWriter(tablePath, true))
            {
                if (poppedLabelAdd == null)
                {
                    file.WriteLine(routerName + "_ILM, {0}, {1}, {2}, {3}", inLabelAdd, inPortAdd, "-", idAdd);
                }
                else
                {
                    file.WriteLine(routerName + "_ILM, {0}, {1}, {2}, {3}", inLabelAdd, inPortAdd, poppedLabelAdd, idAdd);
                }
            }
            Logs.ShowLog(LogType.INFO, $"\nSaved {routerName} ILM table to file.\n");
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

                    if (splitRow.Length<4)
                    {
                        counter++;
                        continue;
                    }
                    if ((splitRow[1] + ", " + splitRow[2] + ", " + splitRow[3]) == row)
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
                    Logs.ShowLog(LogType.INFO, "\nDeleted entry.\n");
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
            Console.WriteLine("Index, InLabel, InPort, PoppedLabel, ID");
            foreach (KeyValuePair<string, IlmEntry> kvp in entries)
            {
                Console.WriteLine(i + ". {0}, {1}", kvp.Key, kvp.Value.id);
                i++;
            }
        }
    }
}
