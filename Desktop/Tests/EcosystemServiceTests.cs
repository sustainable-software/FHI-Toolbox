using System;
using System.Collections.ObjectModel;
using System.Linq;
using FhiModel.Common.Timeseries;
using FhiModel.EcosystemServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
    [TestClass]
    public class EcosystemServiceTests
    {
        [TestMethod]
        public void F1Test()
        {
            var indicator = new EcosystemServicesIndicator {EvidenceLevel = Confidence.F1};

            foreach (var i in Enumerable.Range(0, 10))
            {
                indicator.SpatialUnits.Add(new F1SpatialUnit
                {
                    Name = $"SU {i}",
                    NonCompliant = i % 2 == 0
                });
            }
            
            Assert.IsTrue(indicator.Value == 50);
        }
        
        [TestMethod]
        public void F2Test()
        {
            var indicator = new EcosystemServicesIndicator {EvidenceLevel = Confidence.F2};

            var results = new ObservableCollection<ObjectiveResult>();
            foreach (var i in Enumerable.Range(1, 12))
            {
                results.Add(new ObjectiveResult
                {
                    Time = new DateTime(2018, i, 1),
                    NonCompliant = i % 2 == 0
                });
            }
            
            foreach (var i in Enumerable.Range(0, 10))
            {
                var su = new F2SpatialUnit
                {
                    Name = $"SU {i}"
                };
                
                foreach (var r in results)
                    su.Results.Add(r);    
                indicator.SpatialUnits.Add(su);
            }
            
            Assert.IsTrue(indicator.Value == 29);
            Assert.IsTrue(indicator.F1 == 100);
            Assert.IsTrue(indicator.F2 == 50);
        }
        
        [TestMethod]
        public void F3FuzzyTest()
        {
            var indicator = new EcosystemServicesIndicator {EvidenceLevel = Confidence.F3Fuzzy};

            var results = new ObservableCollection<ObjectiveResult>();
            foreach (var i in Enumerable.Range(1, 12))
            {
                results.Add(new ObjectiveResult
                {
                    Time = new DateTime(2018, i, 1),
                    Value = i % 2 == 0 ? 0 : 5
                });
            }
            
            foreach (var i in Enumerable.Range(0, 10))
            {
                var su = new F3FuzzySpatialUnit
                {
                    Name = $"SU {i}"
                };
                
                foreach (var r in results)
                    su.Results.Add(r);    
                indicator.SpatialUnits.Add(su);
            }

            Assert.IsTrue(indicator.Value == 16);
            Assert.IsTrue(indicator.F1 == 100);
            Assert.IsTrue(indicator.F2 == 50);
            Assert.IsTrue(indicator.F3 == 71);
        }
        
        [TestMethod]
        public void F3SharpLessThanTest()
        {
            var indicator = new EcosystemServicesIndicator {EvidenceLevel = Confidence.F3Sharp};
            
            var data = new ObservableCollection<TimeseriesDatum>();
            foreach (var i in Enumerable.Range(1, 12))
            {
                data.Add(new TimeseriesDatum
                {
                    Time = new DateTime(2018, i, 1),
                    Value = i % 2 == 0 ? 1 : 5
                });
            }
            
            foreach (var i in Enumerable.Range(0, 10))
            {
                var su = new F3SharpSpatialUnit
                {
                    Name = $"SU {i}", 
                    Objective = new Objective {Function = new ObjectiveFunctionLessThan()}
                };

                su.Objective.Metrics.Add(new ObjectiveMetricSingleValue
                {
                    Start = new DateTime(2018, 1, 1),
                    End = new DateTime(2018, 12, 31),
                    Value = 4
                });
                
                foreach (var r in data)
                    su.Data.Add(r);    
                indicator.SpatialUnits.Add(su);
            }
            var foo = indicator.Value;
            Assert.IsTrue(indicator.Value == 23);
            Assert.IsTrue(indicator.F1 == 100);
            Assert.IsTrue(indicator.F2 == 50);
            Assert.IsTrue(indicator.F3 == 60);
        }
        
        [TestMethod]
        public void F3SharpGreaterThanTest()
        {
            var indicator = new EcosystemServicesIndicator {EvidenceLevel = Confidence.F3Sharp};

            var data = new ObservableCollection<TimeseriesDatum>();
            foreach (var i in Enumerable.Range(1, 12))
            {
                data.Add(new TimeseriesDatum
                {
                    Time = new DateTime(2018, i, 1),
                    Value = i % 2 == 0 ? 1 : 5
                });
            }
            
            foreach (var i in Enumerable.Range(0, 10))
            {
                var su = new F3SharpSpatialUnit
                {
                    Name = $"SU {i}", 
                    Objective = new Objective {Function = new ObjectiveFunctionGreaterThan()}
                };

                su.Objective.Metrics.Add(new ObjectiveMetricSingleValue
                {
                    Start = new DateTime(2018, 1, 1),
                    End = new DateTime(2018, 12, 31),
                    Value = 2
                });
                
                foreach (var r in data)
                    su.Data.Add(r);    
                indicator.SpatialUnits.Add(su);
            }

            var foo = indicator.Value;
            Assert.IsTrue(indicator.Value == 34);
            Assert.IsTrue(indicator.F1 == 100);
            Assert.IsTrue(indicator.F2 == 50);
            Assert.IsTrue(indicator.F3 == 43);
        }
        
        [TestMethod]
        public void F3SharpRangeMinTest()
        {
            var indicator = new EcosystemServicesIndicator {EvidenceLevel = Confidence.F3Sharp};

            var data = new ObservableCollection<TimeseriesDatum>();
            foreach (var i in Enumerable.Range(1, 12))
            {
                data.Add(new TimeseriesDatum
                {
                    Time = new DateTime(2018, i, 1),
                    Value = i % 2 == 0 ? 1 : 5
                });
            }
            
            foreach (var i in Enumerable.Range(0, 10))
            {
                var su = new F3SharpSpatialUnit
                {
                    Name = $"SU {i}", 
                    Objective = new Objective {Function = new ObjectiveFunctionRange()}
                };

                su.Objective.Metrics.Add(new ObjectiveMetricRange
                {
                    Start = new DateTime(2018, 1, 1),
                    End = new DateTime(2018, 12, 31),
                    Minimum = 4,
                    Maximum = 6
                });
                
                foreach (var r in data)
                    su.Data.Add(r);    
                indicator.SpatialUnits.Add(su);
            }

            Assert.IsTrue(indicator.Value == 23);
            Assert.IsTrue(indicator.F1 == 100);
            Assert.IsTrue(indicator.F2 == 50);
            Assert.IsTrue(indicator.F3 == 60);
        }
        
        [TestMethod]
        public void F3SharpRangeMaxTest()
        {
            var indicator = new EcosystemServicesIndicator {EvidenceLevel = Confidence.F3Sharp};

            var data = new ObservableCollection<TimeseriesDatum>();
            foreach (var i in Enumerable.Range(1, 12))
            {
                data.Add(new TimeseriesDatum
                {
                    Time = new DateTime(2018, i, 1),
                    Value = i % 2 == 0 ? 8 : 5
                });
            }
            
            foreach (var i in Enumerable.Range(0, 10))
            {
                var su = new F3SharpSpatialUnit
                {
                    Name = $"SU {i}", 
                    Objective = new Objective {Function = new ObjectiveFunctionRange()}
                };

                su.Objective.Metrics.Add(new ObjectiveMetricRange
                {
                    Start = new DateTime(2018, 1, 1),
                    End = new DateTime(2018, 12, 31),
                    Minimum = 4,
                    Maximum = 6
                });
                
                foreach (var r in data)
                    su.Data.Add(r);    
                indicator.SpatialUnits.Add(su);
            }

            Assert.IsTrue(indicator.Value == 63);
            Assert.IsTrue(indicator.F1 == 100);
            Assert.IsTrue(indicator.F2 == 50);
            Assert.IsTrue(indicator.F3 == 14);
        }
    }
}