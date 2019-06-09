using System;
using System.CodeDom;
using System.Collections.Generic;
using Fhi.Controls.Indicators.EcosystemVitality;
using Fhi.Controls.Indicators.EcosystemVitality.DamWizard;
using FhiModel.Common;
using FhiModel.EcosystemVitality.DendreticConnectivity;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
    [TestClass]
    public class ConnectivityIndicatorTests
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
                DciD = .3614238,
                Wkid = 32648
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
                DciD = .6558908,
                Wkid = 32648

            }
        };
        
        [TestMethod]
        public void PotadromousIndicatorTest()
        {
            foreach (var test in _testSets)
            {
                var indicator = new ConnectivityIndicator {Reaches = GetReaches(test)};
                using (new Measure("DciP"))
                    Assert.AreEqual(indicator.DciP, (Int32) Math.Round(test.DciP * 100, 0));
            }
        }

        [TestMethod]
        public void DiadromousIndicatorTest()
        {
            foreach (var test in _testSets)
            {
                var indicator = new ConnectivityIndicator { Reaches = GetReaches(test)};
                using (new Measure("DciD"))
                    Assert.AreEqual(indicator.DciD, (Int32) Math.Round(test.DciD * 100, 0));
            }
        }

        private List<Reach> GetReaches(DciTestSet set)
        {
            var ir = new ImportReachesViewModel { Wkid = set.Wkid };

            using (new Measure("River Import"))
            {
                ir.RiverCsvImport(set.Rivers);

                foreach (var reach in ir.Reaches)
                {
                    foreach (var node in reach.Nodes)
                    {
                        if (!node.Location.Match(set.Outflow)) continue;

                        reach.Outlet = true;
                        break;
                    }

                    if (reach.Outlet) break;
                }

                ir.Process();
            }

            using (new Measure("Dam Import"))
            {
                var dams = new Step1ViewModel(ir.Reaches, set.Wkid, null);
                dams.ImportCsv(set.Dams);
            }

            return ir.Reaches;
        }
    }
}