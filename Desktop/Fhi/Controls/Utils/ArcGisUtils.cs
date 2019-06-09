using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Esri.ArcGISRuntime.Geometry;

namespace Fhi.Controls.Utils
{
    public static class ArcGisUtils
    {
        public static String WkidToWktxt(int wkid)
        {
            var sr = new SpatialReference(wkid);
            return sr.WkText;
        }
    }
}
