using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using FhiModel.Common;

namespace FhiModel.EcosystemServices
{
    public enum Confidence {F1, F2, F3Fuzzy, F3Sharp}
    
    [DataContract(Namespace = "", IsReference = true)]
    public class EcosystemServicesIndicator : Indicator
    {
        private Confidence _evidenceLevel;
        private ObservableCollection<SpatialUnit> _spatialUnits;

        public EcosystemServicesIndicator()
        {
            OnDeserialized();   
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context = default(StreamingContext))
        {
            SpatialUnits = SpatialUnits ?? new ObservableCollection<SpatialUnit>();
            if (Rank == Uncertainty.Undefined)
            {
                switch (EvidenceLevel)
                {
                    case Confidence.F1:
                        Rank = Uncertainty.High;
                        break;
                    case Confidence.F2:
                        Rank = Uncertainty.Medium;
                        break;
                    case Confidence.F3Sharp:
                    case Confidence.F3Fuzzy:
                        Rank = Uncertainty.Low;
                        break;
                }
            }
        }

        [DataMember]
        public ObservableCollection<SpatialUnit> SpatialUnits
        {
            get => _spatialUnits;
            set
            {
                if (!Set(ref _spatialUnits, value)) return;
                if (_spatialUnits == null) return;
                
                foreach (var spatialUnit in _spatialUnits)
                    spatialUnit.PropertyChanged += SpatialUnitOnPropertyChanged;
                
                _spatialUnits.CollectionChanged += (sender, args) =>
                {
                    switch (args.Action)
                    {
                        case NotifyCollectionChangedAction.Add:
                            foreach (var item in args.NewItems)
                            {
                                if (!(item is SpatialUnit su)) continue;
                                su.PropertyChanged += SpatialUnitOnPropertyChanged;
                            }
                            break;
                        case NotifyCollectionChangedAction.Remove:
                            foreach (var item in args.OldItems)
                            {
                                if (!(item is SpatialUnit su)) continue;
                                su.PropertyChanged -= SpatialUnitOnPropertyChanged;
                            }
                            break;
                    }
                    SpatialUnitOnPropertyChanged(null, null);
                };
            }
        }

        [DataMember]
        public Confidence EvidenceLevel
        {
            get => _evidenceLevel;
            set => Set(ref _evidenceLevel, value);
        }

        [DataMember]
        public Int32? FailedTimestepsTerm { get; set; }
        [DataMember]
        public Int32? TotalTimestepsTerm { get; set; }
        [DataMember]
        public Int32? FailedSuTerm { get; set; }       
        [DataMember]
        public Double? ExcursionSumTerm { get; set; }
        
        public Int32? TotalSuTerm => SpatialUnits?.Count;

        public Double? NormalizedSumExcursions => ExcursionSumTerm / TotalTimestepsTerm;
        

        public Int32? F1 =>
            FailedSuTerm.HasValue && TotalSuTerm.HasValue
                ? (Int32) Math.Round(((Double) FailedSuTerm / (Double) TotalSuTerm) * 100.0, 0)
                : (Int32?) null;
        
        public Int32? F2 => 
            FailedTimestepsTerm.HasValue && TotalTimestepsTerm.HasValue 
                ? (Int32) Math.Round(((Double)FailedTimestepsTerm /(Double) TotalTimestepsTerm) * 100.0, 0) 
                : (Int32?) null;

        public Int32? F3 =>
            NormalizedSumExcursions.HasValue
                ? (Int32) Math.Round(((Double) NormalizedSumExcursions / (Double) (NormalizedSumExcursions + 1.0)) * 100.0, 0)
                : (Int32?) null;


        public Int32? Esi1 => 100 - F1;

        public Int32? Esi2 => F1.HasValue && F2.HasValue
            ? (Int32) Math.Round(100.0 - Math.Sqrt(F1.Value * F2.Value))
            : (Int32?) null;

        public Int32? Esi3 => F1.HasValue && F3.HasValue
            ? (Int32) Math.Round(100.0 - Math.Sqrt(F1.Value * F3.Value))
            : (Int32?) null;
        
        protected override Int32? ComputeIndicator()
        {
            if (!(SpatialUnits?.Count > 0)) return null;
            
            switch (EvidenceLevel)
            {
                case Confidence.F3Sharp:
                case Confidence.F3Fuzzy:
                    ExcursionSumTerm = 0;
                    TotalTimestepsTerm = 0;
                    FailedTimestepsTerm = 0;
                    break;
                case Confidence.F2:
                    TotalTimestepsTerm = 0;
                    FailedTimestepsTerm = 0;
                    break;     
            }
            
            FailedSuTerm = 0;
            
            foreach (var su in SpatialUnits)
            {
                switch (su)
                {
                    case F1SpatialUnit f1:
                        if (f1.NonCompliant == true)
                            FailedSuTerm++;
                        break;
                    case F2SpatialUnit f2:
                    {
                        if (!(f2.Results?.Count > 0)) continue;
                        TotalTimestepsTerm += f2.Results.Count;
                        var failed = f2.Results.Count(x => x.NonCompliant);
                        if (failed > 0)
                            FailedSuTerm++;
                        FailedTimestepsTerm += failed;
                        break;
                    }
                    case F3FuzzySpatialUnit f3Fuzzy:
                    {
                        if (!(f3Fuzzy.Results?.Count > 0)) continue;
                        ExcursionSumTerm += f3Fuzzy.Results.Sum(x => x.Value);
                        TotalTimestepsTerm += f3Fuzzy.Results.Count;
                        var failed = f3Fuzzy.Results.Count(x => x.Value > 0);
                        if (failed > 0)
                            FailedSuTerm++;
                        FailedTimestepsTerm += failed;
                        break;
                    }
                    case F3SharpSpatialUnit f3Sharp:
                    {
                        if (f3Sharp.Results == null) continue;
                        ExcursionSumTerm += f3Sharp.Results.Sum(x => x.Value);
                        TotalTimestepsTerm += f3Sharp.Results.Count;
                        var failed = f3Sharp.Results.Count(x => x.NonCompliant);
                        if (failed > 0)
                            FailedSuTerm++;
                        FailedTimestepsTerm += failed;
                        break;
                    }
                }
            }

            switch (EvidenceLevel)
            {
                case Confidence.F3Sharp:
                case Confidence.F3Fuzzy:
                    return Esi3;
                case Confidence.F2:
                    return Esi2;
                case Confidence.F1:
                    return Esi1;
                default:
                    return null;
            }
        }
        
        private void SpatialUnitOnPropertyChanged(Object sender, PropertyChangedEventArgs e)
        {
            Value = null;
        }
    }
}
