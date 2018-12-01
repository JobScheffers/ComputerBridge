using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bridge.Test.Helpers
{
    [TestClass]
    public abstract class BridgeTestBase : TestBase
    {
        static BridgeTestBase()
        {
            Log.Initialize(0, new TestLogger());
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
}
