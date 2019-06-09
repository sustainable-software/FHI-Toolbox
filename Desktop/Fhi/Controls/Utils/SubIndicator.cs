using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using FhiModel.Common;

namespace Fhi.Controls.Utils
{
    public class SubIndicator : Button
    {
        public static readonly DependencyProperty IndicatorNameProperty = DependencyProperty.Register(
            "IndicatorName", typeof(String), typeof(SubIndicator), new PropertyMetadata(default(String)));

        public String IndicatorName
        {
            get { return (String)GetValue(IndicatorNameProperty); }
            set { SetValue(IndicatorNameProperty, value); }
        }

        public static readonly DependencyProperty IndicatorProperty = DependencyProperty.Register(
            "Indicator", typeof(IIndicator), typeof(SubIndicator), new PropertyMetadata(default(IIndicator)));

        public IIndicator Indicator
        {
            get { return (IIndicator)GetValue(IndicatorProperty); }
            set { SetValue(IndicatorProperty, value); }
        }

        public static readonly DependencyProperty SelectedProperty = DependencyProperty.Register(
            "Selected", typeof(Boolean), typeof(SubIndicator), new PropertyMetadata(default(Boolean)));

        public Boolean Selected
        {
            get { return (Boolean)GetValue(SelectedProperty); }
            set { SetValue(SelectedProperty, value); }
        }

        public static readonly DependencyProperty ContextHelpProperty = DependencyProperty.Register(
            "ContextHelp", typeof(String), typeof(SubIndicator), new PropertyMetadata(default(String)));

        public String ContextHelp
        {
            get { return (String)GetValue(ContextHelpProperty); }
            set { SetValue(ContextHelpProperty, value); }
        }
    }
}
