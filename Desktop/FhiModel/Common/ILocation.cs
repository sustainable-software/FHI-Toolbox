using System;
using System.Runtime.Serialization;

namespace FhiModel.Common
{
    public interface ILocation
    {
        Double Latitude { get; set; }
        Double Longitude { get; set; }
    }
}