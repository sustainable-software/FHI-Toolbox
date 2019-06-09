using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Fhi.Controls.MVVM;
using Fhi.Controls.Utils;
using Fhi.Properties;
using FhiModel;
using Microsoft.Win32;

namespace Fhi.Controls.Infrastructure
{
    public class BackstageViewModel : NavigationViewModel
    {
        private readonly HomeViewModel _home;
        private String _filename;
        private ViewModelBase _selectedViewModel;
        private readonly OpenViewModel _open;
        private bool _openSelected;

        public BackstageViewModel() { }

        public BackstageViewModel(Action<NavigationViewModel> navigate, HomeViewModel home) : base(navigate, home)
        {
            _home = home;

            OpenCommand = new RelayCommand(_ => SelectedViewModel = _open);
            InfoCommand = new RelayCommand(_ => SelectedViewModel = new InfoViewModel());
            AboutCommand = new RelayCommand(_ => SelectedViewModel = new AboutViewModel());
            ImportCommand = new RelayCommand(_ => SelectedViewModel = new ImportBasinViewModel());
            OptionsCommand = new RelayCommand(_ => SelectedViewModel = new OptionsViewModel());
            BackCommand = new RelayCommand(Back);

            NewCommand = new RelayCommand(New);
            SaveCommand = new RelayCommand(Save);
            SaveAsCommand = new RelayCommand(SaveAs);

            _open = new OpenViewModel(NavigateBack);
            _open.PropertyChanged += (sender, args) => Filename = _open.Filename;

            var commandLineArgs = Environment.GetCommandLineArgs();
            if (commandLineArgs.Length == 2)
                _open.Open(commandLineArgs[1]);

            Reset();
        }
        public ICommand InfoCommand { get; }
        public ICommand OpenCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand SaveAsCommand { get; }
        public ICommand NewCommand { get; }
        public ICommand AboutCommand { get; }
        public ICommand ImportCommand { get; }
        public ICommand OptionsCommand { get; }

        private void Reset()
        {
            OpenSelected = true;
            SelectedViewModel = _open;
        }

        private void Back()
        {
            Navigate(_home);
            Reset();
        }

        public Boolean OpenSelected
        {
            get => _openSelected;
            set => Set(ref _openSelected, value);
        }

        public ViewModelBase SelectedViewModel
        {
            get => _selectedViewModel;
            set => Set(ref _selectedViewModel, value);
        }

        #region File Management
        
        public String Filename
        {
            get => _filename;
            set => Set(ref _filename, value);
        }

        private void New()
        {
            Globals.Model = new Model(Settings.Default.User);
            NavigateBack();
        }

        private void SaveAs()
        {
            Filename = null;
            Save();
        }

        private void Save()
        {
            if (String.IsNullOrWhiteSpace(Filename))
            {
                var dialog = new SaveFileDialog
                {
                    Title = "Save FHI File",
                    Filter = "FHI File (*.fhix)|*.fhix",
                    DefaultExt = ".fhix",
                };
                if (dialog.ShowDialog() != true)
                {
                    NavigateBack();
                    return;
                }
                Filename = dialog.FileName;
            }
            var progress = new ProgressDialog { Owner = Application.Current.MainWindow, Label = "Saving...", IsCancellable = true };
            try
            {
                progress.Execute((ct, p) => Globals.Model.Write(Filename, ct, p));
                _open.UpdateHistory(Filename, Globals.Model);
                _home.Modified = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Save error: {CollectErrors(ex)}");
            }
            NavigateBack();
        }

        private String CollectErrors(Exception ex)
        {
            var sb = new StringBuilder();
            if (ex.InnerException != null)
                sb.Append(CollectErrors(ex.InnerException));
            sb.Append(ex.Message);
            return sb.ToString();
        }
        #endregion File Management
    }
}
