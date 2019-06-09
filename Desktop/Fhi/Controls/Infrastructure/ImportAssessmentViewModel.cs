using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using System.Windows.Input;
using Fhi.Controls.MVVM;
using Fhi.Controls.Utils;
using FhiModel;
using FhiModel.Common;
using Microsoft.Win32;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;

namespace Fhi.Controls.Infrastructure
{
    public class ImportAssessmentViewModel : ViewModelBase
    {
        private ObservableCollection<ImportIndicatorViewModel> _indicators;
        private RecentFile _openFileInfo;

        public ImportAssessmentViewModel()
        {
            OpenCommand = new RelayCommand(Open);
            ClearCommand = new RelayCommand(Clear);
        }

        public RecentFile OpenFileInfo
        {
            get => _openFileInfo;
            set
            {
                Set(ref _openFileInfo, value);
                RaisePropertyChanged(nameof(BrowseMode));
            }
        }

        public Boolean BrowseMode => OpenFileInfo != null;

        public ICommand OpenCommand { get; }
        public ICommand ClearCommand { get; }

        public ObservableCollection<ImportIndicatorViewModel> Indicators
        {
            get => _indicators;
            private set => Set(ref _indicators, value);
        }

        private void Clear()
        {
            OpenFileInfo = null;
            Indicators = null;
        }

        private void Open()
        {
            var dialog = new OpenFileDialog
            {
                Title = "Open FHI File",
                Filter = "FHI File (*.fhix)|*.fhix",
                DefaultExt = ".fhix"
            };
            if (dialog.ShowDialog() != true) return;
            var filename = dialog.FileName;

            var progress = new ProgressDialog { IsCancellable = true, Label = "Opening..." };
            if (Application.Current.MainWindow?.IsVisible == true)
                progress.Owner = Application.Current.MainWindow;

            try
            {
                var model = progress.Execute((ct, p) => Model.Read(filename, ct, p));
                Indicators = BuildHierarchy(new[] { model.EcosystemVitality, model.EcosystemServices, model.Governance });
                OpenFileInfo = new RecentFile(filename, model);
                RaisePropertyChanged(nameof(BrowseMode));
            }
            catch (Exception ex)
            {
                var msg = CollectErrors(ex);
                MessageBox.Show($"Open error: {msg}");
            }
        }

        private ObservableCollection<ImportIndicatorViewModel> BuildHierarchy(IEnumerable<IIndicator> indicators)
        {
            var rv = new ObservableCollection<ImportIndicatorViewModel>();
            foreach (var indicator in indicators)
            {
                if (indicator.Value == null) continue;
                var vm = new ImportIndicatorViewModel(indicator);
                rv.Add(vm);
                if (indicator.Children?.Count > 0)
                    vm.Children = new List<ImportIndicatorViewModel>(BuildHierarchy(indicator.Children));
            }
            return rv.Count > 0 ? rv: null;
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

    public class ImportIndicatorViewModel : ViewModelBase
    {
        public ImportIndicatorViewModel(IIndicator indicator)
        {
            Indicator = indicator;
            ImportCommand = new RelayCommand(Import);
        }

        public IIndicator Indicator { get; }

        public List<ImportIndicatorViewModel> Children { get; set; }

        public Boolean Importable => Children?.Count > 0;

        public ICommand ImportCommand { get; }

        private void Import()
        {
            IIndicator parent = null;
            foreach (var i in new[] {Model.EcosystemVitality, Model.EcosystemServices, Model.Governance})
            {
                parent = FindParent(i, Indicator);
                if (parent != null) break;
            }
            if (parent == null) return;

            IIndicator replace = null;
            foreach (var child in parent.Children)
            {
                if (Indicator.Name != child.Name) continue;
                replace = child;
                break;
            }
            if (replace == null) return;
            parent.Children.Remove(replace);
            parent.Children.Add(Indicator);
            parent.Value = null;
        }

        private IIndicator FindParent(IIndicator tree, IIndicator indicator)
        {
            if (tree.Children?.Count > 0)
            {
                foreach (var child in tree.Children)
                {
                    if (child.Children?.Count > 0)
                    {
                        var result = FindParent(child, indicator);
                        if (result != null) return result;
                    }
                    if (child.Name == indicator.Name) return tree;
                }
            }
            return null;
        }

        
        public override string ToString()
        {
            return Indicator.Name;
        }
    }
}
