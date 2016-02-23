using Microsoft.VisualStudio.TestTools.UnitTesting;
using RoboBridge.TableManager.Client.UI.ViewModel;
using Sodes.Bridge.Base;
using Sodes.Bridge.Networking;
using System.Threading;

namespace RoboBridge.TableManager.Client.UI.UnitTests
{
    [TestClass]
    public class MainViewModelTests
    {
        private BridgeEventBus hostEventBus;

        [TestMethod]
        public void TableManagerClient_Test()
        {
            // Comment the next 3 lines if you want to test against a real TableManager
            this.hostEventBus = new BridgeEventBus();
            var host = new TableManagerTcpHost(2000, this.hostEventBus);
            host.OnHostEvent += Host_OnHostEvent;

            var vms = new SeatCollection<MainViewModel>();
            for (Seats s = Seats.North; s <= Seats.West; s++)
            {
                vms[s] = new MainViewModel();
                vms[s].Connect(s, "localhost", 2000, 120, 10, "Robo" + (s == Seats.North || s == Seats.South ? "NS" : "EW"), 4, false).Wait();
            }

            while (!vms[Seats.North].SessionEnd)
            {
                Thread.Sleep(1000);
            }
        }

        private void Host_OnHostEvent(TableManagerHost sender, HostEvents hostEvent, Seats seat, string message)
        {
            switch (hostEvent)
            {
                case HostEvents.Seated:
                    break;
                case HostEvents.ReadyForTeams:
                    break;
                case HostEvents.ReadyToStart:
                    sender.BroadCast("Start of board");
                    break;
                case HostEvents.ReadyForDeal:
                    this.hostEventBus.HandleBoardStarted(1, Seats.North, Vulnerable.Neither);
                    break;
                case HostEvents.ReadyForCards:
                    switch (seat)
                    {
                        case Seats.North:
                            sender.WriteData(Seats.North, "North's cards : S A K J 6.H A K J.D 8 6 2.C A 7 6.");
                            break;
                        case Seats.East:
                            sender.WriteData(Seats.East, "East's cards : S T 8 7 3.H 7 4.D Q 5 4.C J 9 8 5.");
                            break;
                        case Seats.South:
                            sender.WriteData(Seats.South, "South's cards : S 9 5 4.H 5 3 2.D A K 9 3.C Q T 3.");
                            break;
                        case Seats.West:
                            sender.WriteData(Seats.West, "West's cards : S Q 2.H Q T 9 8 6.D J T 7.C K 4 2.");
                            break;
                    }
                    break;
                case HostEvents.ReadyForDummiesCards:
                    string cards = string.Empty;
                    switch (seat)   // seat == dummy
                    {
                        case Seats.North:
                            cards = "Dummy's cards : S A K J 6.H A K J.D 8 6 2.C A 7 6.";
                            break;
                        case Seats.East:
                            cards = "Dummy's cards : S T 8 7 3.H 7 4.D Q 5 4.C J 9 8 5.";
                            break;
                        case Seats.South:
                            cards = "Dummy's cards : S 9 5 4.H 5 3 2.D A K 9 3.C Q T 3.";
                            break;
                        case Seats.West:
                            cards = "Dummy's cards : S Q 2.H Q T 9 8 6.D J T 7.C K 4 2.";
                            break;
                    }

                    for (Seats s = Seats.North; s <= Seats.West; s++)
                    {
                        if (s != seat)
                        {
                            sender.WriteData(s, cards);
                        }
                    }

                    break;
                default:
                    break;
            }
        }
    }
}
