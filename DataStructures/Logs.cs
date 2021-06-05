using System;
using System.Text;

namespace DataStructures
{
    public enum LogType {CONNECTED, ERROR, INFO }
    static public class Logs
    {
        public static void ShowLog(LogType type, string message)
        {
            StringBuilder sb = new StringBuilder();
            switch (type)
            {
                case LogType.CONNECTED:
                    sb.Append("CONNECTED :: ");
                    break;
                case LogType.ERROR:
                    sb.Append("ERROR :: ");
                    break;
                case LogType.INFO:
                    sb.Append("INFO :: ");
                    break;
            }
            sb.Append(DateTime.Now.ToString());
            sb.Append(" :: ");
            sb.Append(message);
            //sb.Append(Environment.NewLine);
            Console.WriteLine(sb.ToString());
        }
    }
}
