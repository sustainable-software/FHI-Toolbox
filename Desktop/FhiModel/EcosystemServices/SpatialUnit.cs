using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using FhiModel.Common;
using FhiModel.Common.Timeseries;

namespace FhiModel.EcosystemServices
{
    [KnownType(typeof(F1SpatialUnit))]
    [KnownType(typeof(F2SpatialUnit))]
    [KnownType(typeof(F3FuzzySpatialUnit))]
    [KnownType(typeof(F3SharpSpatialUnit))]
    [DataContract(Namespace = "", IsReference = true)]
    public abstract class SpatialUnit : ModelBase, ILocated
    {
        private String _name;
        private String _units;


        protected SpatialUnit()
        {
            OnDeserialized();
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context = default(StreamingContext))
        {
            Location = Location ?? new Location();
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

        [DataMember] public Location Location { get; set; }
    }

    [DataContract(Namespace = "", IsReference = true)]
    public class F1SpatialUnit : SpatialUnit
    {
        private Boolean? _nonCompliant;

        public F1SpatialUnit()
        {
            OnDeserialized();
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context = default(StreamingContext))
        {
        }

        [DataMember]
        public Boolean? NonCompliant
        {
            get => _nonCompliant;
            set => Set(ref _nonCompliant, value);
        }
    }

    [DataContract(Namespace = "", IsReference = true)]
    public class F2SpatialUnit : SpatialUnit
    {
        private Int32 _nonCompliantTimesteps;
        private ObservableCollection<ObjectiveResult> _results;

        public F2SpatialUnit()
        {
            OnDeserialized();
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context = default(StreamingContext))
        {
            Results = Results ?? new ObservableCollection<ObjectiveResult>();
        }

        [DataMember]
        public ObservableCollection<ObjectiveResult> Results
        {
            get => _results;
            protected set
            {
                if (!Set(ref _results, value)) return;
                if (_results == null) return;
                foreach (var result in _results)
                    result.PropertyChanged += ResultsOnPropertyChanged;

                _results.CollectionChanged += (sender, args) =>
                {
                    switch (args.Action)
                    {
                        case NotifyCollectionChangedAction.Add:
                            foreach (var item in args.NewItems)
                            {
                                if (!(item is ObjectiveResult result)) continue;
                                result.PropertyChanged += ResultsOnPropertyChanged;
                            }

                            break;
                        case NotifyCollectionChangedAction.Remove:
                            foreach (var item in args.OldItems)
                            {
                                if (!(item is ObjectiveResult result)) continue;
                                result.PropertyChanged -= ResultsOnPropertyChanged;
                            }

                            break;
                    }
                    ResultsOnPropertyChanged(null, null);
                };
            }
        }

        [DataMember]
        public Int32 NonCompliantTimesteps
        {
            get => _nonCompliantTimesteps;
            set => Set(ref _nonCompliantTimesteps, value);
        }

        private void ResultsOnPropertyChanged(Object sender, PropertyChangedEventArgs e)
        {
            if (!(Results?.Count > 0)) return;
            NonCompliantTimesteps = Results.Count(x => x.NonCompliant);
        }
    }

    [DataContract(Namespace = "", IsReference = true)]
    public abstract class F3SpatialUnit : SpatialUnit
    {
    }

    [DataContract(Namespace = "", IsReference = true)]
    public class F3FuzzySpatialUnit : F3SpatialUnit
    {
        private ObservableCollection<ObjectiveResult> _results;
        private Int32 _nonCompliantTimesteps;
        private Double _totalExcursions;
        
        public F3FuzzySpatialUnit()
        {
            OnDeserialized();
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context = default(StreamingContext))
        {
            Results = Results ?? new ObservableCollection<ObjectiveResult>();
        }

        [DataMember]
        public ObservableCollection<ObjectiveResult> Results
        {
            get => _results;
            protected set
            {
                if (!Set(ref _results, value)) return;
                if (_results == null) return;
                foreach (var result in _results)
                    result.PropertyChanged += ResultsOnPropertyChanged;

                _results.CollectionChanged += (sender, args) =>
                {
                    switch (args.Action)
                    {
                        case NotifyCollectionChangedAction.Add:
                            foreach (var item in args.NewItems)
                            {
                                if (!(item is ObjectiveResult result)) continue;
                                result.PropertyChanged += ResultsOnPropertyChanged;
                            }

                            break;
                        case NotifyCollectionChangedAction.Remove:
                            foreach (var item in args.OldItems)
                            {
                                if (!(item is ObjectiveResult result)) continue;
                                result.PropertyChanged -= ResultsOnPropertyChanged;
                            }

                            break;
                    }
                    ResultsOnPropertyChanged(null, null);
                };
            }
        }

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

        private void ResultsOnPropertyChanged(Object sender, PropertyChangedEventArgs e)
        {
            if (!(Results?.Count > 0)) return;
            NonCompliantTimesteps = Results.Count(x => x.Value > 0);
            TotalExcursions = Results.Sum(x => x.Value);
        }
    }

    [DataContract(Namespace = "", IsReference = true)]
    public class F3SharpSpatialUnit : F3SpatialUnit
    {
        private Int32 _nonCompliantTimesteps;
        private Double _totalExcursions;
        private Objective _objective;
        private ObservableCollection<ObjectiveResult> _results;
        private ObservableCollection<TimeseriesDatum> _data;

        public F3SharpSpatialUnit()
        {
            OnDeserialized();
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context = default(StreamingContext))
        {
            Data = Data ?? new ObservableCollection<TimeseriesDatum>();
            Objective = Objective ?? new Objective();
            Objective.PropertyChanged += DataOnPropertyChanged;
            DataOnPropertyChanged(null, null);
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

        [IgnoreDataMember]
        public Int32 NonCompliantTimesteps
        {
            get => _nonCompliantTimesteps;
            set => Set(ref _nonCompliantTimesteps, value);
        }

        [IgnoreDataMember]
        public Double TotalExcursions
        {
            get => _totalExcursions;
            set => Set(ref _totalExcursions, value);
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