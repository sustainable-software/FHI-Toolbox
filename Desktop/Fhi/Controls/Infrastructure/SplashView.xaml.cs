using System.Windows.Controls;

namespace Fhi.Controls.Infrastructure
{
    public partial class SplashView : UserControl
    {
        public SplashView()
        {
            InitializeComponent();
        }
        
        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            System.Diagnostics.Process.Start(e.Uri.AbsoluteUri);
            e.Handled = true;
        }
    }
}
