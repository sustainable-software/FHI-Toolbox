using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using FhiModel.Common;
using FhiModel.Common.Timeseries;
using FhiModel.EcosystemServices;

namespace FhiModel.EcosystemVitality.FlowDeviation
{
    [DataContract(Namespace = "", IsReference = true)]
    public class Station : ModelBase, ILocated
    {
        private Double? _flowDeviation;
        private String _name;
        private String _units;
        private ObservableCollection<TimeseriesDatum> _regulated;
        private ObservableCollection<TimeseriesDatum> _unregulated;
        private Double? _meanDischarge;
        private Double? _netAapfd;

        public Station()
        {
            OnDeserialized();   
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context = default(StreamingContext))
        {
            Regulated = Regulated ?? new ObservableCollection<TimeseriesDatum>();
            Unregulated = Unregulated ?? new ObservableCollection<TimeseriesDatum>();
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

        [DataMember]
        public Location Location { get; set; }

        [DataMember]
        public ObservableCollection<TimeseriesDatum> Regulated
        {
            get => _regulated;
            private set
            {
                if (!Set(ref _regulated, value)) return;
                if (_regulated == null) return;

                foreach (var ts in _regulated)
                    ts.PropertyChanged += DataOnPropertyChanged;
                
                _regulated.CollectionChanged+= (sender, args) =>
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
        public ObservableCollection<TimeseriesDatum> Unregulated
        {
            get => _unregulated;
            private set
            {
                if (!Set(ref _unregulated, value)) return;
                if (_unregulated == null) return;
                
                foreach (var ts in _unregulated)
                    ts.PropertyChanged += DataOnPropertyChanged;
                
                _unregulated.CollectionChanged+= (sender, args) =>
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
        public Double? NetAapfd
        {
            get => _netAapfd;
            private set => Set(ref _netAapfd, value);
        }

        [DataMember]
        public Double? MeanDischarge
        {
            get => _meanDischarge;
            private set => Set(ref _meanDischarge, value);
        }

        [DataMember]
        public Double[] RegulatedAverages { get; private set; }
        
        [DataMember]
        public Double[] UnregulatedAverages { get; private set; }
        
        public Double? FlowDeviation => _flowDeviation ?? (_flowDeviation = Compute());

        public override String ToString()
        {
            return $"{Name} : {FlowDeviation}";
        }

        private Boolean _computing;
        
        private Double? Compute()
        {
            if (Regulated.Count == 0 || Unregulated.Count == 0) return null;
            
            if (_computing) return null;
            _computing = true;
            
            RegulatedAverages = new Double[12];
            UnregulatedAverages = new Double[12];

            foreach (var month in Enumerable.Range(1, 12))
            {
                var rm = Regulated.Where(x => x.Time.Month == month).ToList();
                if (rm.Count > 0)
                    RegulatedAverages[month - 1] = rm.Average(x => x.Value);
                else
                    RegulatedAverages[month - 1] = 0;
                var um = Unregulated.Where(x => x.Time.Month == month).ToList();
                if (um.Count > 0)
                    UnregulatedAverages[month - 1] = um.Average(x => x.Value);
                else
                    UnregulatedAverages[month - 1] = 0;
            }

            NetAapfd = 0.0;
            var aapfd = new Dictionary<Int32, Double>();
            foreach (var rts in Regulated)
            {
                var uts = Unregulated.FirstOrDefault(x => x.Time == rts.Time);
                if (uts == null) continue;
                if (!aapfd.ContainsKey(rts.Time.Year))
                    aapfd.Add(rts.Time.Year, 0.0);
                aapfd[rts.Time.Year] += Math.Pow((rts.Value - uts.Value) / UnregulatedAverages[rts.Time.Month - 1], 2);
            }

            foreach (var year in aapfd.Keys.ToList())
                aapfd[year] = Math.Sqrt(aapfd[year]);

            NetAapfd = aapfd.Values.Average();
            MeanDischarge = RegulatedAverages.Average();
            
            Double? dvNf = 0.0;
            
            if (NetAapfd < 0.3)
                dvNf = 100 - (100 * NetAapfd);
            else if (NetAapfd < 0.5)
                dvNf = 85 - (50 * NetAapfd);
            else if (NetAapfd < 2)
                dvNf = 80 - (20 * NetAapfd);
            else if (NetAapfd < 5)
                dvNf = 50 - (10 * NetAapfd);
            
            _computing = false;
            return dvNf;
        }

        private void DataOnPropertyChanged(Object sender, PropertyChangedEventArgs e)
        {
            _flowDeviation = Compute();
            RaisePropertyChanged(nameof(FlowDeviation));
        }
    }
}