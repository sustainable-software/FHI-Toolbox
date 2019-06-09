using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using FhiModel.Common;

namespace FhiModel.EcosystemVitality.DendreticConnectivity
{
    [DataContract(Namespace = "", IsReference = true)]
    public class Reach : ModelBase
    {
        private double? _length;

        [DataMember]
        public int Id { get; set; }
        [DataMember]
        public List<Reach> UpstreamReaches { get; set; }
        [DataMember]
        public Reach DownstreamReach { get; set; }
        [DataMember]
        public List<int> SegmentId { get; set; }
        [DataMember]
        public Boolean Outlet { get; set; }
        [DataMember]
        public List<Node> Nodes { get; set; }
        [DataMember]
        public Boolean HasDam { get; set; }

        public Node Upstream => Nodes[Nodes.Count - 1];
        public Node Downstream => Nodes[0];
        public Double Length => _length ?? ComputeLength();
        
        private Double ComputeLength()
        {
            var length = 0.0;
            for (var i = 0; i < Nodes.Count - 1; i++)           
                length += Nodes[i].Location.Distance(Nodes[i + 1].Location);
            _length = length;
            return length;
        }

        /// <summary>
        /// Compute distance to the nearest upstream junction.
        /// </summary>
        /// <returns>The distance in XX?</returns>
        public Double DistanceUpstream(Node node)
        {
            var index = Nodes.IndexOf(node);
            var distance = 0d;
            for (var i = index; i < Nodes.Count - 1; i++)
                distance += Nodes[i].Location.Distance(Nodes[i + 1].Location);
            return distance;
        }

        /// <summary>
        /// Compute distance to the nearest downstream junction.
        /// </summary>
        /// <returns>The distance in XX?</returns>
        public Double DistanceDownstream(Node node)
        {
            var index = Nodes.IndexOf(node);
            var distance = 0d;
            for (var i = index; i > 0; i--)
                distance += Nodes[i].Location.Distance(Nodes[i - 1].Location);
            return distance;
        }

        public override string ToString()
        {
            var usid = string.Empty;
            if (UpstreamReaches != null)
                usid = String.Join("/", UpstreamReaches.Select(x => x.Id));
            var segments = string.Empty;
            if (SegmentId != null)
                segments = $"S:{String.Join("/", SegmentId)}";
            
            return $"{Id} U:{usid} D:{DownstreamReach?.Id} {segments} L:{Length}" +
                   (HasDam ? " DAM: " + String.Join("/", Nodes.Select(x => x.Dam?.Name)) : "") +
                   (Outlet ? " OUT" : "");
        }
    }   
}