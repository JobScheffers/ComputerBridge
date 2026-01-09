using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bridge.Test
{
    [TestClass]
	public class AuctionTest
	{
        [TestMethod, TestCategory("CI"), TestCategory("Bid")]
        public void Auction_Vergelijkbaar_Pass012_0()
        {
            var target = new Auction(Vulnerable.EW, Seats.East);
            target.Record(Bid.C("1NT"));

            Assert.IsTrue(target.Vergelijkbaar("pass012 05"));
        }

        [TestMethod, TestCategory("CI"), TestCategory("Bid")]
        public void Auction_Vergelijkbaar_Pass012_1()
        {
            var target = new Auction(Vulnerable.EW, Seats.East);
            target.Record(Bid.C("p"));
            target.Record(Bid.C("1NT"));

            Assert.IsTrue(target.Vergelijkbaar("pass012 05"));
        }

        [TestMethod, TestCategory("CI"), TestCategory("Bid")]
        public void Auction_Vergelijkbaar_Pass012_2()
        {
            var target = new Auction(Vulnerable.EW, Seats.East);
            target.Record(Bid.C("p"));
            target.Record(Bid.C("p"));
            target.Record(Bid.C("1NT"));

            Assert.IsTrue(target.Vergelijkbaar("pass012 05"));
        }

        [TestMethod, TestCategory("CI"), TestCategory("Bid")]
        public void Auction_Vergelijkbaar_Pass012_3()
        {
            var target = new Auction(Vulnerable.EW, Seats.East);
            target.Record(Bid.C("p"));
            target.Record(Bid.C("p"));
            target.Record(Bid.C("p"));
            target.Record(Bid.C("1NT"));

            Assert.IsFalse(target.Vergelijkbaar("pass012 05"));
        }

        [TestMethod, TestCategory("CI"), TestCategory("Bid")]
        public void Auction_Vergelijkbaar()
        {
            var target = new Auction(Vulnerable.EW, Seats.East);
            target.Record(Bid.C("p"));
            target.Record(Bid.C("1NT"));

            Assert.IsTrue(target.Vergelijkbaar("pas* 05"));
            Assert.IsTrue(target.Vergelijkbaar("pas* 00 05"));
            Assert.IsFalse(target.Vergelijkbaar("pas* 00 00 05"));
        }

        [TestMethod, TestCategory("CI"), TestCategory("Bid")]
		public void Auction_RecordOk1()
		{
			var target = new Auction(Vulnerable.EW, Seats.East);
			Assert.AreEqual<Vulnerable>(Vulnerable.EW, target.Vulnerability, "Vulnerability");
			Assert.AreEqual<Seats>(Seats.East, target.Dealer, "Dealer");
			Assert.AreEqual<Seats>(Seats.East, target.WhoseTurn, "WhoseTurn");

			target.Record(Bid.C("p"));

			Assert.AreEqual<Seats>(Seats.South, target.WhoseTurn, "WhoseTurn 1");
			Assert.AreEqual<Seats>(Seats.East, target.WhoBid0(0), "WhoBid0 1");
			Assert.AreEqual<Seats>(Seats.East, target.WhoBid(1), "WhoBid 1");

			target.Record(Bid.C("1NT"));

			Assert.AreEqual<Seats>(Seats.West, target.WhoseTurn, "WhoseTurn 2");
			Assert.AreEqual<Seats>(Seats.South, target.WhoBid0(1), "WhoBid0 2");
			Assert.AreEqual<Seats>(Seats.East, target.WhoBid0(0), "WhoBid0 2a");
			Assert.AreEqual<Seats>(Seats.South, target.WhoBid(1), "WhoBid 2");
			Assert.AreEqual<Seats>(Seats.East, target.WhoBid(2), "WhoBid 2a");

			target.Record(Bid.C("x"));
			target.Record(Bid.C("p"));
			target.Record(Bid.C("p"));

			Assert.AreEqual<Seats>(Seats.South, target.WhoseTurn, "WhoseTurn 5");

			target.Record(Bid.C("p"));

			Assert.AreEqual<Seats>(Seats.West, target.WhoseTurn, "WhoseTurn after end of bidding");
			Assert.AreEqual<Seats>(Seats.South, target.Declarer, "Declarer after end of bidding");
		}

		[TestMethod, TestCategory("CI"), TestCategory("Bid")]
		public void Auction_RecordOk2()
		{
			var target = new Auction(Vulnerable.EW, Seats.East);
			target.Record(Bid.C("p"));
			target.Record(Bid.C("1NT"));
			target.Record(Bid.C("p"));
			target.Record(Bid.C("p"));
			target.Record(Bid.C("x"));
			target.Record(Bid.C("xx"));
		}

		[TestMethod, TestCategory("CI"), TestCategory("Bid")]
		public void Auction_RecordOk3()
		{
			var target = new Auction(Vulnerable.EW, Seats.East);
			target.Record(Bid.C("p"));
			target.Record(Bid.C("1NT"));
			target.Record(Bid.C("p"));
			target.Record(Bid.C("p"));
			target.Record(Bid.C("x"));
			target.Record(Bid.C("p"));
			target.Record(Bid.C("p"));
			target.Record(Bid.C("xx"));
		}

		[TestMethod, TestCategory("CI"), TestCategory("Bid")]
		public void Auction_RecordOk4()
		{
			var target = new Auction(Vulnerable.EW, Seats.East);
            Assert.IsFalse(target.Opened);
            Assert.AreEqual<Vulnerable>(Vulnerable.EW, target.Vulnerability);
            target.Record(Bid.C("p"));
            Assert.IsFalse(target.Opened);
            target.Record(Bid.C("1NT"));
            Assert.IsTrue(target.Opened);
            Assert.AreEqual<Seats>(Seats.South, target.Opener);
            Assert.AreEqual<Bid>(Bid.C("1NT"), target.OpeningBid);
            target.Record(Bid.C("p"));
			target.Record(Bid.C("p"));
			target.Record(Bid.C("x"));
			target.Record(Bid.C("xx"));
			target.Record(Bid.C("2C"));
			target.Record(Bid.C("x"));

            Assert.AreEqual<Seats>(Seats.South, target.Opener);
            Assert.AreEqual<Seats>(Seats.North, target.WhoBid(1));
            Assert.AreEqual<Seats>(Seats.West, target.WhoBid(2));
            Assert.AreEqual<Seats>(Seats.West, target.FirstBid(Suits.Clubs));
            Assert.AreEqual<Seats>(Seats.West, target.FirstToBid(Suits.Clubs, Directions.EastWest));
            Assert.AreEqual<Seats>(Seats.South, target.FirstToBid(Suits.NoTrump, Directions.NorthSouth));
            Assert.AreEqual<Seats>(Seats.South, target.FirstNotToPass);
            Assert.IsTrue(target.HasBid(Suits.Clubs, 2));
            Assert.IsFalse(target.HasBid(Suits.Diamonds, 2));
            Assert.IsFalse(target.HasBid(Suits.Clubs, 3));
            Assert.IsFalse(target.Vergelijkbaar("pas* 03"));
            Assert.IsTrue(target.Vergelijkbaar("pas* 05 pas0 36 37 2X ??"));
            Assert.IsFalse(target.Vergelijkbaar("pas* 05 pas1"));
            Assert.IsFalse(target.Vergelijkbaar("pas* 05 pas2 36 pass0or1"));
            Assert.IsTrue(target.Vergelijkbaar("pas* 05 pas2 **"));
            Assert.IsTrue(target.Vergelijkbaar("pas* 05 pas2 NP **"));
            Assert.IsFalse(target.Vergelijkbaar("pas* 05 pas0 36 37 2W 3W"));
            Assert.IsFalse(target.Vergelijkbaar("pas* 05 pas0 36 37 2Y 3Y"));
            Assert.IsFalse(target.Vergelijkbaar("pas* 05 pas0 36 37 2Z 3Z"));
            Assert.IsFalse(target.Vergelijkbaar("pas* 05 pas0 36 37 2M 36"));
            Assert.IsTrue(target.Vergelijkbaar("pas* 05 pas0 36 37 2N 36"));
            Assert.IsFalse(target.StartedWith("pas* 03"));
            Assert.IsTrue(target.StartedWith("pas* 05"));
            Assert.IsTrue(target.StartedWith("pass012 05"));
            Assert.AreEqual<int>(7, target.WanneerGeboden("1NT"));
            Assert.AreEqual<int>(7, target.WanneerGeboden(1, Suits.NoTrump));
            Assert.AreEqual<Suits>(Suits.NoTrump, target.VierdeKleur);
            Assert.IsFalse(target.WordtVierdeKleur(Suits.Hearts));
//            Assert.AreEqual(@"West  North East  South
//-     -     Pass  1NT
//Pass  Pass  x     xx
//2C    x     ", target.ToString());
        }

        [TestMethod, TestCategory("CI"), TestCategory("Bid")]
        public void Auction_FirstToBid1()
        {
            var target = new Auction(Vulnerable.NS, Seats.East);
            target.Record(Bid.C("p"));
            target.Record(Bid.C("1C"));
            target.Record(Bid.C("p"));
            target.Record(Bid.C("1S"));
            target.Record(Bid.C("p"));
            target.Record(Bid.C("1NT"));

            Assert.AreEqual<Seats>(Seats.South, target.FirstToBid(Suits.NoTrump, Directions.NorthSouth));

            target.Record(Bid.C("p"));
            target.Record(Bid.C("2C"));
            target.Record(Bid.C("p"));
            target.Record(Bid.C("2D"));
            target.Record(Bid.C("p"));
            target.Record(Bid.C("4NT"));

            Assert.AreEqual<Seats>(Seats.South, target.FirstToBid(Suits.NoTrump, Directions.NorthSouth));

            target.Record(Bid.C("p"));
            target.Record(Bid.C("5D"));
            target.Record(Bid.C("p"));

            Assert.AreEqual<Seats>(Seats.South, target.FirstToBid(Suits.NoTrump, Directions.NorthSouth));
        }

        [TestMethod, TestCategory("CI"), TestCategory("Bid"), ExpectedException(typeof(AuctionException))]
		public void Auction_RecordFault1()
		{
			var target = new Auction(Vulnerable.EW, Seats.East);
			target.Record(Bid.C("p"));
			target.Record(Bid.C("1NT"));
			target.Record(Bid.C("p"));
			target.Record(Bid.C("x"));
		}

		[TestMethod, TestCategory("CI"), TestCategory("Bid"), ExpectedException(typeof(AuctionException))]
		public void Auction_RecordFault2()
		{
			var target = new Auction(Vulnerable.EW, Seats.East);
			target.Record(Bid.C("p"));
			target.Record(Bid.C("1NT"));
			target.Record(Bid.C("x"));
			target.Record(Bid.C("x"));
		}

		[TestMethod, TestCategory("CI"), TestCategory("Bid"), ExpectedException(typeof(AuctionException))]
		public void Auction_RecordFault3()
		{
			var target = new Auction(Vulnerable.EW, Seats.East);
			target.Record(Bid.C("p"));
			target.Record(Bid.C("1NT"));
			target.Record(Bid.C("x"));
			target.Record(Bid.C("p"));
			target.Record(Bid.C("xx"));
		}

		[TestMethod, TestCategory("CI"), TestCategory("Bid"), ExpectedException(typeof(AuctionException))]
		public void Auction_RecordFault4()
		{
			var target = new Auction(Vulnerable.EW, Seats.East);
			target.Record(Bid.C("x"));
		}

		[TestMethod, TestCategory("CI"), TestCategory("Bid"), ExpectedException(typeof(AuctionException))]
		public void Auction_RecordFault5()
		{
			var target = new Auction(Vulnerable.EW, Seats.East);
			target.Record(Bid.C("xx"));
		}

		[TestMethod, TestCategory("CI"), TestCategory("Bid"), ExpectedException(typeof(AuctionException))]
		public void Auction_RecordFault6()
		{
			var target = new Auction(Vulnerable.EW, Seats.East);
			target.Record(Bid.C("p"));
			target.Record(Bid.C("1NT"));
			target.Record(Bid.C("xx"));
		}

		[TestMethod, TestCategory("CI"), TestCategory("Bid"), ExpectedException(typeof(AuctionException))]
		public void Auction_RecordFault7()
		{
			var target = new Auction(Vulnerable.EW, Seats.East);
			target.Record(Bid.C("p"));
			target.Record(Bid.C("1NT"));
			target.Record(Bid.C("p"));
			target.Record(Bid.C("xx"));
		}

		[TestMethod, TestCategory("CI"), TestCategory("Bid"), TestCategory("CI"), TestCategory("Bid")]
		public void Auction_Declarer_4Pass()
		{
			var target = new Auction(Vulnerable.EW, Seats.East);
			target.Record(Bid.C("p"));
			target.Record(Bid.C("p"));
			target.Record(Bid.C("p"));
			target.Record(Bid.C("p"));
			Assert.AreEqual<Seats>(Seats.East, target.Declarer);
		}
	}
}
