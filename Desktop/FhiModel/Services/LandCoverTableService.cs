using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FhiModel.EcosystemVitality;

namespace FhiModel.Services
{
    public static class LandCoverTableService
    {
        static LandCoverTableService()
        {
            LandCoverMappings = new List<String>(_landCoverMappings.Select(x => x.Key));
        }

        public static  IEnumerable<String> LandCoverMappings { get; }

        public static IEnumerable<LandCoverItem> GetTable(string name)
        {
            if (!_landCoverMappings.ContainsKey(name)) return null;
            return _landCoverMappings[name];
        }
        
        private static readonly IEnumerable<LandCoverItem> _esacci = new []
        {
            new LandCoverItem
            {
                Naturalness = "Natural and semi-natural",
                Characteristics = "Native",
                Examples = "Forest (primary and secondary); lakes (natural) and wetlands; native grasslands; native shrublands",
                Weight = 100,
                Mapping = new List<byte> {50, 60, 61, 62, 70, 71, 72, 80, 81, 82, 90, 100, 110, 120, 121, 122, 130, 140, 150, 151, 152, 153, 160, 170, 180}
            },
            new LandCoverItem
            {
                Naturalness = "Cultural assisted system",
                Characteristics = "Mixed, high diversity",
                Examples = "Mosaic native vegetation (>50%, vegetation cover <50%)",
                Weight = 70,
                Mapping = new List<byte> {40}

            },
            new LandCoverItem
            {
                Naturalness = "Cultural assisted system",
                Characteristics = "Mixed, moderate diversity",
                Examples = "Mosaic cropland (>50%, natural vegetation <50%)",
                Weight = 60,
                Mapping = new List<byte> {30}

            },
            new LandCoverItem
            {
                Naturalness = "Transformed system",
                Characteristics = "Permanent cover with atypical species",
                Examples = "Permanent pasture land; agroforestry; tree crops",
                Weight = 50,
                Mapping = new List<byte> {}

            },
            new LandCoverItem
            {
                Naturalness = "Transformed system",
                Characteristics = "Seasonal cover with atypical species",
                Examples = "Non-irrigated arable land",
                Weight = 40,
                Mapping = new List<byte> {10, 11, 12}

            },
            new LandCoverItem
            {
                Naturalness = "Transformed system",
                Characteristics = "Seasonal cover with atypical species",
                Examples = "Permanently irrigated arable land",
                Weight = 30,
                Mapping = new List<byte> {20 }

            },
            new LandCoverItem
            {
                Naturalness = "Completely artificial",
                Characteristics = "Sparse cover with grass",
                Examples = "Urban park space; low-density suburban areas; barren land",
                Weight = 10,
                Mapping = new List<byte> {200, 201, 202}

            },
            new LandCoverItem
            {
                Naturalness = "Completely artificial",
                Characteristics = "None",
                Examples = "Urban commercial areas; mining areas",
                Weight = 0,
                Mapping = new List<byte> {190}
            }
        };

        private static readonly Dictionary<String, IEnumerable<LandCoverItem>> _landCoverMappings = new Dictionary<string, IEnumerable<LandCoverItem>>
        {
            { "ESACCI LCCS", _esacci }
        };
    }
}
