using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bridge.Test.Helpers
{
	public class TestBase
	{
		private TestContext testContextInstance;

		public TestContext TestContext
		{
			get
			{
				return testContextInstance;
			}
			set
			{
				testContextInstance = value;
			}
		}
	}
}
