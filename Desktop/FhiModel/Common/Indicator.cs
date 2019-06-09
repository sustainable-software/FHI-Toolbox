using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace FhiModel.Common
{
    
    [DataContract(Namespace = "", IsReference = true)]
    public class Indicator : ModelBase, IIndicator
    {
        private Int32? _value;
        private String _name;
        private ObservableCollection<IIndicator> _children;
        private Double? _weight;
        private Int32? _userOverride;
        private String _overrideComment;
        private String _notes;
        private Uncertainty _rank;

        public Indicator()
        {
            OnDeserialized();
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context = default(StreamingContext))
        {
            
        }

        [DataMember]
        public String Name
        {
            get => _name;
            set => Set(ref _name, value);
        }

        [IgnoreDataMember]
        public Int32? Value
        {
            // Note that Value is ignored for serialization. That forces a recompute when
            // the file is open, allowing computation problems to get fixed in
            // future releases.
            get => _userOverride ?? (_value ?? (_value = ComputeIndicator()));
            set
            {
                if (value == null)
                {
                    _value = ComputeIndicator();
                    if (_value != null)
                        RaisePropertyChanged();
                }
                else
                {
                    Set(ref _value, value);
                }
            }
        }

        [IgnoreDataMember]
        public Boolean HasMetadata => Rank != Uncertainty.Undefined || !String.IsNullOrWhiteSpace(Notes);

        [DataMember]
        public Uncertainty Rank
        {
            get => _rank;
            set
            {
                Set(ref _rank, value);
                RaisePropertyChanged(nameof(HasMetadata));
            }
        }

        [DataMember]
        public Int32? UserOverride
        {
            get => _userOverride;
            set
            {
                if (Set(ref _userOverride, value))
                    RaisePropertyChanged(nameof(Value));               
            }
        }

        [DataMember]
        public String OverrideComment
        {
            get => _overrideComment;
            set => Set(ref _overrideComment, value);
        }

        [DataMember]
        public String Notes
        {
            get => _notes;
            set
            {
                Set(ref _notes, value);
                RaisePropertyChanged(nameof(HasMetadata));
            }
        }

        [DataMember]
        public ObservableCollection<IIndicator> Children
        {
            get => _children;
            set
            {
                if (!Set(ref _children, value)) return;
                if (_children == null) return;
                
                foreach (var child in _children)
                    child.PropertyChanged += IndicatorOnPropertyChanged;
                
                _children.CollectionChanged += (sender, args) =>
                {
                    switch (args.Action)
                    {
                        case NotifyCollectionChangedAction.Add:
                            foreach (var item in args.NewItems)
                            {
                                if (!(item is Indicator indicator)) continue;
                                indicator.PropertyChanged += IndicatorOnPropertyChanged;
                            }
                            break;
                        case NotifyCollectionChangedAction.Remove:
                            foreach (var item in args.OldItems)
                            {
                                if (!(item is Indicator indicator)) continue;
                                indicator.PropertyChanged -= IndicatorOnPropertyChanged;
                            }
                            break;
                    }
                };
                Value = null;
            }
        }

        private void IndicatorOnPropertyChanged(Object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Value) || e.PropertyName == nameof(UserOverride))
                Value = null;
        }

        [DataMember]
        public Double? Weight
        {
            get => _weight;
            set 
            { 
                if (Set(ref _weight, value))
                    Value = null; 
            }
        }

        /// <summary>
        /// Compute the value of the indicator and return it. 
        /// </summary>
        protected virtual Int32? ComputeIndicator()
        {
            var terms = 0.0;
            var weights = 0.0;
            var empty = true;
            if (Children != null)
            {
                foreach (var child in Children)
                {
                    if (child.Value == null || child.Weight == null)
                        continue;
                    var value = Math.Pow(child.Value.Value, child.Weight.Value);
                    if (terms == 0.0)
                        terms = value;
                    else
                        terms *= value;
                    empty = false;
                    weights += child.Weight.Value;
                }
            }

            if (empty) return null;
            var score = Math.Pow(terms, 1/weights);
            return (Int32) Math.Round(score, 0);
        }

        public override String ToString()
        {
            return Name;
        }
    }

    [DataContract(Namespace = "")]
    public enum Uncertainty
    {
        [EnumMember]
        Undefined,
        [EnumMember]
        High,
        [EnumMember]
        Medium,
        [EnumMember]
        Low
    }
}
