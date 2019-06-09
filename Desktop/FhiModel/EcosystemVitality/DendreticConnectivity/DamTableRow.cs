using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CsvHelper;
using FhiModel.Common;

namespace FhiModel.EcosystemVitality.DendreticConnectivity
{
    public class DamTableRow
    {
        public int Id { get; set; }
        public Location Location { get; set; }
        public String Name { get; set; }
        public Double Passability { get; set; }

        public static List<DamTableRow> Create(string filename)
        {
            if (!File.Exists(filename))
                throw new ArgumentException($"File {filename} not found.");
            try
            {
                var damTable = new List<DamTableRow>();
                using (var stream = new FileStream(filename, FileMode.Open))
                {
                    using (var reader = new CsvReader(new StreamReader(stream, Encoding.UTF8)))
                    {
                        reader.Read();
                        // Id, X, Y, Name, P
                        while (reader.Read())
                        {
                            var row = new DamTableRow
                            {
                                Id = reader.GetField<int>(0),
                                Location = new Location
                                {
                                    Longitude = reader.GetField<Double>(1), 
                                    Latitude = reader.GetField<Double>(2)
                                },
                            };
                            row.Name = reader.TryGetField<string>(3, out var name) ? name : row.Id.ToString();
                            row.Passability = reader.TryGetField<Double>(4, out var pass) ? pass : 0.5;
                            damTable.Add(row);
                        }
                    }
                }

                return damTable;
            }
            catch (Exception)
            {
                throw new ArgumentException($"Dam file {filename} not in proper format or in use by another application.");
            }
        }

        public override string ToString()
        {
            return $"{Id} {Name} {Location} {Passability}";
        }
    }
}
