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
    public class Segment : ModelBase
    {
        public int Id => Dam?.Id ?? 0;
        [DataMember]
        public Dam Dam { get; set; }
        [DataMember]
        public Double UpstreamLength { get; set; }
        [DataMember]
        public Segment DownstreamSegment { get; set; }
        [DataMember]
        public List<Segment> UpstreamSegments { get; set; }
        [DataMember]
        public Dictionary<int, Double> Passability { get; set; }

        public override string ToString()
        {
            return $"dam: {Dam?.Name} length: {UpstreamLength}";
        }

        public void BuildPassability()
        {
            Passability = new Dictionary<int, double>();
            _damIdDebug.Clear();
            _passDebug.Clear();
            _debugLevel = 0;
            Print($"START: {Id} D:{DownstreamSegment?.Id} U:" + (UpstreamSegments != null ? String.Join(" ", UpstreamSegments.Select(x => x.Id)) : ""));
            BuildPassabilityInternal(this, 1);
            Print("VISIT:" + String.Join(" ", Passability.Keys));
        }

        private readonly List<int> _damIdDebug = new List<int>();
        private readonly Dictionary<int, List<int>> _passDebug = new Dictionary<int, List<int>>();

        private static int _debugLevel;
        private void BuildPassabilityInternal(Segment segment, double passability)
        {
            _debugLevel++;
            Passability.Add(segment.Id, passability);
            // debug
            _damIdDebug.Add(segment.Id);
            _passDebug[segment.Id] = _damIdDebug.Clone();
            // end debug
            if (segment.UpstreamSegments != null)
            {
                foreach (var us in segment.UpstreamSegments)
                {
                    if (Passability.ContainsKey(us.Id)) continue;
                    BuildPassabilityInternal(us, passability * us.Dam.Passability);
                }
            }

            if (segment.DownstreamSegment != null && !Passability.ContainsKey(segment.DownstreamSegment.Id))
                BuildPassabilityInternal(segment.DownstreamSegment, passability * Dam.Passability);
            _debugLevel--;
            Print($"{segment.Id} D:{segment.DownstreamSegment?.Id} U:" + (segment.UpstreamSegments != null ? String.Join(" ", segment.UpstreamSegments.Select(x => x.Id)) : "") );
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
    }
}
