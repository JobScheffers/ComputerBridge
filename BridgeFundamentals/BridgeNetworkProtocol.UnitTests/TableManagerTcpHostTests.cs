using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sodes.Bridge.Networking;
using Sodes.Bridge.Base;

namespace BridgeNetworkProtocol.UnitTests
{
    [TestClass]
    public class TableManagerTcpHostTests
    {
        [TestMethod]
        public void TableManagerTcpHost_Run()
        {
            var host = new TableManagerTcpHost(2000, new BridgeEventBus());

        }
    }
}
