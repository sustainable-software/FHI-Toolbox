using System;
using Fhi.Controls.MVVM;
using FhiModel.Governance;

namespace Fhi.Controls.Indicators.Governance.Wizard
{
    public class GovernanceIndicatorViewModel : ViewModelBase
    {
        private Boolean _checked;
        
        public String MajorIndicator { get; set; }
        
        public GovernanceIndicator Indicator { get; set; }

        public Boolean Checked
        {
            get => _checked;
            set => Set(ref _checked, value);
        }
    }
}