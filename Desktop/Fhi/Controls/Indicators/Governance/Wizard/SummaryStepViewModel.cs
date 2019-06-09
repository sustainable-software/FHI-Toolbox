using System;
using System.Collections.Generic;
using Fhi.Controls.Utils;
using Fhi.Controls.Wizard;

namespace Fhi.Controls.Indicators.Governance.Wizard
{
    public class SummaryStepViewModel : WizardStepViewModel, IFinishStep
    {
        private readonly Step3ViewModel _step3;
        
        public SummaryStepViewModel(Step3ViewModel step3)
        {
            _step3 = step3;
            _step3.PropertyChanged += (sender, args) => RaisePropertyChanged(nameof(Indicators));
        }

        public List<GovernanceIndicatorViewModel> Indicators => _step3.IndicatorChoices;       

        public override Boolean ReadyForNext => true;
        
        public void Finish(IProgress<WizardProgressEventArgs> progress)
        {
            Globals.Model.Governance = _step3.Governance;
        }
    }
}