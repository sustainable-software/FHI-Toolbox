using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace FhiModel.Governance
{
    [DataContract(Namespace = "", IsReference = true)]
    public class Question
    {
        [DataMember]
        public String Name { get; set; }
        [DataMember]
        public Double? Weight { get; set; }
        [DataMember]
        public Int32? Score { get; set; }
        [DataMember]
        public List<Answer> Answers { get; set; }

        public override String ToString()
        {
            return Name;
        }
    }
}