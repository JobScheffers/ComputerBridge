using Bridge.Test.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Bridge.Test
{
    [TestClass]
    public class WsdlTests : BridgeTestBase
    {
        [TestMethod, DeploymentItem("TestData\\uBidParscore.pbn")]
        public async Task Serializable_PbnTournament()
        {
            var t = await TournamentLoader.LoadAsync(File.OpenRead("uBidParscore.pbn"));
            var serializer = new DataContractSerializer(typeof(PbnTournament));
            using (var stream = File.Create("tournament.xml"))
            {
                using (var xdw = XmlDictionaryWriter.CreateTextWriter(stream, Encoding.UTF8))
                {
                    serializer.WriteObject(xdw, t);
                }
            }

            var tournamentXml = await File.OpenText("tournament.xml").ReadToEndAsync();
            Debug.WriteLine(tournamentXml);

            Tournament t2;
            using (var fs = File.OpenRead("tournament.xml"))
            {
                var reader = XmlDictionaryReader.CreateTextReader(fs, new XmlDictionaryReaderQuotas());
                t2 = (Tournament)serializer.ReadObject(reader);
            }

            Assert.AreEqual(t.Boards[0], t2.Boards[0]);
            //Assert.AreEqual(t.Boards[0].DoubleDummyTricks[Seats.North][Suits.Spades], t2.Boards[0].DoubleDummyTricks[Seats.North][Suits.Spades]);
        }

        [TestMethod]
        public async Task Serializable_OnlineTournament()
        {
            var t = new OnlineTournament { Id = 1, Name = "test", Ranking = new Collection<OnlineTournamentResult>(), Scoring = Scorings.scButler };
            t.Ranking.Add(new OnlineTournamentResult { Average = 1.3, Boards = 6, Country = "NL", Participant = "test", Rank = 1, UserId = Guid.NewGuid() });

            var serializer = new DataContractSerializer(typeof(OnlineTournament));
            using (var stream = File.Create("OnlineTournament.xml"))
            {
                using (var xdw = XmlDictionaryWriter.CreateTextWriter(stream, Encoding.UTF8))
                {
                    serializer.WriteObject(xdw, t);
                }
            }

            var tournamentXml = await File.OpenText("OnlineTournament.xml").ReadToEndAsync();
            Debug.WriteLine(tournamentXml);

            OnlineTournament t2;
            using (var fs = File.OpenRead("OnlineTournament.xml"))
            {
                var reader = XmlDictionaryReader.CreateTextReader(fs, new XmlDictionaryReaderQuotas());
                t2 = (OnlineTournament)serializer.ReadObject(reader);
            }

            Assert.AreEqual(t.Name, t2.Name);
            Assert.AreEqual(t.Ranking.Count, t2.Ranking.Count);
        }
    }
}
