using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Input;
using Fhi.Controls.MVVM;
using Fhi.Controls.Wizard;
using FhiModel.Governance;
using Microsoft.Win32;

namespace Fhi.Controls.Indicators.Governance.Wizard
{
    public class Step2ViewModel : WizardStepViewModel
    {
        private readonly Step1ViewModel _step1;
        private readonly Step3ViewModel _step3;
        private String _filename;
        private Boolean _autoAssignQuestions;

        public Step2ViewModel(Step1ViewModel step1, Step3ViewModel step3)
        {
            ImportCommand = new RelayCommand(Import);
            _step1 = step1;
            _step3 = step3;
            AutoAssignQuestions = true;
        }
        
        public ICommand ImportCommand { get; }
        
        public List<List<Question>> Survey { get; private set; }

        public int? Users => Survey?[0][0]?.Answers?.Count;
        public int? Groups => Survey?.Count;
        public override Boolean ReadyForNext => Groups > 0;

        public String Filename
        {
            get => _filename;
            set => Set(ref _filename, value);
        }

        public Boolean AutoAssignQuestions
        {
            get => _autoAssignQuestions;
            set => Set(ref _autoAssignQuestions, value);
        }

        private void Import()
        {
            var dialog = new OpenFileDialog
            {
                Title = "Import Survey CSV File",
                DefaultExt = ".csv",
                CheckFileExists = true
            };
            try
            {
                if (dialog.ShowDialog() != true)
                    return;
                Survey = FhiModel.Governance.Import.Read(dialog.FileName, _step1.UserColumn, _step1.QuestionColumn, _step1.LastQuestion);
                if (AutoAssignQuestions)
                {
                    if (!AssignQuestionsToIndicators())
                    {
                        Survey = null;
                        return;
                    }
                }
                else
                {
                    _step3.UpdateSteps(Survey);
                }
                Filename = Path.GetFileName(dialog.FileName);
                RaisePropertyChanged(nameof(Users));
                RaisePropertyChanged(nameof(Groups));
                RaisePropertyChanged(nameof(ReadyForNext));
                ClearErrors();
                
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to import {dialog.FileName} : {ex.Message}");
            }
        }

        private Boolean AssignQuestionsToIndicators()
        {
            if (Groups != 12)
            {
                MessageBox.Show(
                    $"Auto assignment problem. Expecting {_step3.IndicatorChoices.Count} groups of questions. Try unchecking automatic assignment.");
                return false;
            }

            if (_step3.IndicatorChoices.Count != 12)
            {
                MessageBox.Show(
                    "Program configuration problem. Must have 12 sets of governance indicators for auto assignment to work. Try unchecking automatic assignment.");
                return false;
            }
            foreach (var vm in _step3.IndicatorChoices)
            {
                switch (vm.Indicator.Name.ToLowerInvariant())
                {
                    case "water resources management":
                        vm.Checked = true;
                        vm.Indicator.Questions = Survey[0];
                        break;
                    case "right to resource use":
                        vm.Checked = true;
                        vm.Indicator.Questions = Survey[1];
                        break;
                    case "incentives & regulations":
                        vm.Checked = true;
                        vm.Indicator.Questions = Survey[2];
                        break;
                    case "financial capacity":
                        vm.Checked = true;
                        vm.Indicator.Questions = Survey[3];
                        break;
                    case "technical capacity":
                        vm.Checked = true;
                        vm.Indicator.Questions = Survey[4];
                        break;
                    case "information access":
                        vm.Checked = true;
                        vm.Indicator.Questions = Survey[5];
                        break;
                    case "engagement in decision-making":
                        vm.Checked = true;
                        vm.Indicator.Questions = Survey[6];
                        break;
                    case "enforcement & compliance":
                        vm.Checked = true;
                        vm.Indicator.Questions = Survey[7];
                        break;
                    case "distribution of benefits":
                        vm.Checked = true;
                        vm.Indicator.Questions = Survey[8];
                        break;
                    case "water-related conflict":
                        vm.Checked = true;
                        vm.Indicator.Questions = Survey[9];
                        break;
                    case "monitoring mechanisms":
                        vm.Checked = true;
                        vm.Indicator.Questions = Survey[10];
                        break;
                    case "strategic planning":
                        vm.Checked = true;
                        vm.Indicator.Questions = Survey[11];
                        break;
                }
            }
            return true;
        }
    }
}