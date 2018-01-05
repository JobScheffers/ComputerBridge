using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bridge.Test.Helpers
{
	public abstract class TestBase
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
