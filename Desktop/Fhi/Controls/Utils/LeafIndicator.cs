using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using FhiModel.Common;
using MaterialDesignThemes.Wpf;

namespace Fhi.Controls.Utils
{
    public class LeafIndicator : Card
    {
        public static readonly DependencyProperty IndicatorNameProperty = DependencyProperty.Register(
            "IndicatorName", typeof(String), typeof(LeafIndicator), new PropertyMetadata(default(String)));

        public String IndicatorName
        {
            get => (String)GetValue(IndicatorNameProperty);
            set => SetValue(IndicatorNameProperty, value);
        }

        public static readonly DependencyProperty IndicatorProperty = DependencyProperty.Register(
            "Indicator", typeof(IIndicator), typeof(LeafIndicator), new PropertyMetadata(default(IIndicator)));

        public IIndicator Indicator
        {
            get => (IIndicator)GetValue(IndicatorProperty);
            set => SetValue(IndicatorProperty, value);
        }

        public static readonly DependencyProperty ContextHelpProperty = DependencyProperty.Register(
            "ContextHelp", typeof(String), typeof(LeafIndicator), new PropertyMetadata(default(String)));

        public String ContextHelp
        {
            get => (String)GetValue(ContextHelpProperty);
            set => SetValue(ContextHelpProperty, value);
        }

        public static readonly DependencyProperty EditCommandProperty = DependencyProperty.Register(
            "EditCommand", typeof(ICommand), typeof(LeafIndicator), new PropertyMetadata(default(ICommand)));

        public ICommand EditCommand
        {
            get { return (ICommand) GetValue(EditCommandProperty); }
            set { SetValue(EditCommandProperty, value); }
        }
    }
}
