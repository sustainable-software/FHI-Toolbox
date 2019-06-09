using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.Serialization;
using FhiModel.Common;

namespace FhiModel.EcosystemVitality.Biodiversity
{
    [DataContract(Namespace = "", IsReference = true)]
    public class InvasiveSpeciesIndicator : Indicator
    {
        private Int32? _priorAssessmentInvasiveCount;
        private Int32? _priorAssessmentIndicator;
        private Double? _changeInSpeciesCountTerm;
        private Double? _populationTrendTerm;
        private Double? _invasiveIndexTerm;
        private ObservableCollection<Species> _includedSpecies;

        public InvasiveSpeciesIndicator()
        {
            OnDeserialized();   
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context = default(StreamingContext))
        {
            IncludedSpecies = IncludedSpecies ?? new ObservableCollection<Species>();
        }
        
        [DataMember]
        public ObservableCollection<Species> IncludedSpecies
        {
            get => _includedSpecies;
            set
            {
                if (!Set(ref _includedSpecies, value)) return;
                if (_includedSpecies == null) return;

                foreach (var species in _includedSpecies)
                    species.PropertyChanged += SpeciesOnPropertyChanged;

                _includedSpecies.CollectionChanged += (sender, args) =>
                {
                    switch (args.Action)
                    {
                        case NotifyCollectionChangedAction.Add:
                            foreach (var item in args.NewItems)
                            {
                                if (!(item is Species species)) continue;
                                species.PropertyChanged += SpeciesOnPropertyChanged;
                            }
                            break;
                        case NotifyCollectionChangedAction.Remove:
                            foreach (var item in args.OldItems)
                            {
                                if (!(item is Species species)) continue;
                                species.PropertyChanged -= SpeciesOnPropertyChanged;
                            }
                            break;
                    }
                    SpeciesOnPropertyChanged(null, null);
                };
            }
        }

        [DataMember]
        public Int32? PriorAssessmentIndicator
        {
            get => _priorAssessmentIndicator;
            set
            {
                if(Set(ref _priorAssessmentIndicator, value))
                    Value = null;
            }
        }

        [DataMember]
        public Int32? PriorAssessmentInvasiveCount
        {
            get => _priorAssessmentInvasiveCount;
            set
            {
                if (Set(ref _priorAssessmentInvasiveCount, value))
                    Value = null;
            }
        }

        [DataMember]
        public Double? ChangeInSpeciesCountTerm
        {
            get => _changeInSpeciesCountTerm;
            private set => Set(ref _changeInSpeciesCountTerm, value);
        }

        [DataMember]
        public Double? PopulationTrendTerm
        {
            get => _populationTrendTerm;
            private set => Set(ref _populationTrendTerm, value);
        }

        [DataMember]
        public Double? InvasiveIndexTerm
        {
            get => _invasiveIndexTerm;
            private set => Set(ref _invasiveIndexTerm, value);
        }

        protected override Int32? ComputeIndicator()
        {
            if (IncludedSpecies == null || IncludedSpecies.Count == 0)
            {
                ChangeInSpeciesCountTerm = null;
                PopulationTrendTerm = null;
                InvasiveIndexTerm = null;
                return null;
            }
           
            var populationTrend = 0.0;
            var trendCount = 0;
            foreach (var item in IncludedSpecies)
            {
                // population trend
                if (item.PopulationTrend == null) continue;
                
                populationTrend += item.PopulationTrend.Value;
                trendCount++;
            }
            
            var trend = trendCount > 0 ? Math.Exp(1.0 / trendCount * populationTrend) : 1;
            var deltaSpeciesCount = PriorAssessmentInvasiveCount.HasValue ? (Double)PriorAssessmentInvasiveCount.Value /  IncludedSpecies.Count : 1;
            var priorIndicator = PriorAssessmentIndicator ?? 100;
            var invasiveIndex = IncludedSpecies.Count >= 9 ? 0.1 : 1.0 - IncludedSpecies.Count / 10.0;

            ChangeInSpeciesCountTerm = deltaSpeciesCount;
            PopulationTrendTerm = trend;
            InvasiveIndexTerm = invasiveIndex;
            
            return (Int32) Math.Round(Math.Min(priorIndicator * invasiveIndex * deltaSpeciesCount * trend, 100), 0);
        }

        private void SpeciesOnPropertyChanged(Object sender, PropertyChangedEventArgs e)
        {
            Value = null;
        }
    }
}
