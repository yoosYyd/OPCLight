using ArchiverII.CODE;
using ArchiverII.SHARED;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

namespace ArchiverII
{
    class Program
    {
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseWindowsService()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<Service>();
                });
        static void Start()
        {
            ConfigLoader cLoader = new ConfigLoader();
            SQLtool st = new SQLtool();
            st.PrepareTables(cLoader.GetListOnWatch());
            string opcUser = "";
            string opcPass = "";
            cLoader.GetOPCcredentials(out opcUser, out opcPass);
            OPCwatcher opW = new OPCwatcher(opcUser, opcPass, cLoader.GetListOnWatch());
            opW.Watching(st);
        }
        static void Main(string[] args)
        {
            bool isWindows = Environment.OSVersion.Platform == PlatformID.Win32NT;
            if (!isWindows)
            {
                Console.WriteLine("for run as 'windows service' use system.d");
            }
            bool debug = false;
            if (args.Length > 0)
            {
                debug = args[0].Equals("debug");
            }
            if (debug || !isWindows)
            {
                Start();
            }
            else
            {
                //Console.WriteLine(2);
                CreateHostBuilder(args).Build().Run();
            }
        }
    }
}
