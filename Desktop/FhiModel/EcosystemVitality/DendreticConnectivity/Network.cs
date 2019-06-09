using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using FhiModel.Common;

namespace FhiModel.EcosystemVitality.DendreticConnectivity
{
    [DataContract(Namespace = "", IsReference = true)]
    public class Network : ModelBase
    {
        public Network(List<Reach> reaches, int wkid)
        {
            Wkid = wkid;
            Reaches = reaches;
            Outlet = reaches.FirstOrDefault(x => x.Outlet);
            Recalculate();
        }

        public Network(List<RiverTableRow> riverTable, List<DamTableRow> damTable, Location outlet)
        {
            Reaches = InitializeFromWkt(riverTable, outlet);
            AddDams(Reaches, damTable);
            Recalculate();
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context = default(StreamingContext))
        {
            Recalculate();
        }

        [DataMember]
        public Int32 Wkid { get; private set; }
        [DataMember]
        public Reach Outlet { get; private set; }
        [DataMember]
        public List<Reach> Reaches { get; private set; }
        [DataMember]
        public List<Segment> Segments { get; private set; }
        [DataMember]
        public Double Length { get; private set; }
        [DataMember]
        public Double DciD { get; private set; }
        [DataMember]
        public Double DciP { get; private set; }
        
        [DataMember]
        public String RiverFile { get; set; }
        [DataMember]
        public String DamFile { get; set; }

        private void Recalculate()
        {
            try
            {
                Segments = ComputeSegments(Outlet);
                InitializeSegmentNetwork(Reaches, Segments);
                Length = ComputeTotalNetworkLength();
            
                ComputeMetrics();
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Compute metrics crash {ex}");
            }
        }

        private void ComputeMetrics()
        {
            var segmentLookup = new Dictionary<int, Segment>();
            foreach (var segment in Segments)
                segmentLookup.Add(segment.Id, segment);
            
            var vector = new List<double>();
            foreach (var keyI in segmentLookup.Keys)
            {
                var from = segmentLookup[keyI];
                foreach (var keyJ in segmentLookup.Keys)
                {
                    var to = segmentLookup[keyJ];
                    var p = from.Passability[keyJ];
                    vector.Add(from.UpstreamLength * to.UpstreamLength * p);
                }
            }
            DciP = vector.Sum() / Math.Pow(Length, 2);
            
            DciD = 0;
            var zero = segmentLookup[0];
            foreach (var keyJ in segmentLookup.Keys)
            {
                var to = segmentLookup[keyJ];
                var p = zero.Passability[keyJ];
                DciD += p * to.UpstreamLength / Length;
            }
        }

        private List<Reach> InitializeFromWkt(List<RiverTableRow> riverTable, Location outlet)
        {
            var rv = new List<Reach>();
            foreach (var row in riverTable)
                rv.Add(ParseLinestring(row.Wkt, row.ArcId, outlet));
            Outlet = InitializeNetwork(rv);
            return rv;
        }

        private Double ComputeTotalNetworkLength()
        {
            var distance = 0.0;
            foreach (var reach in Reaches)
                distance += reach.Length;   
            return distance;
        }

        /// <summary>
        /// Parses the so-called "well known text" LINESTRING into a list of nodes.
        /// </summary>
        /// <param name="wkt">The well known text string</param>
        /// <param name="outlet">Location of the outlet</param>
        /// <param name="arcId">Id of the arc or "reach"</param>
        /// <returns></returns>
        private static Reach ParseLinestring(String wkt, string arcId, Location outlet)
        {
            var reach = new Reach();
            var nodes = new List<Node>();
            reach.Nodes = nodes;
            reach.Id = int.Parse(arcId);
            
            // todo: this is super-simplistic with no error checking/recovery.
            
            // LINESTRING ( x y, x y, ... )
            var s1 = wkt.Replace("LINESTRING (", "");
            var s2 = s1.Replace(")", "");
            var s3 = s2.Split(',');
            
            var isOutlet = false;
            foreach (var s in s3)
            {
                var node = new Node();
                var pt = s.Split(' ');
                node.Location.Longitude = Double.Parse(pt[0]);
                node.Location.Latitude = Double.Parse(pt[1]);
                
                if (node.Location.Match(outlet))
                    isOutlet = true;
                nodes.Add(node);
            }
            
            if (isOutlet && outlet.Match(reach.Upstream.Location))
            {
                reach.Nodes.Reverse();
                Debug.Assert(reach.Downstream.Location.Match(outlet));
            }
            reach.Outlet = isOutlet;
            return reach;
        }

        /// <summary>
        /// Set up the nodes in the network to have all the correct
        /// upstream and downstream connections.
        /// </summary>
        /// <param name="reaches">List of nodes to initialize.</param>
        private static Reach InitializeNetwork(List<Reach> reaches)
        {
            var outlet = reaches.FirstOrDefault(n => n.Outlet);
            _inChecklist.Clear();
            InitializeNetworkInner(reaches, outlet);
            return outlet;
        }

        private static readonly HashSet<int> _inChecklist = new HashSet<int>();

        private static void InitializeNetworkInner(List<Reach> reaches, Reach root)
        {
            // get the root reach
            // using the root reach, find the end node (first or last in list)
            // search the nodes for matches, get list of reaches that represent children
            // add list of reaches to root reach
            _inChecklist.Add(root.Id);
            foreach (var r in reaches)
            {
                if (_inChecklist.Contains(r.Id)) continue;

                if (root.Upstream.Location.Match(r.Downstream.Location,5) ||
                    root.Upstream.Location.Match(r.Upstream.Location,5))
                {
                    if (root.Upstream.Location.Match(r.Upstream.Location,5))
                        r.Nodes.Reverse();
                    //Debug.Assert(root.Upstream.Location.Match(r.Downstream.Location));

                    if (root.UpstreamReaches == null)
                        root.UpstreamReaches = new List<Reach>();

                    if (r.Id == root.DownstreamReach?.Id)
                        throw new ArgumentException("cycle in graph");

                    _inChecklist.Add(r.Id);
                    root.UpstreamReaches.Add(r);

                    if (r.DownstreamReach != null)
                        throw new ArgumentException("initialization error");

                    r.DownstreamReach = root;
                }
                Debug.Assert(!root.Downstream.Location.Match(r.Upstream.Location));
                Debug.Assert(!root.Downstream.Location.Match(r.Downstream.Location));
            }
            if (root.UpstreamReaches != null)
            {
                foreach (var u in root.UpstreamReaches)
                {
                    InitializeNetworkInner(reaches, u);
                }
            }
        }

        private void AddDams(List<Reach> reaches, List<DamTableRow> damTable)
        {
            foreach (var reach in reaches)
            {
                foreach (var node in reach.Nodes)
                {
                    DamTableRow located = null;
                    foreach (var dam in damTable)
                    {
                        if (!node.Location.Match(dam.Location, 10))
                            continue;
                        node.Dam = new Dam(dam);
                        reach.HasDam = true;
                        located = dam;
                        break;
                    }

                    if (located != null)
                        damTable.Remove(located);
                }
            }
            if (damTable.Count != 0)
                Trace.WriteLine($"Error: dam table should be empty {String.Join(":", damTable)}");
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
                    var segment = new Segment { Dam = node.Dam };
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
                if (reach.SegmentId.Count == 1) continue;
                
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