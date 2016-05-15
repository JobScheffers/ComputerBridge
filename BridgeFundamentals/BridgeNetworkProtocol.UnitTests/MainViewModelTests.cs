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

        [TestMethod, DeploymentItem("TestData\\WC2005final01.pbn")]
        public void TableManager_Client_Test()
        {
            Log.Level = 1;
            // Comment the next 3 lines if you want to test against a real TableManager
#if useOwnHost
            var host = new TestHost(2000, new BridgeEventBus("TM_Host"));
            host.OnHostEvent += Host_OnHostEvent;
#endif

            var vms = new SeatCollection<MainViewModel>();
            Parallel.For(0, 4, (i) =>
            {
                Seats s = (Seats)i;
                vms[s] = new MainViewModel();
                vms[s].Connect(s, "localhost", 2000, 120, 1, "Robo" + (s == Seats.North || s == Seats.South ? "NS" : "EW"), false);
            });

            host.ready.WaitOne();
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
        public void TableManager_2Tables_Test()
        {
            Log.Level = 1;
            var host1 = new TestHost(2000, new BridgeEventBus("Host1"));
            host1.OnHostEvent += Host_OnHostEvent;

            var vms = new SeatCollection<MainViewModel>();
            Parallel.For(0, 4, (i) =>
            {
                Seats s = (Seats)i;
                vms[s] = new MainViewModel();
                vms[s].Connect(s, "localhost", 2000, 120, 1, "Robo" + (s == Seats.North || s == Seats.South ? "NS" : "EW"), false);
            });

            var host2 = new TestHost(2001, new BridgeEventBus("Host2"));
            host2.OnHostEvent += Host_OnHostEvent;

            var vms2 = new SeatCollection<MainViewModel>();
            Parallel.For(0, 4, (i) =>
            {
                Seats s = (Seats)i;
                vms2[s] = new MainViewModel();
                vms2[s].Connect(s, "localhost", 2001, 120, 1, "Robo" + (s == Seats.North || s == Seats.South ? "NS" : "EW"), false);
            });

            host1.ready.WaitOne();
        }

        private class TestHost : TableManagerTcpHost
        {
            public TestHost(int port, BridgeEventBus bus) : base(port, bus)
            {
            }

            public ManualResetEvent ready = new ManualResetEvent(false);

            protected override void Stop()
            {
                this.ready.Set();
            }

            protected override void ExplainBid(Seats source, Bid bid)
            {
                bid.UnAlert();
            }
        }
    }
}
