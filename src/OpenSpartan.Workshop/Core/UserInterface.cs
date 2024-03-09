using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace OpenSpartan.Workshop.Core
{
    internal static class UserInterface
    {
        internal static T FindChildElement<T>(DependencyObject parent) where T : FrameworkElement
        {
            if (parent == null)
                return null;

            int childCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childCount; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);

                if (child is T typedChild)
                {
                    return typedChild;
                }

                T result = FindChildElement<T>(child);
                if (result != null)
                    return result;
            }

            return null;
        }

        internal static T FindParentElement<T>(DependencyObject childElement) where T : FrameworkElement
        {
            DependencyObject currentElement = childElement;

            while (currentElement != null)
            {
                if (currentElement is T matchingElement)
                {
                    return matchingElement;
                }

                currentElement = VisualTreeHelper.GetParent(currentElement);
            }

            return null;
        }
    }
}
