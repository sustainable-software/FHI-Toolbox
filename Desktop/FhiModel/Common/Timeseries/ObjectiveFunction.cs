using System;
using System.Runtime.Serialization;

namespace FhiModel.Common.Timeseries
{
    [KnownType(typeof(ObjectiveFunctionLessThan))]
    [KnownType(typeof(ObjectiveFunctionGreaterThan))]
    [KnownType(typeof(ObjectiveFunctionRange))]
    [DataContract(Namespace = "", IsReference = true)]
    public abstract class ObjectiveFunction : ModelBase
    {
        public abstract Boolean NonCompliant(Double value, ObjectiveMetric metric);
        public abstract Double Excursion(Double value, ObjectiveMetric metric);
    }

    [DataContract(Namespace = "", IsReference = true)]
    public class ObjectiveFunctionLessThan : ObjectiveFunction
    {
        public override Boolean NonCompliant(Double value, ObjectiveMetric metric)
        {
            if (!(metric is ObjectiveMetricSingleValue objective)) throw new ArgumentException("Metric must be single valued");
            return value < objective.Value;
        }

        public override Double Excursion(Double value, ObjectiveMetric metric)
        {
            if (!NonCompliant(value, metric)) return 0;
            if (!(metric is ObjectiveMetricSingleValue objective)) throw new ArgumentException("Metric must be single valued");
            
            if (value == 0.0)
                value = .00001;
            return Math.Abs(Math.Abs(objective.Value / value) - 1);
        }
        
        public override String ToString()
        {
            return "<";
        }
    }
    
    [DataContract(Namespace = "", IsReference = true)]
    public class ObjectiveFunctionGreaterThan : ObjectiveFunction
    {
        public override Boolean NonCompliant(Double value, ObjectiveMetric metric)
        {
            if (!(metric is ObjectiveMetricSingleValue objective)) throw new ArgumentException("Metric must be single valued");
            return value > objective.Value;
        }
        
        public override Double Excursion(Double value, ObjectiveMetric metric)
        {
            if (!NonCompliant(value, metric)) return 0;
            if (!(metric is ObjectiveMetricSingleValue objective)) throw new ArgumentException("Metric must be single valued");
            
            var denominator = objective.Value;
            if (denominator == 0.0)
                denominator = .00001;
            return Math.Abs(Math.Abs(value / denominator) - 1);
        }

        public override String ToString()
        {
            return ">";
        }
    }
    
    [DataContract(Namespace = "", IsReference = true)]
    public class ObjectiveFunctionRange : ObjectiveFunction
    {
        public override Boolean NonCompliant(Double value, ObjectiveMetric metric)
        {
            if (!(metric is ObjectiveMetricRange objective)) throw new ArgumentException("Metric must be range");
            return value < objective.Minimum || value > objective.Maximum;
        }
        
        public override Double Excursion(Double value, ObjectiveMetric metric)
        {
            if (!NonCompliant(value, metric)) return 0;
            if (!(metric is ObjectiveMetricRange objective)) throw new ArgumentException("Metric must be range");
            
            if (value < objective.Minimum)
            {
                if (value == 0.0)
                    value = .00001;
                return  Math.Abs(objective.Minimum / value) - 1;
            }
            
            var denominator = objective.Maximum;
            if (denominator == 0.0)
                denominator = .00001;
            return Math.Abs(Math.Abs(value / denominator) - 1);
        }
        
        public override String ToString()
        {
            return "<>";
        }
    }
}
