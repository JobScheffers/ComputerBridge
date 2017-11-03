using System.Runtime.Serialization;

namespace Bridge
{
    [DataContract(Name = "BridgeEventBusClient", Namespace = "")]
    public abstract class BridgeEventBusClient : BridgeEventHandlers
    {
        private BridgeEventBus myEventBus;

        public BridgeEventBusClient(BridgeEventBus bus, string name)
        {
            this.Name = name;
            if (bus != null)
            {
                this.myEventBus = bus;
                this.myEventBus.Link(this);
            }
        }

        public BridgeEventBusClient() : this(null, null)
        {
        }

        public string Name { get; set; }

        protected BridgeEventBus EventBus
        {
            get
            {
                return this.myEventBus;
            }
        }

        public override void HandleTournamentStopped()
        {
            base.HandleTournamentStopped();
            this.myEventBus.Unlink(this);
        }
    }
}
