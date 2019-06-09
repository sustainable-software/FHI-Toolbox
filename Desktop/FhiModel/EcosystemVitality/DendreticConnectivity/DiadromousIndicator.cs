using System;
using System.Runtime.Serialization;

namespace FhiModel.EcosystemVitality.DendreticConnectivity
{
    [DataContract(Namespace = "", IsReference = true)]
    [Obsolete]
    public class DiadromousIndicator : ConnectivityIndicator
    {
        protected override Int32? ComputeIndicator()
        {
            return null;
        }
    }
}