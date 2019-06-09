using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fhi.Controls.Infrastructure;
using FhiModel.Governance;

namespace Fhi.Controls.Indicators.Governance
{
    class QuestionViewModel : NavigationViewModel
    {
        public QuestionViewModel(GovernanceIndicator indicator, Action<NavigationViewModel> navigate, NavigationViewModel back)
            : base(navigate, back)
        {
            Indicator = indicator;
        }

        public GovernanceIndicator Indicator { get; }
        public IEnumerable<Question> Questions => Indicator.Questions;
    }
}
