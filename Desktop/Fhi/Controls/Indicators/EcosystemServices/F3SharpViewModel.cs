using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Fhi.Controls.MVVM;
using FhiModel.Common;
using FhiModel.Common.Timeseries;
using FhiModel.EcosystemServices;

namespace Fhi.Controls.Indicators.EcosystemServices
{
    public class F3SharpViewModel : SpatialUnitViewModel
    {   
        private TimeseriesDatum _timestep;
        private Int32 _selectedYear;
        private String _selectedObjective;
        private ObjectiveMetric _metric;

        public F3SharpViewModel(SpatialUnit spatialUnit) : base(spatialUnit)
        {
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

            if (Su.Objective.Function == null)
            {
                SelectedObjective = _objectives.Keys.First();
            }
            else
            {
                foreach (var objective in _objectives)
                    if (Su.Objective.Function.GetType() == objective.Value)
                        _selectedObjective = objective.Key;
            }

            Su.Objective.Metrics.CollectionChanged +=
                (sender, args) => RaisePropertyChanged(nameof(CanChangeObjective));
        }
        
        public ICommand AddYearCommand { get; }
        public ICommand AddTimestepCommand { get; }
        public ICommand RemoveTimestepCommand { get; }
        public ICommand AddMetricCommand { get; }
        public ICommand RemoveMetricCommand { get; }
        public ICommand PasteCommand { get; }
        public ICommand ApplyMetricsCommand { get; }
        
        public F3SharpSpatialUnit Su => SpatialUnit as F3SharpSpatialUnit;

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
                    Su.Objective.Function =
                        (ObjectiveFunction) Activator.CreateInstance(_objectives[_selectedObjective]);
                    Metric = Su.Objective.Function is ObjectiveFunctionRange
                        ? new ObjectiveMetricRange { Start = new DateTime(_selectedYear, 1, 1), End = new DateTime(_selectedYear, 12, 31)} as ObjectiveMetric
                        : new ObjectiveMetricSingleValue { Start = new DateTime(_selectedYear, 1, 1), End = new DateTime(_selectedYear, 12, 31)};
                }               
            }
        }

        public Boolean CanChangeObjective => Su.Objective.Metrics.Count == 0;
        
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
            Su.Data.Add(Timestep.Clone());
            Timestep.Time = new DateTime(Timestep.Time.Year, Timestep.Time.Month + 1, 1);
        }
        
        private void AddMetric()
        {
            Su.Objective.Metrics.Add(Metric.Clone());
        }

        private void AddYear()
        {
            Su.Data.Clear();
            foreach (var month in Enumerable.Range(1, 12))
                Su.Data.Add(new TimeseriesDatum
                {
                    Time = new DateTime(SelectedYear, month, 1), 
                    Value = Timestep.Value
                });
        }

        private void RemoveTimestep(Object o)
        {
            if (!(o is TimeseriesDatum ts)) return;
            Su.Data.Remove(ts);
        }

        private void RemoveMetric(Object o)
        {
            if (!(o is ObjectiveMetric m)) return;
            Su.Objective.Metrics.Remove(m);
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
            var rows = data.Split(new [] {"\r\n"}, StringSplitOptions.RemoveEmptyEntries);
            int month = 1;
            var year = SelectedYear;
            if (Su.Data.Count > 0)
            {
                year = Su.Data[Su.Data.Count - 1].Time.Year;
                month = Su.Data[Su.Data.Count - 1].Time.Month + 1;
                
                if (month > 12)
                {
                    month = 1;
                    year++;
                }
            }
            
            foreach (var row in rows)
            {
                if (!Double.TryParse(row, out Double value)) continue;
                
                Su.Data.Add(new TimeseriesDatum
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

        private void ApplyMetrics()
        {
            var answer =
                MessageBox.Show(
                    "This will apply the metrics for this spatial unit to all other Ecosystem Services spatial units that have Name and Units that match this spatial unit. Are you sure?",
                    "Warning", MessageBoxButton.YesNo);
            if (answer == MessageBoxResult.No) return;

            foreach (var esi in Model.EcosystemServices.FetchIndicators<EcosystemServicesIndicator>())
            {
                foreach (var su in esi.SpatialUnits)
                {
                    if (!(su is F3SharpSpatialUnit f3)) continue;
                    if (f3.Name != Su.Name || f3.Units != Su.Units) continue;

                    f3.Objective.Metrics.Clear();
                    f3.Objective.Function = Su.Objective.Function;
                    foreach (var metric in Su.Objective.Metrics)
                        f3.Objective.Metrics.Add(metric);
                }
            }
        }
    }
}