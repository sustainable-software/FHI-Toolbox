using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Fhi.Controls.Infrastructure;
using FhiModel.Common;
using FhiModel.EcosystemVitality;

namespace Fhi.Controls.Indicators.EcosystemVitality
{
    public class GroundwaterStorageViewModel : NavigationViewModel
    {
        private double? _affectedArea;

        public GroundwaterStorageViewModel(Action<NavigationViewModel> navigate, NavigationViewModel back)
            : base(navigate, back)
        {
            AffectedArea = GroundwaterStorage.AffectedArea;
        }

        public GroundwaterStorageIndicator GroundwaterStorage =>
            Model?.EcosystemVitality?.FetchIndicator<GroundwaterStorageIndicator>();

        public Double? AffectedArea
        {
            get => _affectedArea;
            set
            {
                if (!Set(ref _affectedArea, value)) return;

                if (_affectedArea > GroundwaterStorage.BasinArea)
                {
                    MessageBox.Show("Affected area must be less than the basin area.");
                    AffectedArea = GroundwaterStorage.BasinArea;
                    return;
                }
                GroundwaterStorage.AffectedArea = AffectedArea;
            }
        }
    }
}
