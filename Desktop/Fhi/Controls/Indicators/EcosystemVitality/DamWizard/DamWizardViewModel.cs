using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Fhi.Controls.MVVM;
using Fhi.Controls.Utils;
using Fhi.Controls.Wizard;
using FhiModel.Common;
using FhiModel.EcosystemVitality.DendreticConnectivity;

namespace Fhi.Controls.Indicators.EcosystemVitality.DamWizard
{
    public class DamWizardViewModel : ViewModelBase
    {
        public WizardViewModel Wizard { get; }

        private DamWizardViewModel(IProgress<WizardProgressEventArgs> progress, Action<bool> block)
        {
            var steps = new ObservableCollection<WizardViewModel.Step>();
            var ci = Model.EcosystemVitality.FetchIndicator<ConnectivityIndicator>();
            var step1 = new Step1ViewModel(ci.Reaches.Clone(), Model.Attributes.Wkid, steps);
            var summary = new SummaryStepViewModel(step1, steps);
            steps.Add(new WizardViewModel.Step("Introduction", step1));
            steps.Add(new WizardViewModel.Step("Summary", summary));
            Wizard = new WizardViewModel(steps, progress, block, Cancel);
        }


        private void Cancel()
        {

        }

        public static void StartWizard(IProgress<WizardProgressEventArgs> progress, Action<bool> block)
        {
            var ci = Globals.Model.EcosystemVitality.FetchIndicator<ConnectivityIndicator>();
            if (!(ci.Reaches?.Count > 0))
            {
                MessageBox.Show("You must import a river network before you can import dams.");
                return;
            }
            var vm = new DamWizardViewModel(progress, block);
            var window = new DamWizardWindow { DataContext = vm, Owner = Application.Current?.MainWindow };
            window.Show();
        }
    }
}
