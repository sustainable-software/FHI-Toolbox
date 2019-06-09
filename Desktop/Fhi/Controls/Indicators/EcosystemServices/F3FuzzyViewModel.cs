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
    public class F3FuzzyViewModel : SpatialUnitViewModel
    {
        private ObjectiveResult _timestep;
        private Int32 _selectedYear;
        
        public F3FuzzyViewModel(SpatialUnit spatialUnit) : base(spatialUnit)
        {
            AddTimestepCommand = new RelayCommand(AddTimestep);
            RemoveTimestepCommand = new RelayCommand(RemoveTimestep);
            AddYearCommand = new RelayCommand(AddYear);
            PasteCommand = new RelayCommand(Paste);
            
            Timestep = new ObjectiveResult { Value = 0 };
            SelectedYear = Model.Attributes.AssessmentYear;
        }
        
        public ICommand AddTimestepCommand { get; }
        public ICommand AddYearCommand { get; }
        public ICommand RemoveTimestepCommand { get; }
        public ICommand PasteCommand { get; }
        
        public F3FuzzySpatialUnit Su => SpatialUnit as F3FuzzySpatialUnit;

        public ObjectiveResult Timestep
        {
            get => _timestep;
            set => Set(ref _timestep, value);
        }

        public IEnumerable<Int32> Years => Enumerable.Range(2005, 20);

        public Int32 SelectedYear
        {
            get => _selectedYear;
            set
            {
                if(Set(ref _selectedYear, value))
                    Timestep.Time = new DateTime(_selectedYear, 1, 1);
            }
        }

        private void AddTimestep()
        {
            Su.Results.Add(Timestep.Clone());
            Timestep.Time = new DateTime(Timestep.Time.Year, Timestep.Time.Month + 1, 1);
        }

        private void AddYear()
        {
            Su.Results.Clear();
            foreach (var month in Enumerable.Range(1, 12))
                Su.Results.Add(new ObjectiveResult
                {
                    Time = new DateTime(SelectedYear, month, 1), 
                    Value = Timestep.Value
                });
        }

        private void RemoveTimestep(Object o)
        {
            if (!(o is ObjectiveResult ts)) return;
            Su.Results.Remove(ts);
        }

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
            if (Su.Results.Count > 0)
            {
                year = Su.Results[Su.Results.Count - 1].Time.Year;
                month = Su.Results[Su.Results.Count - 1].Time.Month + 1;

                if (month > 12)
                {
                    month = 1;
                    year++;
                }
            }

            foreach (var row in rows)
            {
                if (!Double.TryParse(row, out Double value)) continue;

                Su.Results.Add(new ObjectiveResult
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