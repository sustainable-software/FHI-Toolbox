using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;

namespace FhiModel.Common
{
    public class ModelCheck
    {
        public void Check(Object item)
        {
            switch (item)
            {
                case null:
                    return;
                case INotifyCollectionChanged _:
                {
                    (item as INotifyCollectionChanged).CollectionChanged += OnCollectionChanged;
                    if (item is IEnumerable<INotifyPropertyChanged> children)
                        foreach (var child in children) 
                            Check(child);
                    break;
                }
                case INotifyPropertyChanged _:
                {
                    (item as INotifyPropertyChanged).PropertyChanged += OnPropertyChanged;
                    foreach (var child in item.GetType().GetProperties().Select(pi => pi.GetValue(item))) 
                        Check(child);
                    break;
                }
            }
        }

        public void Stop(Object item)
        {
            switch (item)
            {
                case null:
                    return;
                case INotifyCollectionChanged _:
                {
                    (item as INotifyCollectionChanged).CollectionChanged -= OnCollectionChanged;
                    if (item is IEnumerable<INotifyPropertyChanged> children)
                        foreach (var child in children) 
                            Stop(child);
                    break;
                }
                case INotifyPropertyChanged _:
                {
                    (item as INotifyPropertyChanged).PropertyChanged -= OnPropertyChanged;
                    foreach (var child in item.GetType().GetProperties().Select(pi => pi.GetValue(item))) 
                        Stop(child);
                    break;
                }
            }
        }

        private void OnPropertyChanged(Object sender, PropertyChangedEventArgs args)
        {
            try
            {
                // Check(sender.GetType().GetProperty(args.PropertyName)?.GetValue(sender));
                Trace.WriteLine($"Changed: {args.PropertyName}");
                RaiseChanged();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        private void OnCollectionChanged(Object sender, NotifyCollectionChangedEventArgs args)
        {
            try
            {
                if (args.Action == NotifyCollectionChangedAction.Reset)
                    Check(sender);
                else
                {
                    if (args.OldItems != null)
                        foreach (var item in args.OldItems)
                            Stop(item);
                    if (args.NewItems != null)
                        foreach (var item in args.NewItems)
                            Check(item);
                }
                Trace.WriteLine($"Changed: {sender.GetType()}");
                RaiseChanged();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }
        
        public event EventHandler Changed;

        private void RaiseChanged()
        {
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }
}