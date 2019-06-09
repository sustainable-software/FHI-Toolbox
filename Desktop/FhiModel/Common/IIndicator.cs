using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace FhiModel.Common
{
    public interface IIndicator : INotifyPropertyChanged
    {
        String Name { get; }
        Int32? Value { get; set; }
        Int32? UserOverride { get; set; }
        String OverrideComment { get; set; }
        Double? Weight { get; set; }
        Uncertainty Rank { get; set; }
        String Notes { get; set; }
        Boolean HasMetadata { get; }
        ObservableCollection<IIndicator> Children { get; }
    }
}