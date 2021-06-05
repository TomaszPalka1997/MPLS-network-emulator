using System;
using System.Collections.Generic;
using System.IO;

namespace DataStructures
{
    public class NhlfeTable
    {
        // Naszym kluczem dla każdej pary w słowniku jest to, po czym przeszukujemy tablicę,
        // a wartością reszta pól (obudowane klasą reprezentującą wpis dla odpowiedniej tablicy).
        // Kluczem jest ID.
        public Dictionary<int, NhlfeEntry> entries = new Dictionary<int, NhlfeEntry>();
        private string rowName;

        public NhlfeTable(string routerName)
        {
            rowName = routerName + "_NHLFE";
        }

        public NhlfeTable(string configFilePath, string routerName)
        {
            rowName = routerName + "_NHLFE";
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
                if (splitRow[3] == "-" && splitRow[4] == "-" && splitRow[5] == "-")
                {
                    var entry = new NhlfeEntry(splitRow[2], null, null, null);
                    entries.Add(int.Parse(splitRow[1]), entry);
                }
                else if (splitRow[3] == "-")
                {
                    var entry = new NhlfeEntry(splitRow[2], null, int.Parse(splitRow[4]), int.Parse(splitRow[5]));
                    entries.Add(int.Parse(splitRow[1]), entry);
                } 
                else if (splitRow[4] == "-")
                {
                    var entry = new NhlfeEntry(splitRow[2], int.Parse(splitRow[3]), null, int.Parse(splitRow[5]));
                    entries.Add(int.Parse(splitRow[1]), entry);
                }
                else if (splitRow[5] == "-")
                {
                    var entry = new NhlfeEntry(splitRow[2], int.Parse(splitRow[3]), int.Parse(splitRow[4]), null);
                    entries.Add(int.Parse(splitRow[1]), entry);
                }
                else
                {
                    Console.WriteLine("Unknown NHLFE entry.");
                }
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
                if (splitRow[3] == "-" && splitRow[4] == "-" && splitRow[5] == "-")
                {
                    var entry = new NhlfeEntry(splitRow[2], null, null, null);
                    entries.Add(int.Parse(splitRow[1]), entry);
                }
                else if (splitRow[3] == "-")
                {
                    var entry = new NhlfeEntry(splitRow[2], null, int.Parse(splitRow[4]), int.Parse(splitRow[5]));
                    entries.Add(int.Parse(splitRow[1]), entry);
                }
                else if (splitRow[4] == "-")
                {
                    var entry = new NhlfeEntry(splitRow[2], int.Parse(splitRow[3]), null, int.Parse(splitRow[5]));
                    entries.Add(int.Parse(splitRow[1]), entry);
                }
                else if (splitRow[5] == "-")
                {
                    var entry = new NhlfeEntry(splitRow[2], int.Parse(splitRow[3]), int.Parse(splitRow[4]), null);
                    entries.Add(int.Parse(splitRow[1]), entry);
                }
                else
                {
                    Console.WriteLine("Unknown NHLFE entry.");
                }
            }
        }

        public void AddRowToTable(string tablePath, string routerName, int idAdd, string operationAdd, int? outLabelAdd, int? outPortAdd, int? nextIdAdd)
        {
            entries.Add(idAdd, new NhlfeEntry(operationAdd, outLabelAdd, outPortAdd, nextIdAdd));
            using (StreamWriter file = new StreamWriter(tablePath, true))
            {
                if (outLabelAdd == null && outPortAdd == null && nextIdAdd == null)
                {
                    file.WriteLine(routerName + "_NHLFE, {0}, {1}, {2}, {3}, {4}", idAdd, operationAdd, "-", "-", "-");
                }
                else if (outLabelAdd == null)
                {
                    file.WriteLine(routerName + "_NHLFE, {0}, {1}, {2}, {3}, {4}", idAdd, operationAdd, "-", outPortAdd, nextIdAdd);
                }
                else if (outPortAdd == null)
                {
                    file.WriteLine(routerName + "_NHLFE, {0}, {1}, {2}, {3}, {4}", idAdd, operationAdd, outLabelAdd, "-", nextIdAdd);
                }
                else if (nextIdAdd == null)
                {
                    file.WriteLine(routerName + "_NHLFE, {0}, {1}, {2}, {3}, {4}", idAdd, operationAdd, outLabelAdd, outPortAdd, "-");
                }
                else
                {
                    file.WriteLine(routerName + "_NHLFE, {0}, {1}, {2}, {3}, {4}", idAdd, operationAdd, outLabelAdd, outPortAdd, nextIdAdd);
                }

            }

            Console.WriteLine($"\nSaved {routerName} NHLFE table to file.\n");
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
            Console.WriteLine("Index, ID, Operation, OutLabel, OutPort, NextID");
            foreach (KeyValuePair<int, NhlfeEntry> kvp in entries)
            {
                if (kvp.Value.outLabel == null && kvp.Value.outPort == null && kvp.Value.nextId == null)
                {
                    Console.WriteLine(i + ". {0}, {1}, {2}, {3}, {4}", kvp.Key, kvp.Value.operation, "-", "-", "-");
                    i++;
                }
                else if (kvp.Value.outLabel == null)
                {
                    Console.WriteLine(i + ". {0}, {1}, {2}, {3}, {4}", kvp.Key, kvp.Value.operation, "-", kvp.Value.outPort, kvp.Value.nextId);
                    i++;
                }
                else if (kvp.Value.outPort == null)
                {
                    Console.WriteLine(i + ". {0}, {1}, {2}, {3}, {4}", kvp.Key, kvp.Value.operation, kvp.Value.outLabel, "-", kvp.Value.nextId);
                    i++;
                }
                else if (kvp.Value.nextId == null)
                {
                    Console.WriteLine(i + ". {0}, {1}, {2}, {3}, {4}", kvp.Key, kvp.Value.operation, kvp.Value.outLabel, kvp.Value.outPort, "-");
                    i++;
                }
                else
                {
                    Console.WriteLine(i + ". {0}, {1}, {2}, {3}, {4}", kvp.Key, kvp.Value.operation, kvp.Value.outLabel, kvp.Value.outPort, kvp.Value.nextId);
                    i++;
                }
                
            }
        }
    }
}