using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Runtime.Serialization;
using System.IO;

namespace Bridge.Test
{
	[TestClass]
	public class BoardResultTest
	{

        [TestMethod]
        public void BoardResult_SerializeTest()
        {
            Guid user = Guid.Parse("4C0856A8-BBBF-4F78-8FF9-B7C161F470AC");
            var board = new Board2();
            board.Vulnerable = Vulnerable.Both;

            var participant = new Participant(new SeatCollection<string>(new string[4] { "Robo", "", "NewUser", "" }));
            var newResult = new BoardResult("", board, participant);
            newResult.Auction = new Auction(board.Vulnerable, board.Dealer);
            newResult.Auction.Record(Bid.C("3NT"));
            newResult.Auction.Record(Bid.C("p"));
            newResult.Auction.Record(Bid.C("p"));
            newResult.Auction.Record(Bid.C("p"));
            newResult.Play = new PlaySequence(new Contract("3NT", Seats.South, board.Vulnerable), 13);
            newResult.Play.Record(Suits.Clubs, Ranks.Seven);

            var s = new DataContractSerializer(typeof(BoardResult));
            var m = new MemoryStream();
            s.WriteObject(m, newResult);
            m.Seek(0, SeekOrigin.Begin);
            BoardResult r = s.ReadObject(m) as BoardResult;

            Assert.AreEqual<string>(newResult.TeamName, r.TeamName);
        }
    }
}
