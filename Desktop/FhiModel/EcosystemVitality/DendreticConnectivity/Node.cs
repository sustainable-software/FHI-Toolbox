using System;
using System.Runtime.Serialization;
using FhiModel.Common;

namespace FhiModel.EcosystemVitality.DendreticConnectivity
{
    [DataContract(Namespace = "", IsReference = true)]
    public class Node : ModelBase, ILocated
    {
        public Node()
        {
            OnDeserialized();   
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context = default(StreamingContext))
        {
            Location = Location ?? new Location();
        }
        [IgnoreDataMember]
        public String Name => "Node";

        /// <summary>
        /// Location of the node.
        /// </summary>
        [DataMember]
        public Location Location { get; private set; }
        
        /// <summary>
        /// Not null if this node has a dam.
        /// </summary>
        [DataMember]
        public Dam Dam { get; set; }
        
        /// <summary>
        /// Id of the segment this node lives in.
        /// </summary>
        [DataMember]
        public int SegmentId { get; set; }
        
        
        public override string ToString()
        {
            return $"{Location}";
        }
    }
}