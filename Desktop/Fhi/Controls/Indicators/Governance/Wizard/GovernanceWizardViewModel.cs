using System;
using System.Collections.ObjectModel;
using System.Windows;
using Fhi.Controls.MVVM;
using Fhi.Controls.Wizard;

namespace Fhi.Controls.Indicators.Governance.Wizard
{
    public class GovernanceWizardViewModel : ViewModelBase
    {
        public WizardViewModel Wizard { get; }

        private GovernanceWizardViewModel(IProgress<WizardProgressEventArgs> progress, Action<bool> block)
        {
            var steps = new ObservableCollection<WizardViewModel.Step>();
            var step1 = new Step1ViewModel();
            var step3 = new Step3ViewModel(steps);
            var step2 = new Step2ViewModel(step1, step3);
            steps.Add(new WizardViewModel.Step("Introduction", step1));
            steps.Add(new WizardViewModel.Step("Import Data", step2));
            steps.Add(new WizardViewModel.Step("Assign Questions", step3));
            // step3 assumes that it should add in all of its steps just before the last step.
            var summary = new SummaryStepViewModel(step3);
            steps.Add(new WizardViewModel.Step("Summary", summary));
            
            Wizard = new WizardViewModel(steps, progress, block, Cancel);
        }
        
        private void Cancel()
        {
           
        }

        public static void StartWizard(IProgress<WizardProgressEventArgs> progress, Action<bool> block)
        {
            var vm = new GovernanceWizardViewModel(progress, block);
            var window = new GovernanceWizardWindow { DataContext = vm, Owner = Application.Current?.MainWindow };
            window.Show();
        }
    }
}