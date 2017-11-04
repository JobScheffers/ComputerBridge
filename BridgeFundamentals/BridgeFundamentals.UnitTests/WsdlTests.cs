using Bridge.Test.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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
        public async Task Serializable_Test1()
        {
            Log.Level = 4;
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
    }
}
