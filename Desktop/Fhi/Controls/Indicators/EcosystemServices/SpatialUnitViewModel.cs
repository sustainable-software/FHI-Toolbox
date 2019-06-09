using System.Windows;
using System.Windows.Input;
using Fhi.Controls.MVVM;
using Fhi.Controls.Utils;
using FhiModel.Common;
using FhiModel.EcosystemServices;

namespace Fhi.Controls.Indicators.EcosystemServices
{
    public abstract class SpatialUnitViewModel : ViewModelBase
    {
        protected SpatialUnitViewModel(SpatialUnit spatialUnit)
        {
            SpatialUnit = spatialUnit.Clone(); 
            LocateCommand = new RelayCommand(Locate);
        }
        
        public SpatialUnit SpatialUnit { get; }
        
        public ICommand LocateCommand { get; }

        private void Locate()
        {
            LocationPickerViewModel.Picker(SpatialUnit);
        }
    }
}