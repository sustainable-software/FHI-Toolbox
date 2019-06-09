using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using FhiModel.Common;

namespace Fhi.Controls.Utils
{
    /// <summary>
    /// Interaction logic for SubIndicatorView.xaml
    /// </summary>
    public partial class SubIndicatorView : UserControl
    {
        public SubIndicatorView()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty IndicatorNameProperty = DependencyProperty.Register(
            "IndicatorName", typeof(String), typeof(SubIndicatorView), new PropertyMetadata(default(String)));

        public String IndicatorName
        {
            get { return (String)GetValue(IndicatorNameProperty); }
            set { SetValue(IndicatorNameProperty, value); }
        }

        public static readonly DependencyProperty IndicatorProperty = DependencyProperty.Register(
            "Indicator", typeof(IIndicator), typeof(SubIndicatorView), new PropertyMetadata(default(IIndicator)));

        public IIndicator Indicator
        {
            get { return (IIndicator)GetValue(IndicatorProperty); }
            set { SetValue(IndicatorProperty, value); }
        }

        public static readonly DependencyProperty SelectedProperty = DependencyProperty.Register(
            "Selected", typeof(Boolean), typeof(SubIndicatorView), new PropertyMetadata(default(Boolean)));

        public Boolean Selected
        {
            get { return (Boolean) GetValue(SelectedProperty); }
            set { SetValue(SelectedProperty, value); }
        }

        public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(
            "Command", typeof(ICommand), typeof(SubIndicatorView), new PropertyMetadata(default(ICommand)));

        public ICommand Command
        {
            get { return (ICommand) GetValue(CommandProperty); }
            set { SetValue(CommandProperty, value); }
        }

        public static readonly DependencyProperty ContextHelpProperty = DependencyProperty.Register(
            "ContextHelp", typeof(String), typeof(SubIndicatorView), new PropertyMetadata(default(String)));

        public String ContextHelp
        {
            get { return (String) GetValue(ContextHelpProperty); }
            set { SetValue(ContextHelpProperty, value); }
        }

    }
}
