using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using FhiModel.EcosystemVitality;

namespace FhiModel.Common
{
    public interface ICoverage
    {
        ObservableCollection<LandCoverItem> Coverage { get; }
    }
}
