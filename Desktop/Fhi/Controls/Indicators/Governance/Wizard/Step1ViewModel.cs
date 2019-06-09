using System;
using System.Collections.Generic;
using Fhi.Controls.Wizard;

namespace Fhi.Controls.Indicators.Governance.Wizard
{
    public class Step1ViewModel : WizardStepViewModel
    {
        private String _userColumn = ColumnChoices[0];
        private String _questionColumn = ColumnChoices[10];
        private String _lastQuestion = "Please provide";

        public String UserColumn
        {
            get => _userColumn;
            set => Set(ref _userColumn, value);
        }

        public String QuestionColumn
        {
            get => _questionColumn;
            set => Set(ref _questionColumn, value);
        }

        public static List<String> ColumnChoices { get; }= new List<string>
        {
            "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z"
        };

        public String LastQuestion
        {
            get => _lastQuestion;
            set => Set(ref _lastQuestion, value);
        }

        public override Boolean ReadyForNext => true;
    }
}