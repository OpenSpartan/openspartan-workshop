using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections;
using OpenSpartan.Workshop.Core;
using CommunityToolkit.WinUI.UI.Controls;

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

        public static readonly DependencyProperty MedalNavigationCommandProperty =
        DependencyProperty.Register(
            "MedalNavigationCommand",            // Name of the property
            typeof(RelayCommand<long>),            // Type of the property
            typeof(MatchesGridControl),   // Type of the owner (your user control)
            new PropertyMetadata(null) // Optional metadata, e.g., default value
        );

        public IEnumerable MatchSource
        {
            get { return (IEnumerable)GetValue(MatchSourceProperty); }
            set { SetValue(MatchSourceProperty, value); }
        }

        public RelayCommand<long> MedalNavigationCommand
        {
            get { return (RelayCommand<long>)GetValue(MedalNavigationCommandProperty); }
            set { SetValue(MedalNavigationCommandProperty, value); }
        }

        public MatchesGridControl()
        { 
            this.InitializeComponent();
        }

        private void dgdMatches_PointerReleased(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            StackPanel detailContainer = UserInterface.FindParentElement<StackPanel>((UIElement)e.OriginalSource);

            if (detailContainer == null)
            {
                DataGridRow row = UserInterface.FindParentElement<DataGridRow>((UIElement)e.OriginalSource);

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
        }

        private void MedalGridItem_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            var teachingTip = UserInterface.FindChildElement<TeachingTip>((DependencyObject)sender);
            if (teachingTip != null)
            {
                teachingTip.IsOpen = true;
                teachingTip.ActionButtonCommand = this.MedalNavigationCommand;
            }
        }
    }
}
