﻿using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bridge.Test.Helpers
{
    [TestClass]
    public abstract class BridgeTestBase : TestBase
    {
        static BridgeTestBase()
        {
            TestLogger.Initialize();
        }

        public static void ClassInitialize(TestContext testContext)
        {
            TestDeployment.Init(testContext);
        }

        [TestInitialize]
        public void TestInitialize()
        {
            // make sure that all event subscriptions are gone
            BridgeEventBus.MainEventBus = new BridgeEventBus("MainEventBus");
        }

        [TestCleanup]
        public void TestCleanup()
        {
            TestDeployment.Cleanup();
        }
    }

    public abstract class TcpTestBase : BridgeTestBase
    {
        private static int nextPort = 3000;
        private static readonly object locker = new();

        protected static int GetNextPort()
        {
            lock (locker)
            {
                return nextPort++;
            }
        }
    }
}
