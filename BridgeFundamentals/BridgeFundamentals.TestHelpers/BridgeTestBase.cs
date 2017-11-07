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

        public void ClassInitialize(TestContext testContext)
        {
            TestDeployment.Init(testContext);
        }

        [TestInitialize]
        public void TestInitialize()
        {
        }

        [TestCleanup]
        public void TestCleanup()
        {
            TestDeployment.Cleanup();
        }
    }
}
