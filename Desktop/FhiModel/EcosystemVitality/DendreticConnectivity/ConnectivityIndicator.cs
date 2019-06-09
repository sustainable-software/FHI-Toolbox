using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using FhiModel.Common;

namespace FhiModel.EcosystemVitality.DendreticConnectivity
{
    [DataContract(Namespace = "", IsReference = true)]
    public class ConnectivityIndicator : Indicator
    {
        private List<Reach> _reaches;
        private List<Segment> _segments;
        private Dictionary<int, Segment> _segmentLookup;
        private Double _length;
        private int? _dciD;
        private int? _dciP;
        private double _diadromousWeight = 0.5;
        private double _potadromousWeight = 0.5;

        private Boolean Initialize()
        {
            var outlet = Reaches?.Find(x => x.Outlet);
            if (outlet == null)
            {
                return false;
            }
            Trace.WriteLine("CI: initialize");
            foreach (var reach in Reaches)
                reach.SegmentId = null;
            Segments = ComputeSegments(outlet);
            InitializeSegmentNetwork(Reaches, Segments);
            Length = ComputeTotalNetworkLength(Reaches);
            _segmentLookup = Segments.ToDictionary(x => x.Id);
            return true;
        }      
              
        [DataMember]
        public List<Reach> Reaches
        {
            get => _reaches;
            set
            {
                _reaches = value;
                Initialize();
                Value = null;
                RaisePropertyChanged();
            }
        }

        [DataMember]
        public List<Segment> Segments
        {
            get => _segments;
            protected set => Set(ref _segments, value);
        }

        [DataMember]
        public Double Length
        {
            get => _length;
            protected set => Set(ref _length, value);
        }

        [DataMember]
        public Double DiadromousWeight
        {
            get => _diadromousWeight;
            set
            {
                if (Set(ref _diadromousWeight, value))
                    Value = null;

            }
        }

        [DataMember]
        public Double PotadromousWeight
        {
            get => _potadromousWeight;
            set
            {
                if (Set(ref _potadromousWeight, value))
                    Value = null;
            }
        }

        public Int32? DciD
        {
            get => _dciD;
            set => Set(ref _dciD, value);
        }

        public Int32? DciP
        {
            get => _dciP;
            set => Set(ref _dciP, value);
        }

        protected override Int32? ComputeIndicator()
        {
            if (!Initialize()) return null;

            var dciD = ComputeDiadromousIndicator();
            DciD = (Int32)Math.Round(dciD * 100, 0);

            var dciP = ComputePotadromousIndicator();
            DciP = (Int32)Math.Round(dciP * 100, 0);

            return (Int32)Math.Round((DiadromousWeight * dciD + PotadromousWeight * dciP / (DiadromousWeight + PotadromousWeight)) * 100, 0);
        }
        
        private Double ComputeDiadromousIndicator()
        {
            var dciD = 0.0;
            var zero = _segmentLookup[0];
            foreach (var keyJ in _segmentLookup.Keys)
            {
                var to = _segmentLookup[keyJ];
                var p = zero.Passability[keyJ];
                dciD += p * to.UpstreamLength / Length;
            }

            return dciD;
        }

        private Double ComputePotadromousIndicator()
        {
            
            var vector = new List<Double>();
            foreach (var keyI in _segmentLookup.Keys)
            {
                var from = _segmentLookup[keyI];
                foreach (var keyJ in _segmentLookup.Keys)
                {
                    var to = _segmentLookup[keyJ];
                    var p = from.Passability[keyJ];
                    vector.Add(from.UpstreamLength * to.UpstreamLength * p);
                }
            }

            var dciP = vector.Sum() / Math.Pow(Length, 2);

            return dciP;
        }

        private static Double ComputeTotalNetworkLength(List<Reach> reaches)
        {
            var distance = 0.0;
            foreach (var reach in reaches)
                distance += reach.Length;
            return distance;
        }

        private static List<Segment> ComputeSegments(Reach outlet)
        {
            var segments = new List<Segment>
            {
                new Segment
                {
                    UpstreamLength = ComputeSegmentLength(outlet, outlet.Downstream, 0)
                }
            };
            ComputeSegmentsInner(segments, outlet);
            return segments;
        }

        private static void ComputeSegmentsInner(List<Segment> segments, Reach reach)
        {
            if (reach.HasDam)
            {
                foreach (var node in reach.Nodes)
                {
                    if (node.Dam == null) continue;
                    var segment = new Segment {Dam = node.Dam};
                    segment.UpstreamLength = ComputeSegmentLength(reach, node, segment.Id);
                    segments.Add(segment);
                }
            }

            if (reach.UpstreamReaches != null)
            {
                foreach (var up in reach.UpstreamReaches)
                    ComputeSegmentsInner(segments, up);
            }
        }

        /// <summary>
        /// Compute the length of river in the given segment and mark each reach with the segment
        /// id for later creation of the segment network.
        /// </summary>
        private static double ComputeSegmentLength(Reach reach, Node node, int id)
        {
            AddSegmentIdToReach(reach, id);
            var index = reach.Nodes.IndexOf(node);
            var distance = 0.0;
            for (var i = index; i < reach.Nodes.Count - 1; i++)
            {
                if (i != index && reach.Nodes[i].Dam != null) // dam in the same reach
                    return distance;
                distance += reach.Nodes[i].Location.Distance(reach.Nodes[i + 1].Location);
                reach.Nodes[i].SegmentId = id;
            }

            Print($"SEGMENT: {id} - {reach}");
            var rv = ComputeSegmentLengthInner(reach, id, distance);
            Print($"SEGMENT: {id} is {rv}");
            return rv;
        }

        private static int _debugLevel = 0;
        
        private static double ComputeSegmentLengthInner(Reach reach, int id, double? fromDam = null)
        {
            _debugLevel++;
            var distance = 0.0;
            if (reach.UpstreamReaches != null)
            {
                foreach (var ur in reach.UpstreamReaches)
                {
                    AddSegmentIdToReach(ur, id);

                    Print($"{ur}");

                    if (ur.HasDam)
                    {
                        var toDam = 0.0;
                        for (var i = 0; i < ur.Nodes.Count - 1; i++)
                        {
                            if (ur.Nodes[i].Dam != null)
                                break;
                            toDam += ur.Nodes[i].Location.Distance(ur.Nodes[i + 1].Location);
                            ur.Nodes[i].SegmentId = id;
                        }

                        Print($" TO DAM: {toDam}");
                        distance += toDam;
                    }
                    else
                    {
                        distance += ComputeSegmentLengthInner(ur, id);
                    }
                }
            }

            _debugLevel--;

            distance += fromDam ?? reach.Length;
            Print($"{reach.Id}: {distance}" + (fromDam != null ? $" FD:{fromDam}" : ""));

            return distance;
        }

        private static void Print(string s)
        {
#if false
            var sb = new StringBuilder();
            for (int i = 0; i < _debugLevel; i++)
                sb.Append(" ");
            Trace.WriteLine(sb + s);
#endif
        }

        private static void AddSegmentIdToReach(Reach reach, int id)
        {
            if (reach.SegmentId == null)
                reach.SegmentId = new List<int>();
            if (!reach.SegmentId.Contains(id))
                reach.SegmentId.Add(id);
        }

        private static void InitializeSegmentNetwork(List<Reach> reaches, List<Segment> segments)
        {
            var segmentLookup = segments.ToDictionary(x => x.Id, x => x);

            // segment transitions always happen in a single reach
            // we don't handle multiple segments within a reach yet
            foreach (var reach in reaches)
            {
                if (reach.SegmentId == null || reach.SegmentId.Count == 1) continue;

                for (var i = 0; i < reach.SegmentId.Count - 1; i++)
                {
                    var up = segmentLookup[reach.SegmentId[i + 1]];
                    var down = segmentLookup[reach.SegmentId[i]];

                    up.DownstreamSegment = down;
                    if (down.UpstreamSegments == null)
                        down.UpstreamSegments = new List<Segment>();
                    down.UpstreamSegments.Add(up);
                }
            }

            foreach (var segment in segments)
            {
                segment.BuildPassability();
            }
        }
    }
}