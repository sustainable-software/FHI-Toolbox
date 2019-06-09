using System;
using Fhi.Controls.MVVM;

namespace Fhi.Controls.Wizard
{
    public abstract class WizardStepViewModel : ViewModelBase
    {
        public virtual void Validate() { }
        
        public abstract Boolean ReadyForNext { get; }
    }
}