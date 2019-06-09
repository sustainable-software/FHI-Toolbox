using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Threading;
using Fhi.Controls.Utils;

namespace Fhi.Controls.MVVM
{
    public abstract class ViewModelBase : INotifyPropertyChanged, INotifyDataErrorInfo
    {
        #region INotifyPropertyChanged
        protected void RaisePropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;
        #endregion

        #region INotifyDataErrorInfo
        private Dictionary<String, List<String>> _errors;

        public IEnumerable GetAllErrors()
        {
            return (_errors != null)
                ? _errors.SelectMany(kv => kv.Value)
                : Enumerable.Empty<String>();
        }

        public IEnumerable GetErrors(String propertyName)
        {
            return (_errors != null && propertyName != null && _errors.ContainsKey(propertyName))
                ? _errors[propertyName]
                : Enumerable.Empty<String>();
        }

        public void ClearErrors([CallerMemberName] String propertyName = "")
        {
            SetErrors(null, propertyName);
        }

        public void SetErrors(List<String> errors, [CallerMemberName] String propertyName = "")
        {
            if (_errors == null)
                _errors = new Dictionary<String, List<String>>();
            if (errors != null && errors.Count > 0)
                _errors[propertyName] = new List<String>(errors);
            else
                _errors.Remove(propertyName);
            RaiseErrorsChanged(propertyName);
        }

        public Boolean HasErrors => _errors != null && _errors.Count > 0;

        private void RaiseErrorsChanged(String propertyName)
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        }

        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;
        #endregion

        protected Boolean Set<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;
            field = value;
            RaisePropertyChanged(propertyName);
            return true;
        }

        private Lazy<Boolean> InDesignModeLazy { get; } = new Lazy<Boolean>(() => DesignerProperties.GetIsInDesignMode(new DependencyObject()));
        protected Boolean InDesignMode => InDesignModeLazy.Value;
        
        private Lazy<Boolean> UnitTestLazy { get; } = new Lazy<bool>(() =>
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.FullName.ToLowerInvariant())
                .Any(name => name.StartsWith("microsoft.visualstudio.qualitytools.unittestframework"));
        });
        protected Boolean UnitTest => UnitTestLazy.Value;

        protected void CallLater(Action action)
        {
            Dispatcher.CurrentDispatcher.Invoke(action, DispatcherPriority.Background);
        }

        public FhiModel.Model Model => Globals.Model;
    }
}
