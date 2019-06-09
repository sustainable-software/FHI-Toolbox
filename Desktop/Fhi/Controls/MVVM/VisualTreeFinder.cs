using System.Windows;
using System.Windows.Media;

namespace Fhi.Controls.MVVM
{
    public static class VisualTreeFinder
    {

        /// <summary>
        /// Find a specific parent object type in the visual tree
        /// </summary>
        public static T FindParentControl<T>(DependencyObject outerDepObj) where T : DependencyObject
        {
            DependencyObject dObj = VisualTreeHelper.GetParent(outerDepObj);
            if (dObj == null)
                return null;

            if (dObj is T)
                return dObj as T;

            while ((dObj = VisualTreeHelper.GetParent(dObj)) != null)
            {
                if (dObj is T)
                    return dObj as T;
            }

            return null;
        }

    }
}
