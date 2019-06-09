using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CsvHelper;

namespace FhiModel.EcosystemVitality.DendreticConnectivity
{
    public class RiverTableRow
    {
        public String Wkt { get; set; }
        public String ArcId { get; set; }
        public String FromNode { get; set; }
        public String ToNode { get; set; }
        public String SubBasin { get; set; }
        public String MajorBasin { get; set; }
        public String ToBasin { get; set; }
        public String SubName { get; set; }
        public String MajorName { get; set; }
        public String SubArea { get; set; }
        public String MajorArea { get; set; }
        public String Strahler { get; set; }
        public String Gms { get; set; }
        public Single Length { get; set; }

        public static List<RiverTableRow> Create(String filename, Int32 wktColumn, Int32? idColumn)
        {
            if (!File.Exists(filename))
                throw new ArgumentException($"{filename} not found");

            try
            {
                
                var riverTable = new List<RiverTableRow>();
                using (var stream = new FileStream(filename, FileMode.Open))
                {
                    using (var reader = new CsvReader(new StreamReader(stream, Encoding.UTF8)))
                    {
                        reader.Read();
                        var reachId = 1000;
                        // WKT,ARCID,FROM_NODE,TO_NODE,Sub_Bas,Maj_Bas,To_Bas,Sub_Name,Maj_Name,Sub_Area,Maj_Area,Strahler,GMS,len
                        while (reader.Read())
                        {
                            var row = new RiverTableRow
                            {
                                Wkt = reader.GetField(wktColumn),
                                ArcId = idColumn != null ? reader.GetField(idColumn.Value) : reachId++.ToString()
                            };
                            riverTable.Add(row);
                        }
                    }
                }

                return riverTable;
            }
            catch (Exception)
            {
                throw new ArgumentException($"River file {filename} not in proper format or in use by another application.");
            }
        }
    }
}