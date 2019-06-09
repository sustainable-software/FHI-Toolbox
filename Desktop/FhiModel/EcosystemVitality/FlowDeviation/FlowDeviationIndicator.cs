using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.Serialization;
using FhiModel.Common;

namespace FhiModel.EcosystemVitality.FlowDeviation
{
    [DataContract(Namespace = "", IsReference = true)]
    public class FlowDeviationIndicator : Indicator
    {
        private ObservableCollection<Station> _stations;

        public FlowDeviationIndicator()
        {
            OnDeserialized();   
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context = default(StreamingContext))
        {
            Stations = Stations ?? new ObservableCollection<Station>();
        }

        [DataMember]
        public ObservableCollection<Station> Stations
        {
            get => _stations;
            private set
            {
                if (!Set(ref _stations, value)) return;
                if (_stations == null) return;
                
                foreach (var station in _stations)
                    station.PropertyChanged += StationsOnPropertyChanged;
                
                _stations.CollectionChanged += (sender, args) =>
                {
                    switch (args.Action)
                    {
                        case NotifyCollectionChangedAction.Add:
                            foreach (var item in args.NewItems)
                            {
                                if (!(item is Station station)) continue;
                                station.PropertyChanged += StationsOnPropertyChanged;
                            }
                            break;
                        case NotifyCollectionChangedAction.Remove:
                            foreach (var item in args.OldItems)
                            {
                                if (!(item is Station station)) continue;
                                station.PropertyChanged -= StationsOnPropertyChanged;
                            }
                            break;
                    }
                    StationsOnPropertyChanged(null, null);
                };
            }
        }

        protected override Int32? ComputeIndicator()
        {
            if (!(Stations?.Count > 0)) return null;
            var sum = 0.0;
            var weights = 0.0;
            foreach (var station in Stations)
            {
                if (!station.FlowDeviation.HasValue || !station.MeanDischarge.HasValue) continue;
                sum += station.FlowDeviation.Value * station.MeanDischarge.Value;
                weights += station.MeanDischarge.Value;
            } 
            // weight the station value by the mean discharge for the year
            if (sum == 0.0) return null;
            return (Int32) Math.Round(sum/weights, 0);
        }
        
        private void StationsOnPropertyChanged(Object sender, PropertyChangedEventArgs e)
        {
            Value = null;
        }
    }
}