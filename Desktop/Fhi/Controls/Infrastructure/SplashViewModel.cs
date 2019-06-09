using System;
using System.IO;
using System.Windows.Input;
using Fhi.Controls.MVVM;
using Microsoft.Win32;

namespace Fhi.Controls.Infrastructure
{
    public class SplashViewModel : NavigationViewModel
    {
        private readonly NavigationViewModel _home;
        
        public SplashViewModel(Action<NavigationViewModel> navigate, NavigationViewModel back, NavigationViewModel home) : base(navigate, back)
        {
            BackstageCommand = new RelayCommand(() => Navigate(back));
            BasinShapefileCommand = new RelayCommand(BasinShapefile);

            _home = home;
        }
        
        public ICommand BasinShapefileCommand { get; }
        public ICommand BackstageCommand { get; }
        
        private void BasinShapefile()
        {
            var dialog = new OpenFileDialog
            {
                Title = "Open Shapefile",
                Filter = "GIS Shapefile (*.shp)|*.shp",
                DefaultExt = ".shp"
            };
            if (dialog.ShowDialog() != true) return;
            Model.Assets.Create(Path.GetDirectoryName(dialog.FileName), Path.GetFileNameWithoutExtension(dialog.FileName), "BasinShapefile");
            Navigate(_home);
        }
    }
}