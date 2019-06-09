using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace FhiModel.Common
{
    [DataContract(Namespace = "", IsReference = true)]
    public abstract class ModelBase : INotifyPropertyChanged, INotifyDataErrorInfo
    {
        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged([CallerMemberName] String prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        #endregion

        #region INotifyDataErrorInfo

        // INotifyDataErrorInfo
        [IgnoreDataMember] 
        private Dictionary<String, List<string>> _errors;

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

        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

        private void RaiseErrorsChanged(String propertyName)
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        }

        #endregion

        protected Boolean Set<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;
            field = value;
            RaisePropertyChanged(propertyName);
            return true;
        }
    }
}