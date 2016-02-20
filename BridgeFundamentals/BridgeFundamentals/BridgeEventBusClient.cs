using System.Runtime.Serialization;

namespace Sodes.Bridge.Base
{
    [DataContract]
    public abstract class BridgeEventBusClient : BridgeEventHandlers
    {
        private BridgeEventBus myEventBus;

        public BridgeEventBusClient(BridgeEventBus bus)
        {
            this.myEventBus = bus == null ? BridgeEventBus.MainEventBus : bus;
            //this.myEventBus = bus;
            if (this.myEventBus != null) this.myEventBus.Link(this);
        }

        public BridgeEventBusClient() : this(null)
        {
        }

        protected BridgeEventBus EventBus
        {
            get
            {
                return this.myEventBus;
            }
        }
    }
}
