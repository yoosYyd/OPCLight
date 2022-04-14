using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace TotaLLOGER
{
    class Program
    {
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseWindowsService()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<ServiceClass>();
                });
        static void Main(string[] args)
        {
            bool isWindows = Environment.OSVersion.Platform == PlatformID.Win32NT;
            if (!isWindows)
            {
                Console.WriteLine("for run as 'windows service' use system.d");
            }
            bool debug = false;
            //bool debug = true;
            if (args.Length > 0)
            {
                debug = args[0].Equals("debug");
            }
            if (debug || !isWindows)
            {
                Console.WriteLine(1);
                NetLog log = new NetLog();
                log.Run(null);
            }
            else
            {
                Console.WriteLine(2);
                CreateHostBuilder(args).Build().Run();
            }
            Console.ReadKey();
        }
    }
}
