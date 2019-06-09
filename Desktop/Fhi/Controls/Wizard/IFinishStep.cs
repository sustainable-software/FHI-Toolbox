using System;

namespace Fhi.Controls.Wizard
{
    public interface IFinishStep
    {
        void Finish(IProgress<WizardProgressEventArgs> progress);
    }
}