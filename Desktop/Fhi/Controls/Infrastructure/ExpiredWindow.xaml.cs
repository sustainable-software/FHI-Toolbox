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

namespace Fhi.Controls.Infrastructure
{
    /// <summary>
    /// Interaction logic for ExpiredWindow.xaml
    /// </summary>
    public partial class ExpiredWindow : Window
    {
        public ExpiredWindow()
        {
            InitializeComponent();
        }
        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(e.Uri.ToString());
                e.Handled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not run {e.Uri} : {ex.Message}.");
            }
        }
    }
}
