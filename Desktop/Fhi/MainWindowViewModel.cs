using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using Fhi.Controls.Infrastructure;
using Fhi.Controls.MVVM;
using Fhi.Controls.Utils;
using Fhi.Properties;
using FhiModel;


namespace Fhi
{
    public class MainWindowViewModel : ViewModelBase
    {
        private readonly BackstageViewModel _backstage;
        private readonly HomeViewModel _home;
        private NavigationViewModel _selectedViewModel;

        public MainWindowViewModel()
        {
            Globals.Model = new Model(Settings.Default.User);
            _home = new HomeViewModel(Navigate, null);
            _home.Backstage = _backstage = new BackstageViewModel(Navigate, _home);
            if (!Globals.Model.Assets.Exists("BasinShapefile"))
                SelectedViewModel = new SplashViewModel(Navigate, _backstage, _home);
            else
                SelectedViewModel = _home;

            RaisePropertyChanged(nameof(Title));
            _home.PropertyChanged += (sender, args) => RaisePropertyChanged(nameof(Title));
            _backstage.PropertyChanged += (sender, args) => RaisePropertyChanged(nameof(Title));
        }

        public NavigationViewModel SelectedViewModel
        {
            get => _selectedViewModel;
            set => Set(ref _selectedViewModel, value);
        }

        private void Navigate(NavigationViewModel viewModel)
        {
            SelectedViewModel = viewModel;
        }

        public String Title => $"Freshwater Health Index Tool {Assembly.GetExecutingAssembly().GetName().Version}"
                               + (!String.IsNullOrWhiteSpace(_backstage.Filename) ? $" [{Path.GetFileNameWithoutExtension(_backstage.Filename)}]" : String.Empty)
                               + (_home.Modified ? " *" : String.Empty);


        public Boolean TryExit()
        {
            if (!_home.Modified || Application.Current?.MainWindow == null) return true;
            
            var message = "Your assessment has been modified, would you like to save it?";
            if (!String.IsNullOrWhiteSpace(_backstage.Filename))
                message = $"The {Path.GetFileNameWithoutExtension(_backstage.Filename)} assessment has been modified, would you like to save it?"; 
            
            var answer = MessageBox.Show(Application.Current.MainWindow,
                message,
                "Save assessment file?",
                MessageBoxButton.YesNoCancel);
            
            switch (answer)
            {
                case MessageBoxResult.Yes:
                    _backstage.SaveCommand.Execute(null);
                    return true;
                case MessageBoxResult.No:
                    return true;
                default:
                    return false;
            }
        }
    }
}
