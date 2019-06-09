using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using FhiModel.Common;

namespace FhiModel.EcosystemVitality.WaterQuality
{
    [DataContract(Namespace = "", IsReference = true)]
    public class Gauge : ModelBase, ILocated
    {
        private String _name;

        public Gauge()
        {
            OnDeserialized();   
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context = default(StreamingContext))
        {
            Location = Location ?? new Location();
            Parameters = Parameters ?? new ObservableCollection<WaterQualityParameter>();
        }

        [DataMember]
        public String Name
        {
            get => _name;
            set => Set(ref _name, value);
        }

        [DataMember]
        public Location Location { get; set; }

        [DataMember]
        public String Notes { get; set; }
        
        [DataMember]
        public ObservableCollection<WaterQualityParameter> Parameters { get; private set; }

        public Double? F1
        {
            get
            {
                if (!(Parameters?.Count > 0)) return null;
                return (Double)Parameters.Count(x => x.NonCompliantTimesteps > 0) / Parameters.Count;
            }
        }

        public Double? F2
        {
            get
            {
                if (!(Parameters?.Count > 0)) return null;

                var timesteps = 0;
                var failed = 0;
                var excursions = 0.0;
                foreach (var parameter in Parameters)
                {
                    if (!(parameter.Results?.Count > 0)) continue;
                    timesteps += parameter.Results.Count;
                    excursions += parameter.Results.Sum(x => x.Value);
                    failed += parameter.Results.Count(x => x.NonCompliant);
                }

                if (timesteps == 0) return null;
                NormalizedSumExcursions = excursions / timesteps;
                return (Double) failed / timesteps;
            }
        }
        
        public Double? NormalizedSumExcursions { get; private set; }

        public Double? F3
        {
            get
            {
                if (F2 == null) return null;
                return NormalizedSumExcursions / (NormalizedSumExcursions + 1.0);
            }
        }

        public Double? Value => F1 == null || F3 == null ? (Double?) null : 100.0 - Math.Sqrt(F1.Value * F3.Value);
    }
}