using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using FhiModel.Common;
using FhiModel.Common.Timeseries;

namespace FhiModel.EcosystemVitality.WaterQuality
{
    [DataContract(Namespace = "", IsReference = true)]
    public class WaterQualityParameter : ModelBase
    {
        private Objective _objective;
        private ObservableCollection<ObjectiveResult> _results;
        private String _name;
        private String _units;
        private ObservableCollection<TimeseriesDatum> _data;
        private Int32 _nonCompliantTimesteps;
        private Double _totalExcursions;

        public WaterQualityParameter()
        {
            OnDeserialized();    
        }
        
        [OnDeserialized]
        private void OnDeserialized(StreamingContext context = default(StreamingContext))
        {
            Data = Data ?? new ObservableCollection<TimeseriesDatum>();
            Objective = Objective ?? new Objective();
            Objective.PropertyChanged += DataOnPropertyChanged;
        }

        [DataMember]
        public String Name
        {
            get => _name;
            set => Set(ref _name, value);
        }

        [DataMember]
        public String Units
        {
            get => _units;
            set => Set(ref _units, value);
        }

        [DataMember]
        public ObservableCollection<TimeseriesDatum> Data
        {
            get => _data;
            private set
            {
                if (!Set(ref _data, value)) return;
                if (_data == null) return;
                foreach (var datum in _data)
                    datum.PropertyChanged += DataOnPropertyChanged;

                _data.CollectionChanged += (sender, args) =>
                {
                    switch (args.Action)
                    {
                        case NotifyCollectionChangedAction.Add:
                            foreach (var item in args.NewItems)
                            {
                                if (!(item is TimeseriesDatum datum)) continue;
                                datum.PropertyChanged += DataOnPropertyChanged;
                            }

                            break;
                        case NotifyCollectionChangedAction.Remove:
                            foreach (var item in args.OldItems)
                            {
                                if (!(item is TimeseriesDatum datum)) continue;
                                datum.PropertyChanged -= DataOnPropertyChanged;
                            }

                            break;
                    }
                    DataOnPropertyChanged(null, null);
                };
            }
        }

        [DataMember]
        public Objective Objective
        {
            get => _objective;
            set => Set(ref _objective, value);
        }

        [IgnoreDataMember]
        public ObservableCollection<ObjectiveResult> Results => _results ?? (_results = ComputeResults());
        
        [DataMember]
        public Int32 NonCompliantTimesteps
        {
            get => _nonCompliantTimesteps;
            set => Set(ref _nonCompliantTimesteps, value);
        }

        [DataMember]
        public Double TotalExcursions
        {
            get => _totalExcursions;
            set => Set(ref _totalExcursions, value);
        }

        public override String ToString()
        {
            return $"{Name} [{Units}]";
        }

        private ObservableCollection<ObjectiveResult> ComputeResults()
        {
            return !(Data?.Count > 0) || Objective.Metrics.Count == 0
                ? null
                : new ObservableCollection<ObjectiveResult>(Data.Select(x => Objective.Compute(x)));
        }
        
        private void DataOnPropertyChanged(Object sender, PropertyChangedEventArgs e)
        {
            NonCompliantTimesteps = 0;
            TotalExcursions = 0;
            _results = ComputeResults();
            if (!(Results?.Count > 0)) return;
            NonCompliantTimesteps = Results.Count(x => x.NonCompliant);
            TotalExcursions = Results.Sum(x => x.Value);
        }
    }
}