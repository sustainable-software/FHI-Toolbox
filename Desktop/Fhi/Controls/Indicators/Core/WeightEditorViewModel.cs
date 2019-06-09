using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Fhi.Controls.MVVM;
using Fhi.Controls.Utils;
using FhiModel.Common;
using Microsoft.Win32;
using CsvHelper;

namespace Fhi.Controls.Indicators.Core
{
    public class WeightEditorViewModel : ViewModelBase
    {
        private WeightViewModel _servicesPillar;
        private WeightViewModel _provisioningMajor;
        private WeightViewModel _culturalMajor;
        private WeightViewModel _regulationMajor;
        private WeightViewModel _governancePillar;
        private WeightViewModel _effectivenessMajor;
        private WeightViewModel _engagementMajor;
        private WeightViewModel _visionMajor;
        private WeightViewModel _environmentMajor;

        public WeightEditorViewModel()
        {
            CommitCommand = new RelayCommand(Commit);
            ResetCommand = new RelayCommand(Update);
            ImportCommand = new RelayCommand(Import);

            Globals.ModelChanged += (sender, args) => Update();
            Update();
        }

        public ICommand CommitCommand { get; }
        public ICommand ResetCommand { get; }
        public ICommand ImportCommand { get; }

        private void Import()
        {

            var dialog = new OpenFileDialog
            {
                Title = "Import Wights CSV",
                DefaultExt = ".csv",
                CheckFileExists = true
            };
            if (dialog.ShowDialog() != true)
                return;

            var filename = dialog.FileName;

            if (String.IsNullOrWhiteSpace(filename)) return;

            var weights = new Dictionary<string, List<int>>();
            try
            {
                using (var stream = new FileStream(filename, FileMode.Open))
                {
                    using (var reader = new CsvReader(new StreamReader(stream, Encoding.UTF8)))
                    {
                        reader.Read();
                        reader.ReadHeader();
                        foreach (var id in reader.Context.HeaderRecord)
                            weights.Add(id, new List<int>());
                        while (reader.Read())
                        {
                            foreach (var id in reader.Context.HeaderRecord)
                                weights[id].Add(reader.GetField<int>(id));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Import failed: {ex.Message}");
            }

            foreach (var id in weights.Keys)
            {
                var indicator = GetIndicatorById(id);
                if (indicator == null) continue;
                indicator.Weight = weights[id].Average()/100.0;
            }
            Update();
        }

        private void Commit()
        {
            foreach (var vm in new List<WeightViewModel> { ServicesPillar, ProvisioningMajor, CulturalMajor, RegulationMajor, GovernancePillar, EffectivenessMajor, EngagementMajor, VisionMajor, EnvironmentMajor})
                vm.Commit();
        }

        private void Update()
        {
            ServicesPillar = new WeightViewModel(Model.EcosystemServices.Children);
            ProvisioningMajor = new WeightViewModel(Model.EcosystemServices.Children.First(x => x.Name == "Provisioning").Children);
            CulturalMajor = new WeightViewModel(Model.EcosystemServices.Children.First(x => x.Name == "Cultural").Children);
            RegulationMajor = new WeightViewModel(Model.EcosystemServices.Children.First(x => x.Name == "Regulation").Children);

            GovernancePillar = new WeightViewModel(Model.Governance.Children);
            EffectivenessMajor = new WeightViewModel(Model.Governance.Children.First(x => x.Name == "Effectiveness").Children);
            EngagementMajor = new WeightViewModel(Model.Governance.Children.First(x => x.Name == "Stakeholder Engagement").Children);
            VisionMajor = new WeightViewModel(Model.Governance.Children.First(x => x.Name == "Vision & Adaptive Governance").Children);
            EnvironmentMajor = new WeightViewModel(Model.Governance.Children.First(x => x.Name == "Enabling Environment").Children);

            RaisePropertyChanged(nameof(Provisioning));
            RaisePropertyChanged(nameof(Regulation));
            RaisePropertyChanged(nameof(Cultural));

            RaisePropertyChanged(nameof(Effectiveness));
            RaisePropertyChanged(nameof(Engagement));
            RaisePropertyChanged(nameof(Vision));
            RaisePropertyChanged(nameof(Environment));
        }

        public WeightViewModel ServicesPillar
        {
            get => _servicesPillar;
            private set => Set(ref _servicesPillar, value);
        }

        public WeightViewModel ProvisioningMajor
        {
            get => _provisioningMajor;
            private set => Set(ref _provisioningMajor, value);
        }

        public WeightViewModel CulturalMajor
        {
            get => _culturalMajor;
            private set => Set(ref _culturalMajor, value);
        }

        public WeightViewModel RegulationMajor
        {
            get => _regulationMajor;
            private set => Set(ref _regulationMajor, value);
        }

        public WeightViewModel GovernancePillar
        {
            get => _governancePillar;
            private set => Set(ref _governancePillar, value);
        }

        public WeightViewModel EffectivenessMajor
        {
            get => _effectivenessMajor;
            private set => Set(ref _effectivenessMajor, value);
        }

        public WeightViewModel EngagementMajor
        {
            get => _engagementMajor;
            private set => Set(ref _engagementMajor, value);
        }

        public WeightViewModel VisionMajor
        {
            get => _visionMajor;
            private set => Set(ref _visionMajor, value);
        }

        public WeightViewModel EnvironmentMajor
        {
            get => _environmentMajor;
            private set => Set(ref _environmentMajor, value);
        }


        #region Ecosystem Services
        public WeightValueViewModel Provisioning => ServicesPillar.Weights.Find(x => x.Name == "Provisioning");
        
        public WeightValueViewModel Cultural => ServicesPillar.Weights.Find(x => x.Name == "Cultural");
        
        public WeightValueViewModel Regulation => ServicesPillar.Weights.Find(x => x.Name == "Regulation");
        
        #endregion Ecosystem Services

        #region Governance
        public WeightValueViewModel Effectiveness => GovernancePillar.Weights.Find(x => x.Name == "Effectiveness");
        
        public WeightValueViewModel Engagement => GovernancePillar.Weights.Find(x => x.Name == "Stakeholder Engagement");
        
        public WeightValueViewModel Vision => GovernancePillar.Weights.Find(x => x.Name == "Vision & Adaptive Governance");
        
        public WeightValueViewModel Environment => GovernancePillar.Weights.Find(x => x.Name == "Enabling Environment");
        
        #endregion Governance

        private readonly Dictionary<String, String> _idMap = new Dictionary<string, string>
        {
            { "ES1", "Provisioning"},
            { "ES2", "Regulation"},
            { "ES3", "Cultural"},
            { "ES11", "Water Supply Reliability"},
            { "ES12", "Biomass"},
            { "ES21", "Sediment Regulation"},
            { "ES22", "Water Quality Regulation"},
            { "ES23", "Flood Regulation"},
            { "ES24", "Disease Regulation"},
            { "ES31", "Conservation Areas"},
            { "ES32", "Recreation"},
            { "GS1", "Enabling Environment" },
            { "GS2", "Stakeholder Engagement" },
            { "GS3", "Vision & Adaptive Governance" },
            { "GS4", "Effectiveness" },
            { "GS11", "Water resources management" },
            { "GS12", "Right to resource use" },
            { "GS13", "Incentives & regulations" },
            { "GS14", "Financial capacity" },
            { "GS15", "Technical capacity" },
            { "GS21", "Information access" },
            { "GS22", "Engagement in decision-making" },
            { "GS31", "Strategic planning" },
            { "GS32", "Monitoring mechanisms" },
            { "GS41", "Enforcement & compliance" },
            { "GS42", "Distribution of benefits" },
            { "GS43", "Water-related conflict" }
        };

        private IIndicator GetIndicatorById(string id)
        {
            var pillar = id[0] == 'E' ? Model.EcosystemServices : Model.Governance;
            return _idMap.ContainsKey(id) ? pillar.FetchIndicator<Indicator>(_idMap[id]) : null;
        }
    }
}
