using System;
using Fhi.Controls.Infrastructure;

namespace Fhi.Controls.Indicators.Core
{
    public class CoreViewModel : NavigationViewModel
    {
        public CoreViewModel() { }

        public CoreViewModel(Action<NavigationViewModel> navigate, NavigationViewModel back) 
            : base(navigate, back) { }
        
        private WeightEditorViewModel _weightEditorViewModel;
        public WeightEditorViewModel WeightEditorViewModel => 
            _weightEditorViewModel ?? (_weightEditorViewModel = new WeightEditorViewModel());

        private OverrideViewModel _overrideViewModel;
        public OverrideViewModel OverrideViewModel =>
            _overrideViewModel ?? (_overrideViewModel = new OverrideViewModel());

        private ImportAssessmentViewModel _importAssessmentViewModel;
        public ImportAssessmentViewModel ImportAssessmentViewModel =>
            _importAssessmentViewModel ?? (_importAssessmentViewModel = new ImportAssessmentViewModel());
    }
}
