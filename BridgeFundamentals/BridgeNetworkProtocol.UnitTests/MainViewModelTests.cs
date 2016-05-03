#define useOwnHost

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
    public class MainViewModelTests
    {
#if useOwnHost
        private BridgeEventBus hostEventBus;
#endif

        [TestMethod, DeploymentItem("TestData\\WC2005final01.pbn")]
        public void TableManagerClient_Test()
        {
            Log.Level = 1;
            // Comment the next 3 lines if you want to test against a real TableManager
#if useOwnHost
            this.hostEventBus = new BridgeEventBus("TM_Host");
            var host = new TableManagerTcpHost(2000, this.hostEventBus);
            host.OnHostEvent += Host_OnHostEvent;
#endif

            var vms = new SeatCollection<MainViewModel>();
            Parallel.For(0, 4, (i) =>
            {
                Seats s = (Seats)i;
                vms[s] = new MainViewModel();
                vms[s].Connect(s, "localhost", 2000, 120, 1, "Robo" + (s == Seats.North || s == Seats.South ? "NS" : "EW"), false);
            });

            while (!vms[Seats.North].SessionEnd)
            {
                Thread.Sleep(1000);
            }
        }

        private void Host_OnHostEvent(TableManagerHost sender, HostEvents hostEvent, object eventData)
        {
            switch (hostEvent)
            {
                case HostEvents.ReadyForTeams:
                    sender.HostTournament("WC2005final01.pbn");
                    break;
            }
        }

        [TestMethod, DeploymentItem("TestData\\WC2005final01.pbn")]
        public void TableManager2Tables_Test()
        {
            Log.Level = 1;
            var host1 = new TableManagerTcpHost(2000, new BridgeEventBus("Host1"));
            host1.OnHostEvent += Host_OnHostEvent;

            var vms = new SeatCollection<MainViewModel>();
            Parallel.For(0, 4, (i) =>
            {
                Seats s = (Seats)i;
                vms[s] = new MainViewModel();
                vms[s].Connect(s, "localhost", 2000, 120, 1, "Robo" + (s == Seats.North || s == Seats.South ? "NS" : "EW"), false);
            });

            var host2 = new TableManagerTcpHost(2001, new BridgeEventBus("Host2"));
            host2.OnHostEvent += Host_OnHostEvent;

            var vms2 = new SeatCollection<MainViewModel>();
            Parallel.For(0, 4, (i) =>
            {
                Seats s = (Seats)i;
                vms2[s] = new MainViewModel();
                vms2[s].Connect(s, "localhost", 2001, 120, 1, "Robo" + (s == Seats.North || s == Seats.South ? "NS" : "EW"), false);
            });

            while (!vms[Seats.North].SessionEnd)
            {
                Thread.Sleep(1000);
            }
        }
    }
}
