using ArchiverII.CODE;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ArchiverII
{
    public class Service : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            new Thread(
                () =>
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
                ).Start();
            while (!stoppingToken.IsCancellationRequested)
            {
                //_log.DebugMessage("RUN");
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}
