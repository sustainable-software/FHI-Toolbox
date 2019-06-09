using System;
using System.Drawing;
using System.Runtime.Serialization;
using FhiModel.Common;

namespace FhiModel.EcosystemVitality.DendreticConnectivity
{
    [DataContract(Namespace = "", IsReference = true)]
    public class Dam : ModelBase, ILocated
    {
        private Double _passability;

        public Dam()
        {
            OnDeserialized();   
        }
        
        public Dam(DamTableRow row)
        {
            Id = row.Id;
            Name = row.Name;
            Passability = row.Passability;
            OnDeserialized();
            Location.Longitude = row.Location.Longitude;
            Location.Latitude = row.Location.Latitude;
            Location.Symbol = Location.MapSymbol.X;
            Location.Color = Color.DarkRed;
        }
        
        [OnDeserialized]
        private void OnDeserialized(StreamingContext context = default(StreamingContext))
        {
            Location = Location ?? new Location();
        }

        [DataMember]
        public int Id { get; set; }
        
        /// <summary>
        /// Name of the dam
        /// </summary>
        [DataMember]
        public String Name { get; set; }
        
        [DataMember]
        public Location Location { get; private set; }

        /// <summary>
        /// The chance that a fish can pass through this dam (Pu * Pd)
        /// </summary>
        [DataMember]
        public Double Passability
        {
            get => _passability;
            set => Set(ref _passability, value);
        }
    }
}