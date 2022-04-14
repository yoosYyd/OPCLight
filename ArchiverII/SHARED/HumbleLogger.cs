using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ArchiverII.SHARED
{
    class HumbleLogger
    {
        private static HumbleLogger instance = null;
        private static readonly object padlock = new object();
        private ConcurrentBag<string> messages = new ConcurrentBag<string>();
        private string LogID = "{OPC_Archiver}";
        private string logServer = "127.0.0.1";
        private int logPort = 45454;
        private HumbleLogger()
        {
            new Thread(
                () =>
                {
                    Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                    IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(logServer), logPort);
                    //string logPath = AppDomain.CurrentDomain.BaseDirectory + "log.txt";
                    for (; ; )
                    {
                        if (!messages.IsEmpty)
                        {
                            /*string message = "";                       
                            if (messages.TryTake(out message))
                            {
                                File.AppendAllText(logPath, message);
                            }*/
                            string message = "";
                            if (messages.TryTake(out message))
                            {
                                byte[] send_buffer = Encoding.ASCII.GetBytes(LogID + message);
                                sock.SendTo(send_buffer, endPoint);
                            }
                        }
                        else
                        {
                            Thread.Sleep(150);
                        }
                    }
                }
                ).Start();
        }
        public static HumbleLogger Instance
        {
            get
            {
                lock (padlock)
                {
                    if (instance == null)
                    {
                        instance = new HumbleLogger();
                    }
                    return instance;
                }
            }
        }
        public void ErrorMessage(string msg)
        {
            string insert = "ERROR (" + DateTime.Now.ToString() + "): " + msg + "\r\n";
            messages.Add(insert);
        }
        public void DebugMessage(string msg)
        {
            string insert = "DEBUG (" + DateTime.Now.ToString() + "): " + msg + "\r\n";
            messages.Add(insert);
        }
        public void AddMultipleErrors(List<string> toAdd)
        {
            foreach (var element in toAdd)
            {
                messages.Add(element);
            }
        }
    }
}
