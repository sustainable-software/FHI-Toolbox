using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Fhi.Controls.Wizard;
using FhiModel.Common;
using FhiModel.Governance;

namespace Fhi.Controls.Indicators.Governance.Wizard
{
    public class Step3ViewModel : WizardStepViewModel
    {
        private ObservableCollection<WizardViewModel.Step> _steps;
        
        public Step3ViewModel(ObservableCollection<WizardViewModel.Step> steps)
        {
            _steps = steps;
            Governance = InitializeGovernance.Create();
            IndicatorChoices = new List<GovernanceIndicatorViewModel>();
            foreach (var majorIndicator in Governance.Children)
            {
                foreach (var subIndicator in majorIndicator.Children)
                    IndicatorChoices.Add(new GovernanceIndicatorViewModel
                    {
                        Indicator = subIndicator as GovernanceIndicator,
                        MajorIndicator = majorIndicator.Name
                    });
            }
        }
        
        public IIndicator Governance { get; set; } // this is here because it's a well defined location for summary

        public List<GovernanceIndicatorViewModel> IndicatorChoices { get; set; }

        public override Boolean ReadyForNext => true;
        
        public void UpdateSteps(List<List<Question>> survey)
        {
            var summary = _steps[_steps.Count - 1];
            _steps.RemoveAt(_steps.Count - 1);
            for (var i = 0; i < survey.Count; i++)
            {
                _steps.Add(new WizardViewModel.Step($"Assign {i}", new StepNViewModel(i + 1, survey.Count, survey[i], IndicatorChoices)));
            }
            _steps.Add(summary);
        }
    }
}