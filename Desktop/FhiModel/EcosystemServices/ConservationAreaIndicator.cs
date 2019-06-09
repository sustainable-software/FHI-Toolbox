using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;
using System.Text;
using FhiModel.Common;

namespace FhiModel.EcosystemServices
{
    [DataContract(Namespace = "", IsReference = true)]
    public class ConservationAreaIndicator : Indicator
    {
        private double? _totalProtectedArea;
        private double _targetProtectedAreaPercent = 20;
        private double? _totalProtectedLength;
        private double _targetProtectedLengthPercent = 17;
        private double? _totalArea;
        private double? _totalLength;
        private ProtectedTarget _target;

        public ConservationAreaIndicator()
        {
            OnDeserialized();
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context = default(StreamingContext))
        {
            AssetNames = AssetNames ?? new ObservableCollection<string>();
        }

        [DataMember]
        public Double? TotalProtectedArea
        {
            get => _totalProtectedArea;
            set
            {
                Set(ref _totalProtectedArea, value);
                RaisePropertyChanged(nameof(TotalProtectedAreaPercent));
                Value = null;
            }
        }

        [DataMember]
        public Double? TotalProtectedLength
        {
            get => _totalProtectedLength;
            set
            {
                Set(ref _totalProtectedLength, value);
                RaisePropertyChanged(nameof(TotalProtectedLengthPercent));
                Value = null;
            }
        }

        [DataMember]
        public Double? TotalArea
        {
            get => _totalArea;
            set
            {
                Set(ref _totalArea, value); 
                RaisePropertyChanged(nameof(TotalProtectedAreaPercent));
                Value = null;
            }
        }

        [DataMember]
        public Double? TotalLength
        {
            get => _totalLength;
            set
            {
                Set(ref _totalLength, value);
                RaisePropertyChanged(nameof(TotalProtectedLengthPercent));
                Value = null;
            }
        }

        [DataMember]
        public Double TargetProtectedAreaPercent
        {
            get => _targetProtectedAreaPercent;
            set
            {
                Set(ref _targetProtectedAreaPercent, value);
                Value = null;
            }
        }

        [DataMember]
        public Double TargetProtectedLengthPercent
        {
            get => _targetProtectedLengthPercent;
            set
            {
                Set(ref _targetProtectedLengthPercent, value);
                Value = null;
            }
        }

        public enum ProtectedTarget { Area, Length }

        [DataMember]
        public ProtectedTarget Target
        {
            get => _target;
            set
            {
                if (Set(ref _target, value))
                    Value = null;
            }
        }

        [DataMember]
        public ObservableCollection<String> AssetNames { get; private set; }

        public Double? TotalProtectedAreaPercent => 100.0 * TotalProtectedArea / TotalArea;

        public Double? TotalProtectedLengthPercent => 100.0 * TotalProtectedLength / TotalLength;

        protected override Int32? ComputeIndicator()
        {
            switch (Target)
            {
                case ProtectedTarget.Area:
                    if (TotalProtectedAreaPercent != null)
                        return (Int32)Math.Min(100, Math.Round(100.0 * TotalProtectedAreaPercent.Value / TargetProtectedAreaPercent, 0));
                    break;
                case ProtectedTarget.Length:
                    if (TotalProtectedLengthPercent != null)
                        return (Int32)Math.Min(100, Math.Round(100.0 * TotalProtectedLengthPercent.Value / TargetProtectedLengthPercent, 0));
                    break;
            }
            return null;
        }
    }
}
