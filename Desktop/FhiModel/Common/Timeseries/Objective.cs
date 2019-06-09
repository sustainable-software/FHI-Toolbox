using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Runtime.Serialization;

namespace FhiModel.Common.Timeseries
{
    [DataContract(Namespace = "", IsReference = true)]
    public class Objective : ModelBase
    {
        private ObjectiveFunction _function;
        private ObservableCollection<ObjectiveMetric> _metrics;

        public Objective()
        {
            OnDeserialized();   
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context = default(StreamingContext))
        {
            Metrics = Metrics ?? new ObservableCollection<ObjectiveMetric>();
        }

        [DataMember]
        public ObjectiveFunction Function
        {
            get => _function;
            set => Set(ref _function, value);
        }

        [DataMember]
        public ObservableCollection<ObjectiveMetric> Metrics
        {
            get => _metrics;
            set
            {
                if(!Set(ref _metrics, value)) return;
                if (_metrics == null) return;
                foreach (var metric in _metrics)
                    metric.PropertyChanged += (sender, args) => RaisePropertyChanged();
                _metrics.CollectionChanged += (sender, args) =>
                {
                    switch (args.Action)
                    {
                        case NotifyCollectionChangedAction.Add:
                            foreach (var item in args.NewItems)
                            {
                                if (!(item is ObjectiveMetric metric)) continue;
                                metric.PropertyChanged += (s, a) => RaisePropertyChanged();
                            }
                            break;                       
                    }
                    RaisePropertyChanged();
                };
            }
        }

        public ObjectiveResult Compute(TimeseriesDatum data)
        {
            foreach (var metric in Metrics)
            {
                if (data.Time >= metric.Start && data.Time <= metric.End)
                {
                    return new ObjectiveResult
                    {
                        Time = data.Time,
                        Value = Function.Excursion(data.Value, metric),
                        NonCompliant = Function.NonCompliant(data.Value, metric)
                    };
                }
            }

            // we didn't match a specific date, so we're going to see if the match failure is the year.
            foreach (var metric in Metrics)
            {
                var start = new DateTime(data.Time.Year, metric.Start.Month, metric.Start.Day, metric.Start.Hour, metric.Start.Minute, metric.Start.Second);
                var end = new DateTime(data.Time.Year, metric.End.Month, metric.End.Day, metric.End.Hour, metric.End.Minute, metric.End.Second);
                if (data.Time >= start && data.Time <= end)
                {
                    return new ObjectiveResult
                    {
                        Time = data.Time,
                        Value = Function.Excursion(data.Value, metric),
                        NonCompliant = Function.NonCompliant(data.Value, metric)
                    };
                }
            }
            // no metric
            return new ObjectiveResult {Time = data.Time, NoMetric = true};
        }
    }

    [DataContract(Namespace = "", IsReference = true)]
    public class ObjectiveResult : ModelBase
    {
        private DateTime _time;
        private Double _value;
        private Boolean _nonCompliant;
        private bool _noMetric;

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

        [DataMember]
        public Boolean NonCompliant
        {
            get => _nonCompliant;
            set => Set(ref _nonCompliant, value);
        }

        [DataMember]
        public Boolean NoMetric
        {
            get => _noMetric;
            set => Set(ref _noMetric, value);
        }

        public override String ToString()
        {
            return $"{Time} : {Value}" + (NonCompliant ? " NC" : "");
        }
    }
}