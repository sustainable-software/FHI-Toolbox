using System;
using System.Runtime.Serialization;

namespace FhiModel.Common.Timeseries
{
    [DataContract(Namespace = "", IsReference = true)]
    public class TimeseriesDatum : ModelBase
    {
        private DateTime _time;
        private Double _value;

        [DataMember]
        public DateTime Time
        {
            get => _time;
            set => Set(ref _time, value);
        }

        [DataMember]
        public Double Value
        {
            get => _value;
            set => Set(ref _value, value);
        }

        public override String ToString()
        {
            return $"{Time}:{Value}";
        }
    }
}