using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Fhi.Controls.MVVM;

namespace Fhi.Controls.Utils
{
    public static class GlobalCommands
    {
        static GlobalCommands()
        {
            HelpCommand = new RelayCommand(path => Help(path as String));
        }

        public static ICommand HelpCommand { get; }

        private static void Help(String path)
        {
            var url = $"https://www.freshwaterhealthindex.org/tool/{path}";
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
