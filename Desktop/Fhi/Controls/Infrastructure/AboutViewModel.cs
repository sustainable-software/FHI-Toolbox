using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Fhi.Controls.MVVM;

namespace Fhi.Controls.Infrastructure
{
    public class AboutViewModel : ViewModelBase
    {
        public String License => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "License.rtf");
    }
}
