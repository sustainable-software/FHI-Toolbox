using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;

namespace Fhi.Controls.MVVM
{
    public static class DialogBehavior
    {
        /*
            IsOk property.

            An attached property for defining the OK button on a dialog.
            This property can be set on any button, if it is set to true, when enter is pressed, or
            the button is clicked, the dialog will be closed, and the dialog result will be set to
            true.
        */

        public static Boolean GetIsOk(DependencyObject obj)
        {
            return (Boolean)obj.GetValue(IsOkProperty);
        }

        public static void SetIsOk(DependencyObject obj, Boolean value)
        {
            obj.SetValue(IsOkProperty, value);
        }

        public static readonly DependencyProperty IsOkProperty = DependencyProperty.RegisterAttached("IsOk", typeof(Boolean),
            typeof(DialogBehavior), new UIPropertyMetadata { DefaultValue = false, PropertyChangedCallback = OnIsOkPropertyChanged });


        private static void OnIsOkPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            if (!(obj is Button) || !(e.NewValue is Boolean)) return;

            var button = obj as Button;
            var value = (Boolean)e.NewValue;

            if (value)
                button.Click += Ok_OnClick;
            else
                button.Click -= Ok_OnClick;

            button.IsDefault = value;
        }

        private static void Ok_OnClick(object sender, RoutedEventArgs e)
        {
            if (!(sender is DependencyObject)) return;

            var parent = VisualTreeFinder.FindParentControl<Window>(sender as DependencyObject);
            if (parent == null) return;

            if (ComponentDispatcher.IsThreadModal)
                parent.DialogResult = true;
            else
                parent.Close();
        }
    }
}
