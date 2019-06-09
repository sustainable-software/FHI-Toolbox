using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using CsvHelper;
using FhiModel.Common;

namespace FhiModel.EcosystemVitality.Biodiversity
{
    // ReSharper disable InconsistentNaming
    public enum RedListCode { CR, EN, VU, LC, NT, DD, NONE}
    public enum Legend { ExtantIntroduced, Extant, NotMapped, OriginUncertain, PossiblyExtant, PresenceUncertain, ProbablyExtant, Reintroduced, None }
    public enum DataSource { RedList, BirdLife }
    
    [DataContract(Namespace = "", IsReference = true)]
    public class Species : ModelBase
    {
        private String _common;
        private RedListCode _code;
        private Int32? _populationCurrent;
        private Int32? _populationPrevious;

        /// <summary>
        /// Id (depends on source)
        /// </summary>
        [DataMember]
        public int Id { get; set; }
        /// <summary>
        /// Scientific name of the species
        /// </summary>
        [DataMember]
        public String Binomial { get; set; }
        /// <summary>
        /// Year (data was collected?)
        /// </summary>
        [DataMember]
        public int? Year { get; set; }
        /// <summary>
        /// Study that identified species for this location
        /// </summary>
        [DataMember]
        public String Citation { get; set; }
        /// <summary>
        /// Presence in basin
        /// </summary>
        [DataMember]
        public Legend Legend { get; set; }
        /// <summary>
        /// Taxa
        /// </summary>
        [DataMember]
        public String Kingdom { get; set; }
        /// <summary>
        /// Taxa
        /// </summary>
        [DataMember]
        public String Phylum { get; set; }
        /// <summary>
        /// Taxa
        /// </summary>
        [DataMember]
        public String Class { get; set; }
        /// <summary>
        /// Taxa
        /// </summary>
        [DataMember]
        public String Order { get; set; }
        /// <summary>
        /// Taxa
        /// </summary>
        [DataMember]
        public String Family { get; set; }
        /// <summary>
        /// Taxa
        /// </summary>
        [DataMember]
        public String Genus { get; set; }

        /// <summary>
        /// Code from the Red List
        /// </summary>
        [DataMember]
        public RedListCode Code
        {
            get => _code;
            set => Set(ref _code, value);
        }
        
        /// <summary>
        /// User is allowed to change the code
        /// </summary>
        [DataMember]
        public Boolean UserCanChangeCode { get; set; }

        /// <summary>
        /// Source of this data
        /// </summary>
        [DataMember]
        public String Source { get; set; }
        
        /// <summary>
        /// What kind of file we read this species in from.
        /// </summary>
        [DataMember]
        public DataSource DataSource { get; set; }

        /// <summary>
        /// Common name
        /// </summary>
        [DataMember]
        public String Common
        {
            get => _common;
            set => Set(ref _common, value);
        }

        /// <summary>
        /// Used for the Invasive Species computation.
        /// </summary>
        [DataMember]
        public Boolean Invasive { get; set; }
        /// <summary>
        /// User entered data
        /// </summary>
        [DataMember]
        public Boolean Custom { get; set; }

        /// <summary>
        /// Species population for the current assessment point
        /// </summary>
        [DataMember]
        public Int32? PopulationCurrent
        {
            get => _populationCurrent;
            set
            {
                if(Set(ref _populationCurrent, value))
                    RaisePropertyChanged(nameof(PopulationTrend));
            }
        }

        /// <summary>
        /// Species population for the previous assessment point
        /// </summary>
        [DataMember]
        public Int32? PopulationPrevious
        {
            get => _populationPrevious;
            set
            {
                if(Set(ref _populationPrevious, value))
                    RaisePropertyChanged(nameof(PopulationTrend));
            }
        }

        public Double? PopulationTrend => PopulationCurrent.HasValue && PopulationPrevious.HasValue
                                            ? Math.Log((Double) PopulationCurrent / (Double) PopulationPrevious)
                                            : (Double?)null;

        public override String ToString()
        {
            return $"{Binomial} : {Code}";
        }

        public static List<Species> Create(string filename)
        {
            if (!File.Exists(filename))
                throw new ArgumentException($"File {filename} not found.");
            
            try
            {
                var rv = new List<Species>();
                using (var stream = new FileStream(filename, FileMode.Open))
                {
                    using (var reader = new CsvReader(new StreamReader(stream, Encoding.UTF8)))
                    {
                        reader.Read();
                        // id_no,binomial,year,citation,legend,kingdom_na,phylum_nam,class_name,order_name,family_nam,genus_name,code,shape_Leng,shape_Area
                        while (reader.Read())
                        {
                            var row = new Species
                            {
                                Id = reader.GetField<int>(0),
                                Binomial = reader.GetField<String>(1),
                                Year = reader.GetField<int?>(2),
                                Citation = reader.GetField<String>(3),
                                Legend = StringToLegend(reader.GetField<String>(4)),
                                Kingdom = reader.GetField<String>(5),
                                Phylum = reader.GetField<String>(6),
                                Class = reader.GetField<String>(7),
                                Order = reader.GetField<String>(8),
                                Family = reader.GetField<String>(9),
                                Genus = reader.GetField<String>(10),
                            };
                            var code = reader.GetField<String>(11);
                            row.Code = Enum.TryParse(code, true, out RedListCode red) ? red : RedListCode.NONE;
                            rv.Add(row);
                            
                        }
                    }
                }
                return rv;
            }
            catch (Exception)
            {
                throw new ArgumentException($"Species file {filename} not in proper format or in use by another application.");
            }
        }

        public static Legend StringToLegend(String name)
        {
            var lowerName = name.ToLowerInvariant();
            foreach (var kvp in LegendKey)
            {
                if (lowerName == kvp.Value.ToLowerInvariant())
                    return kvp.Key;
            }

            return Legend.None;
        }
        
        public static readonly Dictionary<Legend, String> LegendKey = new Dictionary<Legend, String>
        {
            { Legend.ExtantIntroduced, "Extant & Introduced (resident)"},
            { Legend.Extant, "Extant (resident)"},
            { Legend.NotMapped, "Not Mapped"},
            { Legend.OriginUncertain, "Origin uncertain"},
            { Legend.PossiblyExtant, "Possibly Extant (resident)"},
            { Legend.PresenceUncertain, "Presence Uncertain"},
            { Legend.ProbablyExtant, "Probably Extant (resident)"},
            { Legend.Reintroduced, "Reintroduced"},
            { Legend.None, ""}
        };
    }
}
