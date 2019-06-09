using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using Fhi.Controls.MVVM;
using FhiModel.Common;

namespace Fhi.Controls.Indicators.Core
{
    public class WeightViewModel : ViewModelBase
    {
        public WeightViewModel(IList<IIndicator> indicators)
        {
            if (indicators == null)
            {
                Weights = new List<WeightValueViewModel>();
                return;
            }
            
            Weights = indicators.Select(x =>
            {
                var model = new WeightValueViewModel
                    {
                        Value = (Int32?) (x.Weight * 100),
                        Indicator = x
                    };
                model.PropertyChanged += WeightChanged;
                return model;
            }).ToList();
        }

        private WeightValueViewModel _changing;
        private void WeightChanged(Object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != "Value" || _changing != null) return;
            // here's where we adjust the other values if one changes.
            var total = Weights.Sum(x => x.Value);
            var makeup = 100 - total;
            if (makeup == 0) return;
            var addToEach = makeup / (Weights.Count - 1);
            if (addToEach == 0) return;
            
            _changing = (WeightValueViewModel)sender;
            
            foreach (var wvm in Weights)
            {
                if (wvm == _changing) continue;
                wvm.Value += addToEach;
                if (wvm.Value < 0)
                    wvm.Value = 0;
                if (wvm.Value > 100)
                    wvm.Value = 99;
            }
            
            // handle rounding error, charge the one the user is modifying
            total = Weights.Sum(x => x.Value);
            if (total != 100)
                _changing.Value += 100 - total;
            
            _changing = null;
        }

        public List<WeightValueViewModel> Weights { get; }

        public void Commit()
        {
            // Debug.Assert(Weights.Sum(x => x.Value) == 100); todo: there's still an off by on bug here.
            foreach (var wvm in Weights)
                wvm.Commit();
        }
    }

    public class WeightValueViewModel : ViewModelBase
    {
        private Int32? _value;

        public Int32? Value
        {
            get => _value;
            set => Set(ref _value, value);
        }

        public String Name => Indicator.Name;
        
        public IIndicator Indicator { get; set; }

        public void Commit()
        {
            Indicator.Weight = Value / 100.0;
        }
    }
}