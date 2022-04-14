using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TotaLLOGER
{
    public class NetLog
    {
        private string loggingDir;
        public NetLog(string loggingDir)
        {
            this.loggingDir = loggingDir;
        }
        public NetLog()
        {
            loggingDir = AppDomain.CurrentDomain.BaseDirectory + "LOGS\\";
        }
        public void Run(object stoppingObject)
        {
            new Thread(() =>
            {
                try
                {
                    IPEndPoint listenPort = new IPEndPoint(IPAddress.Any, 45454);
                    UdpClient listener = new UdpClient(listenPort);
                    if (stoppingObject == null)
                    {
                        while (true)
                        {
                            string message = Encoding.Default.GetString(listener.Receive(ref listenPort));
                            Console.WriteLine(message);
                            using (StreamWriter w = File.AppendText(loggingDir + DateTime.Now.ToString("d", CultureInfo.CurrentCulture) + "_LOG.txt"))
                            {
                                w.WriteLine(message + " " + AppDomain.CurrentDomain.BaseDirectory);
                            }
                        }
                    }
                    else
                    {
                        CancellationToken stoppingToken = (CancellationToken)stoppingObject;
                        while (!stoppingToken.IsCancellationRequested)
                        {
                            string message = Encoding.Default.GetString(listener.Receive(ref listenPort));
                            Console.WriteLine(message);
                            DateTime now = DateTime.Now;
                            using (StreamWriter w = File.AppendText(loggingDir + now.Day.ToString() 
                                +"_"+now.Month.ToString()+"_"+ now.Year.ToString()+"_LOG.txt"))
                            {
                                w.WriteLine(message);
                            }
                        }
                    }
                }
                catch(Exception ex)
                {
                    using (StreamWriter w = File.AppendText("innerLog.txt"))
                    {
                        w.WriteLine(ex.Message);
                    }
                }
            }).Start();
        }
    }
}
