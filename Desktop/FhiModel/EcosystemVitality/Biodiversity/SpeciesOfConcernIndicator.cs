using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.Serialization;
using FhiModel.Common;
using FhiModel.EcosystemVitality.FlowDeviation;

namespace FhiModel.EcosystemVitality.Biodiversity
{
    [DataContract(Namespace = "", IsReference = true)]
    public class SpeciesOfConcernIndicator : Indicator
    {
        private Int32? _priorAssessmentSocCount;
        private Int32? _priorAssessmentIndicator;
        private Double? _endangeredProportionTerm;
        private Double? _changeInSpeciesCountTerm;
        private Double? _populationTrendTerm;
        private ObservableCollection<Species> _includedSpecies;

        public SpeciesOfConcernIndicator()
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
        public Int32? PriorAssessmentSocCount
        {
            get => _priorAssessmentSocCount;
            set
            {
                if (Set(ref _priorAssessmentSocCount, value))
                    Value = null;
            }
        }

        [DataMember]
        public Double? EndangeredProportionTerm
        {
            get => _endangeredProportionTerm;
            set => Set(ref _endangeredProportionTerm, value);
        }

        [DataMember]
        public Double? ChangeInSpeciesCountTerm
        {
            get => _changeInSpeciesCountTerm;
            set => Set(ref _changeInSpeciesCountTerm, value);
        }

        [DataMember]
        public Double? PopulationTrendTerm
        {
            get => _populationTrendTerm;
            set => Set(ref _populationTrendTerm, value);
        }

        private static readonly Dictionary<RedListCode, Double> _weights = new Dictionary<RedListCode, Double>
        {
            { RedListCode.CR, 3.0},
            { RedListCode.EN, 2.0},
            { RedListCode.VU, 1.0},
            { RedListCode.LC, 0.5},
            { RedListCode.NT, 0.5},
            { RedListCode.DD, 0.0},
            { RedListCode.NONE, 0.0}
        };
        
        private static readonly HashSet<RedListCode> _concern = new HashSet<RedListCode>
        {
            RedListCode.CR, 
            RedListCode.EN, 
            RedListCode.VU
        };
        
        protected override Int32? ComputeIndicator()
        {
            if (IncludedSpecies == null || IncludedSpecies.Count == 0)
            {
                EndangeredProportionTerm = null;
                ChangeInSpeciesCountTerm = null;
                PopulationTrendTerm = null;
                return null;
            }
            
            var numerator = 0.0;
            var denominator = 0.0;
            var populationTrend = 0.0;
            var trendCount = 0;
            var socTotal = 0;
            
            foreach (var item in IncludedSpecies)
            {
                // endangered proportion
                if (_concern.Contains(item.Code))
                    numerator += _weights[item.Code];
                denominator += _weights[item.Code];

                // population trend
                if (_concern.Contains(item.Code) && item.PopulationTrend != null)
                {
                    populationTrend += item.PopulationTrend.Value;
                    trendCount++;
                }

                // total count of species of concern
                if (_concern.Contains(item.Code))
                    socTotal++;
            }

            if (denominator == 0.0) return null;
            
            var threatenedEndangeredProportion = 1 - numerator / denominator;
            var deltaSoc = PriorAssessmentSocCount.HasValue ? (Double)PriorAssessmentSocCount / socTotal : 1;
            var trend = trendCount > 0 ? Math.Exp(1.0 / trendCount * populationTrend) : 1;
            var priorIndicator = PriorAssessmentIndicator ?? 100;

            EndangeredProportionTerm = threatenedEndangeredProportion;
            ChangeInSpeciesCountTerm = deltaSoc;
            PopulationTrendTerm = trend;
            
            return (Int32) Math.Round(Math.Min(priorIndicator * threatenedEndangeredProportion * deltaSoc * trend, 100), 0);
        }

        private void SpeciesOnPropertyChanged(Object sender, PropertyChangedEventArgs e)
        {
            Value = null;
        }
    }
}