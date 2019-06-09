using System;
using Fhi.Controls.Infrastructure;
using Fhi.Controls.MVVM;
using FhiModel.Common;
using FhiModel.EcosystemServices;

namespace Fhi.Controls.Indicators.EcosystemServices
{
    public class F1ViewModel : SpatialUnitViewModel
    {
        public F1ViewModel(SpatialUnit spatialUnit) : base(spatialUnit)
        {
            
        }
        
        public F1SpatialUnit Su => SpatialUnit as F1SpatialUnit;
    }
}