using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using FhiModel.Common;

namespace FhiModel.EcosystemVitality
{
    [DataContract(Namespace = "", IsReference = true)]
    public class LandCoverItem : ModelBase
    {
        private string _naturalness;
        private string _characteristics;
        private string _examples;
        private int? _weight;
        private double? _area;
        private List<byte> _mapping;

        [DataMember]
        public String Naturalness
        {
            get => _naturalness;
            set => Set(ref _naturalness, value);
        }

        [DataMember]
        public String Characteristics
        {
            get => _characteristics;
            set => Set(ref _characteristics, value);
        }

        [IgnoreDataMember]
        public String Category => $"{Naturalness}: {Characteristics}";

        [DataMember]
        public String Examples
        {
            get => _examples;
            set => Set(ref _examples, value);
        }

        [DataMember]
        public Int32? Weight
        {
            get => _weight;
            set => Set(ref _weight, value);
        }

        [DataMember]
        public Double? Area
        {
            get => _area;
            set => Set(ref _area, value);
        }

        [DataMember]
        public List<Byte> Mapping
        {
            get => _mapping;
            set => Set(ref _mapping, value);
        }
    }
}
