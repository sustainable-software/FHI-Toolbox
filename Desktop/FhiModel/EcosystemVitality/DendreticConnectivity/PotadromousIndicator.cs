using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace FhiModel.EcosystemVitality.DendreticConnectivity
{
    [DataContract(Namespace = "", IsReference = true)]
    [Obsolete]
    public class PotadromousIndicator : ConnectivityIndicator
    {
        protected override Int32? ComputeIndicator()
        {
            return null;
        }
    }
}