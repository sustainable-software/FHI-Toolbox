using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Fhi.Controls.Indicators.Governance.Wizard;
using Fhi.Controls.MVVM;
using Fhi.Controls.Wizard;
using FhiModel;
using FhiModel.Common;
using Microsoft.ApplicationInsights;

namespace Fhi.Controls.Utils
{
    public static class Globals
    {
        private static Model _model;
        private static ModelCheck _check;
        
        public static Model Model
        {
            get => _model;
            set
            {
                if (_model == value) return;
                _model = value;
                if (_model != null)
                {
                    _check = new ModelCheck();
                    _check.Check(_model);
                    _check.Changed += (sender, args) => ModelModified?.Invoke(null, EventArgs.Empty);
                }
                RaiseModelChanged();
            }
        }

        /// <summary>
        /// User modified the model
        /// </summary>
        public static event EventHandler ModelModified;
        
        /// <summary>
        /// User created a new model (new or open)
        /// </summary>
        public static event EventHandler ModelChanged;

        private static void RaiseModelChanged()
        {
            ModelChanged?.Invoke(null, EventArgs.Empty);
        }
        
        public static void GovernanceWizard(Action<Boolean> block)
        {
            GovernanceWizardViewModel.StartWizard(new Progress<WizardProgressEventArgs>(), block);
        }

        public static TelemetryClient Telemetry { get; set; }
    }
}
