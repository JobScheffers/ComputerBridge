using Microsoft.VisualStudio.TestTools.UnitTesting;
using RoboBridge.TableManager.Client.UI.ViewModel;
using Sodes.Base;
using Sodes.Bridge.Base;
using Sodes.Bridge.Networking;
using System.Threading;
using System.Threading.Tasks;

namespace RoboBridge.TableManager.Client.UI.UnitTests
{
    [TestClass]
    public class ProtocolTests
    {
        private BridgeEventBus hostEventBus;

        [TestMethod]
        public void TableManagerClientAlert_Test()
        {
            Log.Level = 2;
            // Comment the next 3 lines if you want to test against a real TableManager
            this.hostEventBus = new BridgeEventBus("TM_Host");
            var host = new TableManagerTcpHost(2000, this.hostEventBus);
            host.OnHostEvent += Host_OnHostEvent;
            var vms = new SeatCollection<TableManagerTcpClient>();
            Parallel.For(0, 4, (i) =>
            {
                Seats s = (Seats)i;
                vms[s] = new TableManagerTcpClient();
                vms[s].Connect(s, "localhost", 2000, 120, 10, "Robo" + (s == Seats.North || s == Seats.South ? "NS" : "EW"), 4, false);
            });

            while (vms[Seats.North].state != TableManagerProtocolState.Finished)
            {
                Thread.Sleep(1000);
            }
        }

        private void Host_OnHostEvent(TableManagerHost sender, HostEvents hostEvent, Seats seat, string message)
        {
            switch (hostEvent)
            {
                case HostEvents.ReadyForTeams:
                    sender.HostTournament("WC2005final01.pbn");
                    break;
            }
        }
    }
}
