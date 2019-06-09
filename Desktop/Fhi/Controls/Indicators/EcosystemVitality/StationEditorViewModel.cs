using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Fhi.Controls.MVVM;
using Fhi.Controls.Utils;
using FhiModel.Common;
using FhiModel.Common.Timeseries;
using FhiModel.EcosystemVitality.FlowDeviation;

namespace Fhi.Controls.Indicators.EcosystemVitality
{
    public class StationEditorViewModel : ViewModelBase
    {
        private TimeseriesDatum _timestep;
        private Int32 _selectedYear;
        private Boolean _addRegulated = true;
        private Boolean _addUnregulated;

        public StationEditorViewModel(Station station)
        {
            Station = station.Clone();
            
            LocateCommand = new RelayCommand(Locate);
            AddYearCommand = new RelayCommand(AddYear);
            AddTimestepCommand = new RelayCommand(AddTimestep);
            RemoveRegulatedTimestepCommand = new RelayCommand(RemoveRegulatedTimestep);
            RemoveUnregulatedTimestepCommand = new RelayCommand(RemoveUnregulatedTimestep);
            PasteCommand = new RelayCommand(Paste);
            

            Timestep = new TimeseriesDatum();
            SelectedYear = Model.Attributes.AssessmentYear;
        }
        
        public Station Station { get; }
        
        public ICommand AddYearCommand { get; }
        public ICommand AddTimestepCommand { get; }
        public ICommand RemoveRegulatedTimestepCommand { get; }
        public ICommand RemoveUnregulatedTimestepCommand { get; }
        public ICommand LocateCommand { get; }
        public ICommand PasteCommand { get; }
        
        public TimeseriesDatum Timestep
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
                if (Set(ref _selectedYear, value))
                    Timestep.Time = new DateTime(_selectedYear, 1, 1);
            }
        }

        public Boolean AddRegulated
        {
            get => _addRegulated;
            set => Set(ref _addRegulated, value);
        }

        public Boolean AddUnregulated
        {
            get => _addUnregulated;
            set => Set(ref _addUnregulated, value);
        }

        private void AddTimestep()
        {
            if (AddRegulated)
                Station.Regulated.Add(Timestep.Clone());
            if (AddUnregulated)
                Station.Unregulated.Add(Timestep.Clone());
            
            Timestep.Time = Timestep.Time.AddMonths(1);
        }
        
        private void AddYear()
        {
            var collection = AddRegulated ? Station.Regulated : Station.Unregulated;
            collection.Clear();
            foreach (var month in Enumerable.Range(1, 12))
                collection.Add(new TimeseriesDatum
                {
                    Time = new DateTime(SelectedYear, month, 1), 
                    Value = Timestep.Value
                });
        }

        private void RemoveRegulatedTimestep(Object o)
        {
            if (!(o is TimeseriesDatum ts)) return;
            Station.Regulated.Remove(ts);
        }
        
        private void RemoveUnregulatedTimestep(Object o)
        {
            if (!(o is TimeseriesDatum ts)) return;
            Station.Unregulated.Remove(ts);
        }
        
        private void Locate()
        {
            LocationPickerViewModel.Picker(Station);
        }

        private void Paste(Object parameter)
        {
            var choice = parameter as String;
            var tsd = choice?.ToLowerInvariant() == "regulated" ? Station.Regulated : Station.Unregulated;

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
            var rows = data.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            int month = 1;
            var year = SelectedYear;
            if (tsd.Count > 0)
            {
                year = tsd[tsd.Count - 1].Time.Year;
                month = tsd[tsd.Count - 1].Time.Month + 1;

                if (month > 12)
                {
                    month = 1;
                    year++;
                }
            }

            foreach (var row in rows)
            {
                if (!Double.TryParse(row, out Double value)) continue;

                tsd.Add(new TimeseriesDatum
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
