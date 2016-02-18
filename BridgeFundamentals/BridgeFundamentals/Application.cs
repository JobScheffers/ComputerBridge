using System;
using System.Runtime.Serialization;

namespace Sodes.Bridge.Base
{
    public enum ActivityKind
    {
        None
        ,
        ApplicationStart
        ,
        ApplicationEnd
            ,
        BoardFinished
        ,
        AI_GetBid
        ,
        AI_ExplainBid
        ,
        AI_GetCard
        ,
        AI_ClaimOk
        ,
        AI_PossibleBids
        ,
        AI_DoubleDummy_Tricks
        ,
        AI_DoubleDummy_AllTricks
    }

    [DataContract]
    public class Activity
    {
        [DataMember]
        public ActivityKind What { get; set; }
        [DataMember]
        public Guid Who { get; set; }
        [DataMember]
        public int Weight { get; set; }
        [DataMember]
        public DateTime When { get; set; }
        [DataMember]
        public int Count { get; set; }
    }
}
