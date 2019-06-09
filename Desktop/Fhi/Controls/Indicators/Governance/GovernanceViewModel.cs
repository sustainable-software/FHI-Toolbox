using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Fhi.Controls.Infrastructure;
using Fhi.Controls.MVVM;
using Fhi.Controls.Utils;
using FhiModel.Common;

namespace Fhi.Controls.Indicators.Governance
{
    public class GovernanceViewModel : NavigationViewModel
    {
        private SummaryViewModel _selectedViewModel;
        
        private readonly GovernanceSummaryViewModel _effectiveness;
        private readonly GovernanceSummaryViewModel _vision;
        private readonly GovernanceSummaryViewModel _engagement;
        private readonly GovernanceSummaryViewModel _environment;
        private bool _blockWindow;


        public GovernanceViewModel() { }

        public GovernanceViewModel(Action<NavigationViewModel> navigate, NavigationViewModel back) 
            : base(navigate, back)
        {
            ImportCommand = new RelayCommand(Import);

            EffectivenessCommand = new RelayCommand(() => SelectedViewModel = _effectiveness);
            VisionCommand = new RelayCommand(() => SelectedViewModel = _vision);
            EngagementCommand = new RelayCommand((() => SelectedViewModel = _engagement));
            EnvironmentCommand = new RelayCommand(() => SelectedViewModel = _environment);

            _effectiveness = new GovernanceSummaryViewModel("Effectiveness", navigate, this);
            _vision = new GovernanceSummaryViewModel("Vision & Adaptive Governance", navigate, this);
            _engagement = new GovernanceSummaryViewModel("Stakeholder Engagement", navigate, this);
            _environment = new GovernanceSummaryViewModel("Enabling Environment", navigate, this);

            SelectedViewModel = _effectiveness;

            Globals.Model.PropertyChanged += (sender, args) => RaisePropertyChanged("");
        }

        public IIndicator Pillar => Model?.Governance;
        public IIndicator Effectiveness => Pillar.FetchIndicator<Indicator>("Effectiveness");
        public IIndicator Engagement => Pillar.FetchIndicator<Indicator>("Stakeholder Engagement");
        public IIndicator Vision => Pillar.FetchIndicator<Indicator>("Vision & Adaptive Governance");
        public IIndicator Environment => Pillar.FetchIndicator<Indicator>("Enabling Environment");

        public ICommand ImportCommand { get; }

        public ICommand EffectivenessCommand { get; }
        public ICommand VisionCommand { get; }
        public ICommand EngagementCommand { get; }
        public ICommand EnvironmentCommand { get; }

        public Boolean EffectivenessSelected => SelectedViewModel == _effectiveness;
        public Boolean VisionSelected => SelectedViewModel == _vision;
        public Boolean EngagementSelected => SelectedViewModel == _engagement;
        public Boolean EnvironmentSelected => SelectedViewModel == _environment;

        public SummaryViewModel SelectedViewModel
        {
            get => _selectedViewModel;
            set
            {
                Set(ref _selectedViewModel, value);
                RaisePropertyChanged(nameof(EffectivenessSelected));
                RaisePropertyChanged(nameof(VisionSelected));
                RaisePropertyChanged(nameof(EngagementSelected));
                RaisePropertyChanged(nameof(EnvironmentSelected));
            }
        }

        public Boolean BlockWindow
        {
            get => _blockWindow;
            set => Set(ref _blockWindow, value);
        }

        private void Import()
        {
            Globals.GovernanceWizard((b) => BlockWindow = b);           
        }
    }
}
