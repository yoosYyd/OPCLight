using MDBFeeder.ENTITYs;
using MDBFeeder.TOOLS;
using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;

namespace MDBFeeder
{
    public class ServiceEntity : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
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
            while (!stoppingToken.IsCancellationRequested)
            {
                //_log.DebugMessage("RUN");
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}