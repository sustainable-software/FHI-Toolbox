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
    public class PillarIndicator : Button
    {
        public static readonly DependencyProperty IndicatorNameProperty = DependencyProperty.Register(
            "IndicatorName", typeof(String), typeof(PillarIndicator), new PropertyMetadata(default(String)));

        public String IndicatorName
        {
            get => (String)GetValue(IndicatorNameProperty);
            set => SetValue(IndicatorNameProperty, value);
        }

        public static readonly DependencyProperty IndicatorProperty = DependencyProperty.Register(
            "Indicator", typeof(IIndicator), typeof(PillarIndicator), new PropertyMetadata(default(IIndicator)));

        public IIndicator Indicator
        {
            get => (IIndicator)GetValue(IndicatorProperty);
            set => SetValue(IndicatorProperty, value);
        }

        public static readonly DependencyProperty ContextHelpProperty = DependencyProperty.Register(
            "ContextHelp", typeof(String), typeof(PillarIndicator), new PropertyMetadata(default(String)));

        public String ContextHelp
        {
            get => (String) GetValue(ContextHelpProperty);
            set => SetValue(ContextHelpProperty, value);
        }
    }
}
