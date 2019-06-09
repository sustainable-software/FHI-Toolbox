using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Fhi.Controls.MVVM;
using Fhi.Controls.Utils;
using FhiModel;
using Microsoft.Win32;

namespace Fhi.Controls.Infrastructure
{
    public class OpenViewModel : ViewModelBase
    {
        private readonly Action _navigateBack;
        private String _filename;

        public OpenViewModel(Action navigateBack)
        {
            _navigateBack = navigateBack;

            BrowseCommand = new RelayCommand(Browse);
            OpenCommand = new RelayCommand(o => Open((o as RecentFile)?.Path));

            FileHistory = RecentFile.ReadRecentFiles();
        }

        public List<RecentFile> FileHistory { get; set; }
        public ICommand BrowseCommand { get; }
        public ICommand OpenCommand { get; }

        public String Filename
        {
            get => _filename;
            set => Set(ref _filename, value);
        }

        public void UpdateHistory(String filename, Model model)
        {
            var history = FileHistory.FirstOrDefault(x => x.Path == filename);
            if (history == null)
                FileHistory.Add(new RecentFile(filename, model));
            else
                history.Update(model);
            RecentFile.WriteRecentFiles(FileHistory);
            FileHistory.Sort();
        }

        private void Browse()
        {
            var dialog = new OpenFileDialog
            {
                Title = "Open FHI File",
                Filter = "FHI File (*.fhix)|*.fhix",
                DefaultExt = ".fhix"
            };
            if (dialog.ShowDialog() != true) return;
            Open(dialog.FileName);
        }

        public void Open(string filename)
        {
            if (String.IsNullOrWhiteSpace(filename) || !File.Exists(filename))
            {
                MessageBox.Show($"{filename} does not exist.");
                return;
            }

            var progress = new ProgressDialog { IsCancellable = true, Label = "Opening..." };
            if (Application.Current.MainWindow?.IsVisible == true)
                progress.Owner = Application.Current.MainWindow;
            try
            {
                Globals.Model = progress.Execute((ct, p) => Model.Read(filename, ct, p));
                Filename = filename;
                UpdateHistory(filename, Globals.Model);
            }
            catch (Exception ex)
            {
                var msg = CollectErrors(ex);
                MessageBox.Show($"Open error: {msg}");
            }
            _navigateBack();
        }

        private String CollectErrors(Exception ex)
        {
            var sb = new StringBuilder();
            if (ex.InnerException != null)
                sb.Append(CollectErrors(ex.InnerException));
            sb.Append(ex.Message);
            return sb.ToString();
        }
    }
}
