using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Fhi.Controls.Utils
{
    public class ContextHelp : Button
    {
        public static readonly DependencyProperty UrlProperty = DependencyProperty.Register(
            "Url", typeof(String), typeof(ContextHelp), new PropertyMetadata(default(String)));

        public String Url
        {
            get { return (String) GetValue(UrlProperty); }
            set { SetValue(UrlProperty, value); }
        }

        protected override void OnClick()
        {
            var url = $"https://www.freshwaterhealthindex.org/tool/{Url}";
            try
            {
                System.Diagnostics.Process.Start(url);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not run {url} : {ex.Message}.");
            }
        }
    }
}
