using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Linq;
using FhiModel.Common;

namespace FhiModel.Governance
{
    [DataContract(Namespace = "", IsReference = true)]
    public class GovernanceIndicator : Indicator
    {
        [DataMember]
        public List<Question> Questions { get; set; }

        protected override Int32? ComputeIndicator()
        {
            var empty = true;
            var indicator = 0.0;
            
            if (Questions == null) return null;

            var qN = Questions.Count - 1;
            
            foreach (var question in Questions)
            {
                
                //if (question.Weight == null)
                    question.Weight = 1.0 / qN;
                
                if (question.Answers == null) continue;
                
                var aN = question.Answers.Count(x => x.Value.HasValue);
                if (aN == 0) continue;
                
                var average = 0.0;
                foreach (var answer in question.Answers)
                {
                    if (answer.Value == null) continue;
                    if (answer.Weight == null)
                        answer.Weight = 1.0 / aN;
                    average += answer.Value.Value * answer.Weight.Value;
                }
                question.Score = (Int32) Math.Round((average - 1) * 25, 0);
                indicator += question.Weight.Value * question.Score.Value;
                empty = false;               
            }
            
            if (empty) return null;
            return (Int32) Math.Round(indicator, 0);
        }
    }
}
