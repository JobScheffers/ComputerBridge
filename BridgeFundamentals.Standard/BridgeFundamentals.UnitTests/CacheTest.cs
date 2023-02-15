using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sodes.Base;
using System.Collections.Generic;

namespace Bridge.Test
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
        private Dictionary<string, object> cache = new Dictionary<string, object>();

        public override void Add(string key, object x)
        {
            cache[key] = x;
        }

        public override object Get(string key)
        {
            return cache[key];
        }

        public static void Init()
        {
            TestCache.Instance = new TestCache();
        }
    }
}
