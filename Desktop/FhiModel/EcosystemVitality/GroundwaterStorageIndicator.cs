using System;
using System.Runtime.Serialization;
using FhiModel.Common;

namespace FhiModel.EcosystemVitality
{
    [DataContract(Namespace = "", IsReference = true)]
    public class GroundwaterStorageIndicator : Indicator
    {
        private Double? _basinArea;
        private Double? _affectedArea;

        [DataMember]
        public Double? AffectedArea
        {
            get => _affectedArea;
            set
            {
                if (Set(ref _affectedArea, value))
                    Value = null;
            }
        }

        [DataMember]
        public Double? BasinArea
        {
            get => _basinArea;
            set
            {
                if (Set(ref _basinArea, value))
                    Value = null;
            }
        }

        protected override Int32? ComputeIndicator()
        {
            if (AffectedArea == null || BasinArea == null) return null;
            if (BasinArea <= 0.0 || AffectedArea <= 0.0) return null;
            
            return (Int32)Math.Round(100.0 * (1.0 - (Double)AffectedArea/(Double)BasinArea), 0);
        }
    }
}