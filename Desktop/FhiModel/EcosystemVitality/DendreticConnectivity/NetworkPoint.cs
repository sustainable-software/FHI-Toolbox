using System;
using System.Runtime.Serialization;
using FhiModel.Common;

namespace FhiModel.EcosystemVitality.DendreticConnectivity
{
    [DataContract(Namespace = "", IsReference = true)]
    public class NetworkPoint : ModelBase
    {
        public NetworkPoint() {}

        public NetworkPoint(double x, double y)
        {
            X = x;
            Y = y;
        }
        
        [DataMember]
        public Double X { get; set; }
        [DataMember]
        public Double Y { get; set; }

        /// <summary>
        /// Compute the linear distance from this point to the next.
        /// </summary>
        /// <param name="to">MapPoint to communicate distance to</param>
        /// <returns></returns>
        public Double Distance(NetworkPoint to)
        {
            return Math.Sqrt(Math.Pow(X - to.X, 2) + Math.Pow(Y - to.Y, 2));
        }

        /// <summary>
        /// Could overload equals here, but it has unexpected side effects.
        /// </summary>
        /// <param name="other">The other point</param>
        /// <param name="error">Absolute distance is closer than the error is considered a match</param>
        /// <returns></returns>
        public Boolean Match(NetworkPoint other, Double error = 0.001d)
        {
            return Distance(other) < error;
        }

        public override string ToString()
        {
            return $"{X}, {Y}";
        }
    }
}