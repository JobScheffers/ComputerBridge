using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Runtime.Serialization;
using System.IO;

namespace Bridge.Test
{
    [TestClass]
	public class BoardTest
	{

        [TestMethod]
        public void Board_CompareTest()
        {
            var board1 = new Board2();
            var board2 = new Board2();
            Assert.IsTrue(board1.Equals(board2));
            var board3 = new Board2(@"E,Both
        s 753
        h KT53
        d T654
        c K6
s T984           s AKQJ62
h 842            h AQ7
d AQ             d 3
c AQJ7           c 543
        s 
        h J96
        d K9872
        c T982
");
            Assert.IsFalse(board1.Equals(board3));
        }
    }
}
