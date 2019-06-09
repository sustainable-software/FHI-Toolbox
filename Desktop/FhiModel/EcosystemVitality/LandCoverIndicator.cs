using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.Serialization;
using FhiModel.Common;
using FhiModel.Services;

namespace FhiModel.EcosystemVitality
{
    [DataContract(Namespace = "", IsReference = true)]
    public class LandCoverIndicator : Indicator, ICoverage
    {
        private ObservableCollection<LandCoverItem> _coverage;

        public LandCoverIndicator()
        {
            OnDeserialized();
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context = default(StreamingContext))
        {
            Coverage = Coverage ?? new ObservableCollection<LandCoverItem>();
            if (Coverage.Count == 0 || Coverage[0].Mapping == null)
            {
                Coverage.Clear();
                foreach (var item in LandCoverTableService.GetTable("ESACCI LCCS"))
                    Coverage.Add(item.Clone());
            }
        }

        [DataMember]
        public ObservableCollection<LandCoverItem> Coverage
        {
            get => _coverage;
            private set
            {
                if (!Set(ref _coverage, value)) return;
                if (_coverage == null) return;

                foreach (var c in _coverage)
                    c.PropertyChanged += CoverageOnPropertyChanged;

                _coverage.CollectionChanged += (sender, args) =>
                {
                    switch (args.Action)
                    {
                        case NotifyCollectionChangedAction.Add:
                            foreach (var item in args.NewItems)
                            {
                                if (!(item is LandCoverItem lc)) continue;
                                lc.PropertyChanged += CoverageOnPropertyChanged;
                            }

                            break;
                        case NotifyCollectionChangedAction.Remove:
                            foreach (var item in args.OldItems)
                            {
                                if (!(item is LandCoverItem lc)) continue;
                                lc.PropertyChanged -= CoverageOnPropertyChanged;
                            }

                            break;
                    }
                    CoverageOnPropertyChanged(null, null);
                };
            }
        }

        protected override Int32? ComputeIndicator()
        {
            if (!(Coverage?.Count > 0)) return null;
            var sum = 0.0;
            var area = 0.0;
            var weights = 0.0;
            foreach (var item in Coverage)
            {
                if (!item.Weight.HasValue || !item.Area.HasValue) continue;
                sum += item.Area.Value * item.Weight.Value;
                area += item.Area.Value;
                weights += item.Weight.Value;
            }

            if (area == 0.0) return null;
            return (Int32)Math.Round(sum / area, 0);
        }

        private void CoverageOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Value = null;
        }
    }
}