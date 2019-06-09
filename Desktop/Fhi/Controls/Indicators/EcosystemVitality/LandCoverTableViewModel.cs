using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Fhi.Controls.MVVM;
using FhiModel.EcosystemVitality;
using Microsoft.Expression.Interactivity.Core;

namespace Fhi.Controls.Indicators.EcosystemVitality
{
    public class LandCoverTableViewModel : ViewModelBase
    {
        private double _bufferDistance;
        private bool _importStep;

        public LandCoverTableViewModel(ObservableCollection<LandCoverItem> table, bool buffer = false)
        {
            Table = table;
            ShowBuffer = buffer;
            NextCommand = new ActionCommand(() => ImportStep = true);
        }

        public ObservableCollection<LandCoverItem> Table { get; set; }

        public Boolean ShowBuffer { get; }

        public Double BufferDistance
        {
            get => _bufferDistance;
            set => Set(ref _bufferDistance, value);
        }

        public ICommand NextCommand { get; }

        public Boolean ImportStep
        {
            get => _importStep;
            set => Set(ref _importStep, value);
        }
    }
}
