using System;
using System.Runtime.Serialization;
using System.Text;

namespace FhiModel.Governance
{
    [DataContract(Namespace = "", IsReference = true)]
    public class Answer
    {
        [DataMember]
        public String User { get; set; }
        [DataMember]
        public int? Value { get; set; }
        [DataMember]
        public double? Weight { get; set; }
        [DataMember]
        public String Comment { get; set; }
        
        public override String ToString()
        {
            var sb = new StringBuilder();
            sb.Append($"{User} : ");
            if (!String.IsNullOrWhiteSpace(Comment))
                sb.Append(Comment);
            else
                sb.Append(Value.HasValue ? Value.Value.ToString() : "NA");
            return sb.ToString();
        }
    }
}