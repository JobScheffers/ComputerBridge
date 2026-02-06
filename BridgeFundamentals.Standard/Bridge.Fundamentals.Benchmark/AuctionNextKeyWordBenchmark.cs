using BenchmarkDotNet.Attributes;
using System.Text;
using Bridge;

namespace Bridge.Fundamentals.Benchmark
{
    [MemoryDiagnoser]
    public class AuctionNextKeyWordBenchmark
    {
        private const int Iterations = 10000;
        private Auction auction;
        private string pattern;

        [GlobalSetup]
        public void Setup()
        {
            this.auction = new Auction(Vulnerable.Neither, Seats.North);
            // prepare some bids
            this.auction.Record(AuctionBid.Parse("1H"));
            this.auction.Record(AuctionBid.Parse("p"));
            this.auction.Record(AuctionBid.Parse("2C"));
            this.auction.Record(AuctionBid.Parse("p"));
            this.auction.Record(AuctionBid.Parse("3S"));
            this.auction.Record(AuctionBid.Parse("p"));
            var sb = new StringBuilder();
            string segment = "PASS* 00 ?? NP PAS0 PASS1 PASS2 PASS0OR1 PASS012 W1 X2 Y3 Z4 M2 N3 12 13 ** ";
            for (int i = 0; i < 200; i++)
                sb.Append(segment);
            this.pattern = sb.ToString();
        }

        [Benchmark]
        public bool ParsePattern()
        {
            bool r = false;
            for (int i = 0; i < Iterations; i++)
            {
                // use the same pattern reference to avoid massive allocations from string.Copy
                r ^= this.auction.Vergelijkbaar(this.pattern);
            }

            return r;
        }
    }
}