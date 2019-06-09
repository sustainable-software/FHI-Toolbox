using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using FhiModel.Common;

namespace FhiModel.EcosystemVitality.WaterQuality
{
    [DataContract(Namespace = "", IsReference = true)]
    public class WaterQualityIndicator : Indicator
    {
        private ObservableCollection<Gauge> _gauges;

        public WaterQualityIndicator()
        {
            OnDeserialized();   
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context = default(StreamingContext))
        {
            Gauges = Gauges ?? new ObservableCollection<Gauge>();
            GaugesOnPropertyChanged(null, null);
        }

        [DataMember]
        public ObservableCollection<Gauge> Gauges
        {
            get => _gauges;
            set
            {
                if (!Set(ref _gauges, value)) return;
                if (_gauges == null) return;
                
                foreach (var gauge in _gauges)
                    gauge.PropertyChanged += GaugesOnPropertyChanged;
                
                _gauges.CollectionChanged += (sender, args) =>
                {
                    switch (args.Action)
                    {
                        case NotifyCollectionChangedAction.Add:
                            foreach (var item in args.NewItems)
                            {
                                if (!(item is Gauge gauge)) continue;
                                gauge.PropertyChanged += GaugesOnPropertyChanged;
                            }
                            break;
                        case NotifyCollectionChangedAction.Remove:
                            foreach (var item in args.OldItems)
                            {
                                if (!(item is Gauge gauge)) continue;
                                gauge.PropertyChanged -= GaugesOnPropertyChanged;
                            }
                            break;
                    }
                    GaugesOnPropertyChanged(null, null);
                };
            }
        }

        protected override Int32? ComputeIndicator()
        {
            if (!(Gauges?.Count > 0)) return null;
            var average = Gauges.Average(x => x.Value);
            if (average == null) return null;
            return (Int32)Math.Round(average.Value, 0);
        }
        
        private void GaugesOnPropertyChanged(Object sender, PropertyChangedEventArgs e)
        {
            Value = null;
        }
    }
}