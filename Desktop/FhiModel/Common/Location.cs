using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.Serialization;

namespace FhiModel.Common
{
    [DataContract(Namespace = "", IsReference = true)]
    public class Location : ModelBase
    {
        public Location()
        {
            OnDeserialized();   
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context = default(StreamingContext))
        {
            Points = Points ?? new List<Location>();

#pragma warning disable 612
            // temporary backward compatibility for sprint 4 files.
            if (X != null)
            {
                Longitude = X.Value;
                X = null;
            }
            if (Y != null)
            {
                Latitude = Y.Value;
                Y = null;
            }
#pragma warning restore 612
        }       
        
        [Obsolete]
        [DataMember]
        public Double? X { get; set; }
        [Obsolete]
        [DataMember]
        public Double? Y { get; set; }

        [DataMember]
        public Double Latitude { get; set; }
        
        [DataMember]
        public Double Longitude { get; set; }
        [DataMember]
        public Int32 Wkid { get; set; }
        [DataMember]
        public Color Color { get; set; }
        
        public enum MapSymbol { X, Circle, Cross, Diamond, Square, Triangle, Line }
        [DataMember]
        public MapSymbol Symbol { get; set; }
        
        [DataMember]
        public List<Location> Points { get; set; }
        
        /// <summary>
        /// Compute the linear distance from this point to the next.
        /// </summary>
        /// <param name="to">MapPoint to communicate distance to</param>
        /// <returns></returns>
        public Double Distance(Location to)
        {
            return Math.Sqrt(Math.Pow(Longitude - to.Longitude, 2) + Math.Pow(Latitude - to.Latitude, 2));
        }

        /// <summary>
        /// Could overload equals here, but it has unexpected side effects.
        /// </summary>
        /// <param name="other">The other point</param>
        /// <param name="error">Absolute distance is closer than the error is considered a match</param>
        /// <returns></returns>
        public Boolean Match(Location other, Double error = 0.001d)
        {
            return Distance(other) < error;
        }

        public override string ToString()
        {
            return $"{Longitude}, {Latitude}";
        }
    }
}