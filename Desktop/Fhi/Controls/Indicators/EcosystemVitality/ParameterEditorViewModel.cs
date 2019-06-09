using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Fhi.Controls.MVVM;
using FhiModel.Common;
using FhiModel.Common.Timeseries;
using FhiModel.EcosystemVitality.WaterQuality;

namespace Fhi.Controls.Indicators.EcosystemVitality
{
    public class ParameterEditorViewModel : ViewModelBase
    {
        private TimeseriesDatum _timestep;
        private Int32 _selectedYear;
        private String _selectedObjective;
        private ObjectiveMetric _metric;
        
        public ParameterEditorViewModel(WaterQualityParameter parameter)
        {
            Parameter = parameter.Clone();
            
            AddYearCommand = new RelayCommand(AddYear);
            AddTimestepCommand = new RelayCommand(AddTimestep);
            RemoveTimestepCommand = new RelayCommand(RemoveTimestep);
            AddMetricCommand = new RelayCommand(AddMetric);
            RemoveMetricCommand = new RelayCommand(RemoveMetric);
            PasteCommand = new RelayCommand(Paste);
            ApplyMetricsCommand = new RelayCommand(ApplyMetrics);
            
            Timestep = new TimeseriesDatum();
            Metric = new ObjectiveMetricSingleValue();
            SelectedYear = Model.Attributes.AssessmentYear;
            
            if (Parameter.Objective.Function == null)
            {
                SelectedObjective = _objectives.Keys.First();
            }
            else
            {
                foreach (var objective in _objectives)
                    if (Parameter.Objective.Function.GetType() == objective.Value)
                        _selectedObjective = objective.Key;
            }

            Parameter.Objective.Metrics.CollectionChanged +=
                (sender, args) => RaisePropertyChanged(nameof(CanChangeObjective));
        }
        
        public WaterQualityParameter Parameter { get; }
        
        public ICommand AddYearCommand { get; }
        public ICommand AddTimestepCommand { get; }
        public ICommand RemoveTimestepCommand { get; }
        public ICommand AddMetricCommand { get; }
        public ICommand RemoveMetricCommand { get; }
        public ICommand PasteCommand { get; }
        public ICommand ApplyMetricsCommand { get; }
        
        public TimeseriesDatum Timestep
        {
            get => _timestep;
            set => Set(ref _timestep, value);
        }
        
        public ObjectiveMetric Metric
        {
            get => _metric;
            set
            {
                if(Set(ref _metric, value))
                    RaisePropertyChanged(nameof(RangeMetric));
            }
        }
        
        public Boolean RangeMetric => Metric is ObjectiveMetricRange;
        
        public IEnumerable<Int32> Years => Enumerable.Range(2005, 20);
        
        public IEnumerable<String> Objectives => _objectives.Keys;

        public String SelectedObjective
        {
            get => _selectedObjective;
            set
            {
                if (Set(ref _selectedObjective, value))
                {
                    Parameter.Objective.Function =
                        (ObjectiveFunction) Activator.CreateInstance(_objectives[_selectedObjective]);
                    Metric = Parameter.Objective.Function is ObjectiveFunctionRange
                        ? new ObjectiveMetricRange { Start = new DateTime(_selectedYear, 1, 1), End = new DateTime(_selectedYear, 12, 31)} as ObjectiveMetric
                        : new ObjectiveMetricSingleValue { Start = new DateTime(_selectedYear, 1, 1), End = new DateTime(_selectedYear, 12, 31)};
                }               
            }
        }

        public Boolean CanChangeObjective => Parameter.Objective.Metrics.Count == 0;
        
        public Int32 SelectedYear
        {
            get => _selectedYear;
            set
            {
                if (!Set(ref _selectedYear, value)) return;
                Timestep.Time = new DateTime(_selectedYear, 1, 1);
                Metric.Start = new DateTime(_selectedYear, 1, 1);
                Metric.End = new DateTime(_selectedYear, 12, 31);
            }
        }
        
        private void AddTimestep()
        {
            Parameter.Data.Add(Timestep.Clone());
            Timestep.Time = Timestep.Time.AddMonths(1);
        }
        
        private void AddMetric()
        {
            Parameter.Objective.Metrics.Add(Metric.Clone());
        }
        
        private void AddYear()
        {
            
            Parameter.Data.Clear();
            foreach (var month in Enumerable.Range(1, 12))
                Parameter.Data.Add(new TimeseriesDatum
                {
                    Time = new DateTime(SelectedYear, month, 1), 
                    Value = Timestep.Value
                });
        }

        private void ApplyMetrics()
        {
            var answer =
                MessageBox.Show(
                    "This will apply the metrics for this parameter to all other Water Quality gauges that have parameters that match the Name and Units of this parameter. Are you sure?",
                    "Warning", MessageBoxButton.YesNo);
            if (answer == MessageBoxResult.No) return;

            var wqi = Model.EcosystemVitality.FetchIndicator<WaterQualityIndicator>();
            foreach (var gauge in wqi.Gauges)
            {
                foreach (var parameter in gauge.Parameters)
                {
                    if (Parameter.Name != parameter.Name || Parameter.Units != parameter.Units) continue;

                    parameter.Objective.Metrics.Clear();
                    parameter.Objective.Function = Parameter.Objective.Function;
                    foreach (var metric in Parameter.Objective.Metrics)
                        parameter.Objective.Metrics.Add(metric);
                }
            }
        }

        private void RemoveTimestep(Object o)
        {
            if (!(o is TimeseriesDatum ts)) return;
            Parameter.Data.Remove(ts);
        }
        
        private void RemoveMetric(Object o)
        {
            if (!(o is ObjectiveMetric m)) return;
            Parameter.Objective.Metrics.Remove(m);
        }
        
        private static readonly Dictionary<String, Type> _objectives = new Dictionary<String, Type>
        {
            { "<", typeof(ObjectiveFunctionLessThan)},
            { ">", typeof(ObjectiveFunctionGreaterThan)},
            { "Range", typeof(ObjectiveFunctionRange)}
        };

        private void Paste()
        {

            var dataObj = Clipboard.GetDataObject();
            if (dataObj == null) return;
            var rawData = dataObj.GetData(DataFormats.CommaSeparatedValue) ?? dataObj.GetData(DataFormats.Text);
            if (rawData == null) return;
            var data = rawData as String;
            if (data == null && rawData is MemoryStream ms)
            {
                using (var sr = new StreamReader(ms))
                    data = sr.ReadToEnd();
            }

            if (data == null) return;
            var rows = data.Split(new [] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            int month = 1;
            var year = SelectedYear;
            if (Parameter.Data.Count > 0)
            {
                year = Parameter.Data[Parameter.Data.Count - 1].Time.Year;
                month = Parameter.Data[Parameter.Data.Count - 1].Time.Month + 1;

                if (month > 12)
                {
                    month = 1;
                    year++;
                }
            }

            foreach (var row in rows)
            {
                if (!Double.TryParse(row, out Double value)) continue;

                Parameter.Data.Add(new TimeseriesDatum
                {
                    Time = new DateTime(year, month++, 1),
                    Value = value
                });

                if (month > 12)
                {
                    month = 1;
                    year++;
                }
            }
        }
    }
}