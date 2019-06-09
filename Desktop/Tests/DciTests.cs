using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CsvHelper;
using FhiModel.Common;
using FhiModel.EcosystemVitality.DendreticConnectivity;

namespace Tests
{
    [TestClass]
    public class DciTests
    {
        private static readonly List<DciTestSet> _testSets = new List<DciTestSet>
        {
            new DciTestSet
            {
                Outflow = new Location
                {
                    Longitude = 603021.8638,
                    Latitude = 1497007.872
                },
                Rivers = @"..\..\..\TestData\river2.csv",
                Dams = @"..\..\..\TestData\dams2_update.csv",
                Network = @"..\..\..\TestData\river_result2.csv",
                Segments = @"..\..\..\TestData\pd2.csv",
                DciP = .2061348,
                DciD = .3614238
            },
            new DciTestSet
            {
                Outflow = new Location
                {
                    Longitude = 603021.8638,
                    Latitude = 1497007.872
                },
                Rivers = @"..\..\..\TestData\river1.csv",
                Dams = @"..\..\..\TestData\dams1.csv",
                Network = @"..\..\..\TestData\river_result1.csv",
                Segments = @"..\..\..\TestData\pd1.csv",
                DciP = .5458785,
                DciD = .6558908
            }
        };

        [TestMethod]
        public void NetworkTest()
        {
            foreach (var test in _testSets)
            {
                var net = ReadNetworkFromFile(test.Rivers, test.Dams, test.Outflow.Longitude, test.Outflow.Latitude);
                var answerTable = ReadCorrectNetworkFromFile(test.Network);

                // verify that the input network is translated in the same way as the spreadsheet does
                foreach (var r in net.Reaches)
                {
                    Assert.IsTrue(answerTable.ContainsKey(r.Id));
                    Assert.IsTrue(answerTable[r.Id].Downstream == r.DownstreamReach?.Id);
                    if (r.UpstreamReaches != null)
                    {
                        foreach (var usr in r.UpstreamReaches)
                        {
                            Assert.IsTrue(answerTable[r.Id].Upstream.Contains(usr.Id));
                        }
                    }
                    else
                    {
                        Assert.IsTrue(answerTable[r.Id].Upstream.Count == 0);
                    }
                }

                var locationTable = DamLocationRow.Create(test.Segments);
                var locations = new Dictionary<int, DamLocationRow>();
                foreach (var row in locationTable)
                    locations.Add(row.Id, row);

                // check that segment id's match
                var segments = new Dictionary<int, Segment>();
                foreach (var segment in net.Segments)
                    segments.Add(segment.Id, segment);
                foreach (var location in locations.Keys)
                {
                    if (!segments.ContainsKey(location))
                        Assert.IsTrue(segments.ContainsKey(location));
                }

                // look through the segments and see that the upstream length measurements match
                foreach (var segment in net.Segments)
                {
                    Assert.IsTrue(Math.Abs(segment.UpstreamLength - locations[segment.Id].UpstreamLength) < 1,
                        $"{segment.Id} {segment.UpstreamLength}");
                    // Trace.WriteLine($"{segment.Id} {segment.UpstreamLength} {locations[segment.Id].UpstreamLength} {segment.UpstreamLength - locations[segment.Id].UpstreamLength}");
                }

                Assert.IsTrue(Math.Abs(net.DciD - test.DciD) < .002, $"{net.DciD} {test.DciD} {Math.Abs(net.DciD - test.DciD)}");
                Assert.IsTrue(Math.Abs(net.DciP - test.DciP) < .002, $"{net.DciP} {test.DciP} {Math.Abs(net.DciP - test.DciP)}");
            }
        }

        [TestMethod]
        public void PassabilityMatrixTest()
        {
            foreach (var test in _testSets)
            {
                var net = ReadNetworkFromFile(test.Rivers, test.Dams, test.Outflow.Longitude, test.Outflow.Latitude);
                var segmentLookup = new Dictionary<int, Segment>();
                foreach (var segment in net.Segments)
                    segmentLookup.Add(segment.Id, segment);
                Trace.Write("     ");
                foreach (var key in segmentLookup.Keys)
                    Trace.Write($"{key,6:N0} ");
                Trace.Write("\n");
                foreach (var keyI in segmentLookup.Keys)
                {
                    var from = segmentLookup[keyI];
                    Trace.Write($"{keyI,3}| ");
                    foreach (var keyJ in segmentLookup.Keys)
                    {
                        Trace.Write($"{from.Passability[keyJ],6:N4} ");
                    }

                    Trace.Write("\n");
                }
            }
        }

        [TestMethod]
        public void NodeDistanceTest()
        {
            foreach (var test in _testSets)
            {
                var net = ReadNetworkFromFile(test.Rivers, test.Dams, test.Outflow.Longitude, test.Outflow.Latitude);
                foreach (var reach in net.Reaches)
                {
                    foreach (var node in reach.Nodes)
                    {
                        var upstream = reach.DistanceUpstream(node);
                        var downstream = reach.DistanceDownstream(node);
                        Trace.WriteLine($"{upstream} + {downstream} = ({upstream + downstream}) {reach.Length}");
                        Assert.IsTrue(AboutEqual(upstream + downstream, reach.Length));
                    }
                }
            }
        }
#if false
        [TestMethod]
        public void WellDefinedTest()
        {
            var net = WellDefinedNetwork();
            Assert.AreEqual(net.Segments[0].UpstreamLength, 17);
        }

        private Network WellDefinedNetwork()
        {
            var r0 = new Reach
            {
                Id = 0,
                Outlet = true,
                Nodes = new List<Node>
                {
                    new Node { Location = new NetworkPoint(4, 0) },
                    new Node { Location = new NetworkPoint(4, 1) }
                }

            };
            var r1 = new Reach
            {
                Id = 1,
                Nodes = new List<Node>
                {
                    new Node { Location = new NetworkPoint(4, 1) },
                    new Node { Location = new NetworkPoint(3, 1) },
                    new Node { Location = new NetworkPoint(3, 2) },
                    new Node { Location = new NetworkPoint(2, 2) },
                    new Node { Location = new NetworkPoint(2, 3) }
                }
            };
            var r2 = new Reach
            {
                Id = 2,
                Nodes = new List<Node>
                {
                    new Node { Location = new NetworkPoint(4, 1) },
                    new Node { Location = new NetworkPoint(5, 1) },
                    new Node { Location = new NetworkPoint(5, 2) },
                    new Node { Location = new NetworkPoint(6, 2) },
                    new Node { Location = new NetworkPoint(6, 3) }
                }
            };
            var r3 = new Reach
            {
                Id = 3,
                Nodes = new List<Node>
                {
                    new Node { Location = new NetworkPoint(2, 3) },
                    new Node { Location = new NetworkPoint(1, 3) },
                    new Node { Location = new NetworkPoint(1, 4) }
                }
            };
            var r4 = new Reach
            {
                Id = 4,
                Nodes = new List<Node>
                {
                    new Node { Location = new NetworkPoint(2, 3) },
                    new Node { Location = new NetworkPoint(3, 3) },
                    new Node { Location = new NetworkPoint(3, 4) }
                }
            };
            var r5 = new Reach
            {
                Id = 5,
                Nodes = new List<Node>
                {
                    new Node { Location = new NetworkPoint(6, 3) },
                    new Node { Location = new NetworkPoint(5, 3) },
                    new Node { Location = new NetworkPoint(5, 4) }
                }
            };
            var r6 = new Reach
            {
                Id = 6,
                Nodes = new List<Node>
                {
                    new Node { Location = new NetworkPoint(6, 3) },
                    new Node { Location = new NetworkPoint(7, 3) },
                    new Node { Location = new NetworkPoint(7, 4) }
                }
            };
            r0.UpstreamReaches = new List<Reach> { r1, r2 };
            r1.DownstreamReach = r0;
            r1.UpstreamReaches = new List<Reach> { r3, r4 };
            r2.DownstreamReach = r0;
            r2.UpstreamReaches = new List<Reach> { r5, r6 };
            r3.DownstreamReach = r1;
            r4.DownstreamReach = r1;
            r5.DownstreamReach = r2;
            r5.DownstreamReach = r2;
            return new Network(new List<Reach> { r0, r1, r2, r3, r4, r5, r6 }, 0);
        }
#endif
        private static bool AboutEqual(double x, double y)
        {
            var epsilon = Math.Max(Math.Abs(x), Math.Abs(y)) * 1E-14;
            return Math.Abs(x - y) <= epsilon;
        }

        private Network ReadNetworkFromFile(string riverFile, string damFile, double outletX, double outletY)
        {
            var start = DateTime.Now;
            var net = new Network(RiverTableRow.Create(riverFile, 0, 1), DamTableRow.Create(damFile), new Location{ Longitude = outletX, Latitude = outletY});
            Trace.WriteLine(DateTime.Now - start);
            return net;
        }

        private Dictionary<int, RiverTestNetworkResultRow> ReadCorrectNetworkFromFile(string filename)
        {
            var answerTable = new Dictionary<int, RiverTestNetworkResultRow>();
            if (!File.Exists(filename))
                Assert.Fail($"{filename} not found");

            using (var stream = new FileStream(filename, FileMode.Open))
            {
                using (var reader = new CsvReader(new StreamReader(stream, Encoding.UTF8)))
                {
                    reader.Read();
                    // Reach downstream, reach, reaches upstream
                    while (reader.Read())
                    {
                        var row = new RiverTestNetworkResultRow
                        {
                            Downstream = reader.GetField<int?>(0),
                            Id = reader.GetField<int>(1),
                            Upstream = new List<int>()
                        };
                        
                        var i = 2;
                        while (true)
                        {
                            if (reader.TryGetField(i++, out string result) && !String.IsNullOrWhiteSpace(result))
                                row.Upstream.Add(int.Parse(result));
                            else
                                break;
                        }
                        answerTable.Add(row.Id, row);
                    }
                }
            }
            return answerTable;
        }
    }

    public class DciTestSet
    {
        /// <summary>
        /// Outflow x & y
        /// </summary>
        public Location Outflow { get; set; }
        /// <summary>
        /// Rivers sheet
        /// </summary>
        public String Rivers { get; set; }
        /// <summary>
        /// Dams sheet
        /// </summary>
        public String Dams { get; set; }
        /// <summary>
        /// Network sheet
        /// </summary>
        public String Network { get; set; }
        /// <summary>
        /// DamLocation sheet
        /// </summary>
        public String Segments { get; set; }
        /// <summary>
        /// Expected DCId
        /// </summary>
        public Double DciD { get; set; }
        /// <summary>
        /// Expected DCIp
        /// </summary>
        public Double DciP { get; set; }
        public Int32 Wkid { get; set; }

        
    }

    public class RiverTestNetworkResultRow
    {
        public int Id { get; set; } // reach
        public int? Downstream { get; set; }
        public List<int> Upstream { get; set; }
    }

    public class DamLocationRow
    {
        public int Id { get; set; }
        public double UpstreamLength { get; set; }

        // Dam_id,r_id,n_id,len_downstream,len_upstream

        public static List<DamLocationRow> Create(string filename)
        {
            var rv = new List<DamLocationRow>();
            using (var stream = new FileStream(filename, FileMode.Open))
            {
                using (var reader = new CsvReader(new StreamReader(stream, Encoding.UTF8)))
                {
                    reader.Read();
                    // d_id,d_id_dwn,r_dwn,len_up,passability,level
                    while (reader.Read())
                    {
                        var row = new DamLocationRow
                        {
                            Id = reader.GetField<int>(0),
                            UpstreamLength = reader.GetField<double>(3)
                        };
                        rv.Add(row);
                    }
                }
            }
            return rv;
        }
    }

    public class NodeLocationRow
    {
        public int Id { get; set; }
        public int ReachId { get; set; }
        public Location Location { get; set; }

        // Reach_id,Node_id,xx,yy,,Total nodes,17853
        public static List<NodeLocationRow> Create(string filename)
        {
            var rv = new List<NodeLocationRow>();
            using (var stream = new FileStream(filename, FileMode.Open))
            {
                using (var reader = new CsvReader(new StreamReader(stream, Encoding.UTF8)))
                {
                    reader.Read();
                    // Reach_id,Node_id,xx,yy,,Total nodes,17853
                    while (reader.Read())
                    {
                        var row = new NodeLocationRow
                        {
                            Id = reader.GetField<int>(1),
                            ReachId = reader.GetField<int>(0),
                            Location = new Location { Longitude = reader.GetField<double>(2), Latitude = reader.GetField<double>(3) }
                        };
                        rv.Add(row);
                    }
                }
            }

            return rv;
        }
    }
}
