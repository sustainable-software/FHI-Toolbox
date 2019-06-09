using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fhi.Controls.Network;
using Fhi.Controls.Utils;
using Fhi.Controls.Wizard;
using FhiModel.Common;
using FhiModel.EcosystemVitality.DendreticConnectivity;

namespace Fhi.Controls.Indicators.EcosystemVitality.DamWizard
{
    public class SummaryStepViewModel : WizardStepViewModel, IFinishStep
    {
        private readonly IList<WizardViewModel.Step> _steps;
        private Step1ViewModel _step1;
        private BasinMapViewModel _basinMapViewModel;

        public SummaryStepViewModel(Step1ViewModel step1, IList<WizardViewModel.Step> steps)
        {
            _steps = steps;
            _step1 = step1;
        }

        public override Boolean ReadyForNext => true;

        public BasinMapViewModel BasinMapViewModel => _basinMapViewModel ?? (_basinMapViewModel = new BasinMapViewModel(_step1.Reaches, _step1.Wkid, true));

        public Double? AverageLength => _step1.Reaches?.Average(x => x.Length);
        public Double? MaximumLength => _step1.Reaches?.Max(x => x.Length);
        public Double? MinimumLength => _step1.Reaches?.Min(x => x.Length);
        public Int32 ReachCount => _step1.Reaches.Count;
        public Int32 DamCount => _step1.DamCount;

        public void Finish(IProgress<WizardProgressEventArgs> progress)
        {
            Globals.Model.Attributes.Wkid = _step1.Wkid;
            var indicator = Globals.Model.EcosystemVitality.FetchIndicator<ConnectivityIndicator>();
            foreach (var reach in _step1.Reaches)
                reach.HasDam = reach.Nodes.Any(x => x.Dam != null);
            indicator.Reaches = (List<Reach>)_step1.Reaches;
            _step1.Reaches = null;
        }
    }
}
