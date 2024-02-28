using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using CommunityToolkit.WinUI.UI.Controls;
using System.Collections;

namespace OpenSpartan.Workshop.Controls
{
    public sealed partial class MatchesGridControl : UserControl
    {
        public static readonly DependencyProperty MatchSourceProperty =
        DependencyProperty.Register(
            "MatchSource",            // Name of the property
            typeof(IEnumerable),            // Type of the property
            typeof(MatchesGridControl),   // Type of the owner (your user control)
            new PropertyMetadata(null) // Optional metadata, e.g., default value
        );

        public IEnumerable MatchSource
        {
            get { return (IEnumerable)GetValue(MatchSourceProperty); }
            set { SetValue(MatchSourceProperty, value); }
        }

        public MatchesGridControl()
        {
            this.InitializeComponent();
        }

        private void dgdMatches_PointerReleased(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            DataGridRow row = FindParent<DataGridRow>((UIElement)e.OriginalSource);

            if (row != null)
            {
                if (row.DetailsVisibility == Visibility.Visible)
                {
                    row.DetailsVisibility = Visibility.Collapsed;
                }
                else
                {
                    row.DetailsVisibility = Visibility.Visible;
                }
            }
        }

        public static T FindParent<T>(DependencyObject childElement) where T : Control
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
