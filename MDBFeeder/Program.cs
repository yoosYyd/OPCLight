using MDBFeeder.ENTITYs;
using MDBFeeder.TOOLS;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;

namespace MDBFeeder
{
    class Program
    {
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseWindowsService()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<ServiceEntity>();
                });
        public static void RunThis()
        {
            ConfigParser configParser = new ConfigParser();
            string opcUser = "";
            string opcPass = "";
            configParser.GetOPCParams(out opcUser, out opcPass);
            foreach (var mdb in configParser.GetModbusDevices())
            {
                new Thread(
                () =>
                {
                    ModbusOPCFeeder modbus = new ModbusOPCFeeder(mdb, opcUser, opcPass);
                    for (; ; )
                    {
                        modbus.Update();
                    }
                }
                ).Start();
            }
            while(true)
            {
                Thread.Sleep(3600);
            }
            //Console.ReadKey();
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
                RunThis();
            }
            else
            {
                //Console.WriteLine(2);
                CreateHostBuilder(args).Build().Run();
            }
        }
    }
}
