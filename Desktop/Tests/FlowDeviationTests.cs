using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CsvHelper;
using FhiModel.Common.Timeseries;
using FhiModel.EcosystemVitality.FlowDeviation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
    [TestClass]
    public class FlowDeviationTests
    {
        [TestMethod]
        public void FlowDeviationSmokeTest()
        {
            var indicator = new FlowDeviationIndicator();

            foreach (var i in Enumerable.Range(0, 10))
            {
                var station = new Station { Name = $"Station {i}" };
                
                for (var month = 0; month < 60; month++)
                {
                    station.Regulated.Add(new TimeseriesDatum
                    {
                        Time = new DateTime(2018, 1, 1).AddMonths(month),
                        Value = 10
                    });
                    station.Unregulated.Add(new TimeseriesDatum
                    {
                        Time = new DateTime(2018, 1, 1).AddMonths(month),
                        Value = 15
                    });
                }
                indicator.Stations.Add(station);
            }

            Assert.IsTrue(indicator.Value == 57);
        }

        private readonly List<TestFileSet> _testFiles = new List<TestFileSet>
        {
            new TestFileSet
            {
                Regulated = @"..\..\..\TestData\DvNF_regulated.csv",
                Unregulated = @"..\..\..\TestData\DvNF_unregulated.csv",
                Aapfd = @"..\..\..\TestData\DvNF_aapfd.csv",
                FlowDeviation = 70
            }
        };

        [TestMethod]
        public void FlowDeviationCasesTest()
        {
            foreach (var fileSet in _testFiles)
            {
                var indicator = new FlowDeviationIndicator();
                foreach (var station in LoadStationsFromCsv(fileSet.Regulated, fileSet.Unregulated))
                    indicator.Stations.Add(station);
                
                Assert.IsTrue(indicator.Value == fileSet.FlowDeviation);
                foreach (var result in ReadAapfdCsv(fileSet.Aapfd))
                {
                    var station = indicator.Stations.First(x => x.Name == result.Name);
                    Assert.IsTrue(NearlyEqual(result.FlowDeviation, station.FlowDeviation));
                    Assert.IsTrue(NearlyEqual(result.NetAapfd, station.NetAapfd));
                }
            }
        }

        private Boolean NearlyEqual(Double? a, Double? b)
        {
            if (!a.HasValue || !b.HasValue) return false;
            return Math.Abs(a.Value - b.Value) < .001;
        }
        
        private List<Station> LoadStationsFromCsv(string regulated, string unregulated)
        {
            var rv = new List<Station>();
            var columnCount = 0;
            using (var stream = new FileStream(regulated, FileMode.Open))
            {
                using (var reader = new CsvReader(new StreamReader(stream, Encoding.UTF8)))
                {
                    reader.Read();
                    
                    // list of stations
                    reader.Read();
                    
                    while (true)
                    {
                        if (!reader.TryGetField<String>(columnCount, out var field)) break;
                        columnCount++;
                        if (field == "Date") continue;
                        rv.Add(new Station { Name = field });
                    }
                    
                    while (reader.Read())
                    {
                        var timestep = GoofyExcelDate( reader.GetField<String>(0));
                        for (var i = 1; i < columnCount; i++)
                        {
                            rv[i - 1].Regulated.Add(new TimeseriesDatum
                            {
                                Time = timestep,
                                Value = reader.GetField<Double>(i)
                            });
                        }
                    }
                }
            }
            
            using (var stream = new FileStream(unregulated, FileMode.Open))
            {
                using (var reader = new CsvReader(new StreamReader(stream, Encoding.UTF8)))
                {
                    reader.Read();
                    reader.Read();
                    while (reader.Read())
                    {
                        var timestep = GoofyExcelDate(reader.GetField<String>(0));
                        for (var i = 1; i < columnCount; i++)
                        {
                            rv[i - 1].Unregulated.Add(new TimeseriesDatum
                            {
                                Time = timestep,
                                Value = reader.GetField<Double>(i)
                            });
                        }
                    }
                }
            }

            return rv;
        }

        private DateTime GoofyExcelDate(String s)
        {
            var months = new List<String>
                {"", "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"};
            try
            {
                var split = s.Split('-');
                var month = months.IndexOf(split[0]);
                var year = int.Parse(split[1]);
                return new DateTime(year < 50 ? year + 2000 : year + 1900, month, 1);
            }
            catch (Exception)
            {
                return DateTime.Parse(s);
            }
        }
        
        private List<StationData> ReadAapfdCsv(string filename)
        {
            var rv = new List<StationData>();
            using (var stream = new FileStream(filename, FileMode.Open))
            {
                using (var reader = new CsvReader(new StreamReader(stream, Encoding.UTF8)))
                {
                    reader.Read();
                    
                    // list of stations
                    reader.Read();
                    var columnCount = 0;
                    while (true)
                    {
                        if (!reader.TryGetField<String>(columnCount, out var field)) break;
                        columnCount++;
                        if (field == "Station") continue;
                        rv.Add(new StationData { Name = field });
                    }
                    
                    // net aapfd
                    reader.Read();
                    for (var i = 1; i < columnCount; i++)
                    {
                        rv[i - 1].NetAapfd = reader.GetField<Double>(i);
                    }
                    
                    // dvnf
                    reader.Read();
                    for (var i = 1; i < columnCount; i++)
                    {
                        rv[i - 1].FlowDeviation = reader.GetField<Double>(i);
                    }
                    reader.Read();
                    while (reader.Read())
                    {
                        var year = reader.GetField<Int32>(0);
                        for (var i = 1; i < columnCount; i++)
                        {
                            rv[i - 1].Data.Add(new StationDatum
                            {
                                Year = year,
                                Value = reader.GetField<Double>(i)
                            });
                        }
                    }
                }
            }

            return rv;
        }
        
        private class TestFileSet
        {
            public String Regulated { get; set; }
            public String Unregulated { get; set; }
            public String Aapfd { get; set; }
            public Int32 FlowDeviation { get; set; }
        }

        private class StationData
        {
            public String Name { get; set; }
            public Double NetAapfd { get; set; }
            public Double FlowDeviation { get; set; }
            public List<StationDatum> Data { get; } = new List<StationDatum>();
        }

        private class StationDatum
        {
            public Int32 Year { get; set; }
            public Double Value { get; set; }
        }
    }
}