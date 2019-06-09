using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Fhi.Controls.Infrastructure;
using Fhi.Controls.MVVM;
using Fhi.Controls.Utils;
using FhiModel.Common;
using FhiModel.EcosystemVitality.WaterQuality;
using FhiModel.Services;

namespace Fhi.Controls.Indicators.EcosystemVitality
{
    public class GaugeViewModel : NavigationViewModel
    {
        private readonly Gauge _oldGauge;
        private WaterQualityParameter _selectedTemplate;

        public GaugeViewModel(Gauge gauge, Action<NavigationViewModel> navigate, NavigationViewModel back)
            : base(navigate, back)
        {
            _oldGauge = gauge;
            Gauge = gauge.Clone();
            EditorCommand = new RelayCommand(Editor);
            AddTemplateCommand = new RelayCommand(AddTemplate);
            RemoveCommand = new RelayCommand(Remove);
            LocateCommand = new RelayCommand(Locate);
            OkCommand = new RelayCommand(Ok);
            CancelCommand = new RelayCommand(Cancel);
            RemoveGaugeCommand = new RelayCommand(RemoveGauge);

            SelectedTemplate = ParameterTemplates.First();
        }
        
        public WaterQualityIndicator Indicator => Model.EcosystemVitality.FetchIndicator<WaterQualityIndicator>();
        
        public Gauge Gauge { get; }

        public IEnumerable<WaterQualityParameter> ParameterTemplates => WaterQualityParameterService.Parameters;

        public WaterQualityParameter SelectedTemplate
        {
            get => _selectedTemplate;
            set => Set(ref _selectedTemplate, value);
        }

        public ICommand AddTemplateCommand { get; }
        public ICommand EditorCommand { get; }
        public ICommand RemoveCommand { get; }
        public ICommand LocateCommand { get; }
        public ICommand OkCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand RemoveGaugeCommand { get; }
        
        private void Remove(Object o)
        {
            if (!(o is WaterQualityParameter parameter)) return;
            var answer = MessageBox.Show($"Are you sure you want to remove the {parameter.Name} parameter from the assessment?", 
                "Remove Water Quality Parameter?", 
                MessageBoxButton.YesNo);
            if (answer != MessageBoxResult.Yes) return;
            Gauge.Parameters.Remove(parameter);
        }

        private void Editor(Object o)
        {
            var create = false;
            if (!(o is WaterQualityParameter parameter))
            {
                parameter = new WaterQualityParameter();
                create = true;
            }
            var vm = new ParameterEditorViewModel(parameter);
            var dialog = new ParameterEditorWindow
            {
                DataContext = vm, 
                Owner = Application.Current?.MainWindow, 
                Title = create ? "Create Water Quality Parameter" : "Modify Water Quality Parameter"
            };
            if (dialog.ShowDialog() != true) return;
            if (!create)
                Gauge.Parameters.Remove(parameter);
            Gauge.Parameters.Add(vm.Parameter);
        }

        private void AddTemplate()
        {
            var vm = new ParameterEditorViewModel(SelectedTemplate);
            var dialog = new ParameterEditorWindow
            {
                DataContext = vm, 
                Owner = Application.Current?.MainWindow, 
                Title = $"Water Quality Parameter Template {SelectedTemplate.Name}"
            };
            if (dialog.ShowDialog() != true) return;
            Gauge.Parameters.Add(vm.Parameter);
        }
        
        private void Ok()
        {
            
            if (Indicator.Gauges.Contains(_oldGauge))
                Indicator.Gauges.Remove(_oldGauge);
            Indicator.Gauges.Add(Gauge);
            NavigateBack();
        }

        private void RemoveGauge()
        {
            var res = MessageBox.Show(
                $"Are you sure you want to remove gauge {_oldGauge.Name} and all its water quality parameters from this assessment?",
                "Remove Gauge?", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (res != MessageBoxResult.Yes) return;
            if (Indicator.Gauges.Contains(_oldGauge))
                Indicator.Gauges.Remove(_oldGauge);
            NavigateBack();
        }

        private void Cancel()
        {
            NavigateBack();
        }
        
        private void Locate()
        {
            LocationPickerViewModel.Picker(Gauge);
        }
    }
}