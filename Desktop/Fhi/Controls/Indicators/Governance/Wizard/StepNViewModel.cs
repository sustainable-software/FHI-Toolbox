using System;
using System.Collections.Generic;
using Fhi.Controls.Wizard;
using FhiModel.Governance;

namespace Fhi.Controls.Indicators.Governance.Wizard
{
    public class StepNViewModel : WizardStepViewModel
    {
        private GovernanceIndicatorViewModel _selectedIndicator;

        public StepNViewModel(int groupNumber, int totalGroups, List<Question> questions, List<GovernanceIndicatorViewModel> indicatorChoices)
        {
            IndicatorChoices = indicatorChoices;
            Questions = questions;
            GroupNumber = groupNumber;
            TotalGroups = totalGroups;
        }
        
        public List<Question> Questions { get; set; }
        
        public int GroupNumber { get; set; }
        
        public int TotalGroups { get; set; }

        public GovernanceIndicatorViewModel SelectedIndicator
        {
            get => _selectedIndicator;
            set
            {
                if (_selectedIndicator != null)
                    _selectedIndicator.Checked = false;
                Set(ref _selectedIndicator, value);
                if (_selectedIndicator != null)
                {
                    _selectedIndicator.Checked = true;
                    _selectedIndicator.Indicator.Questions = Questions;
                }
                RaisePropertyChanged(nameof(ReadyForNext));
            }
        }
        
        public List<GovernanceIndicatorViewModel> IndicatorChoices { get; }

        public override Boolean ReadyForNext => SelectedIndicator != null;
    }
}