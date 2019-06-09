using System;
using System.Runtime.Serialization;

namespace FhiModel.Common.Timeseries
{
    [KnownType(typeof(ObjectiveMetricSingleValue))]
    [KnownType(typeof(ObjectiveMetricRange))]
    [DataContract(Namespace = "", IsReference = true)]
    public abstract class ObjectiveMetric : ModelBase
    {
        private DateTime _start;
        private DateTime _end;

        [DataMember]
        public DateTime Start
        {
            get => _start;
            set => Set(ref _start, value);
        }

        [DataMember]
        public DateTime End
        {
            get => _end;
            set => Set(ref _end, value);
        }
    }

    [DataContract(Namespace = "", IsReference = true)]
    public class ObjectiveMetricSingleValue : ObjectiveMetric
    {
        private Double _value;

        [DataMember]
        public Double Value
        {
            get => _value;
            set => Set(ref _value, value);
        }

        public override String ToString()
        {
            return $"{Start} - {End} : {Value}";
        }
    }

    [DataContract(Namespace = "", IsReference = true)]
    public class ObjectiveMetricRange : ObjectiveMetric
    {
        private Double _minimum;
        private Double _maximum;

        [DataMember]
        public Double Minimum
        {
            get => _minimum;
            set => Set(ref _minimum, value);
        }

        [DataMember]
        public Double Maximum
        {
            get => _maximum;
            set => Set(ref _maximum, value);
        }

        public override String ToString()
        {
            return $"{Start} - {End} : {Minimum}/{Maximum}";
        }
    }
}