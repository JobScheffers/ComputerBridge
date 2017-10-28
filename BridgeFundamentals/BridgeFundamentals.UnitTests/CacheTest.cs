using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sodes.Base;

namespace Sodes.Bridge.Base.Test
{
	[TestClass]
	public class CacheTest
	{
        [TestMethod, TestCategory("CI"), TestCategory("Bid")]
        public void Cache_Test1()
        {
            TestCache.Init();
            var x = "Hello";
            TestCache.Instance.Add("test", x);
            var y = (string)TestCache.Instance.Get("test");
            Assert.AreEqual(x, y);
        }
	}

    public class TestCache : Cache
    {

        public override void Add(string key, object x)
        {
            throw new NotImplementedException();
        }

        public override object Get(string key)
        {
            throw new NotImplementedException();
        }

        public static void Init()
        {
            TestCache.Instance = new TestCache();
        }
    }
}
