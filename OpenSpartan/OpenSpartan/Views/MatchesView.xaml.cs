using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.UI.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using OpenSpartan.Workshop.Models;
using OpenSpartan.Workshop.Shared;
using OpenSpartan.Workshop.ViewModels;
using System.Threading.Tasks;

namespace OpenSpartan.Workshop.Views
{
    public sealed partial class MatchesView : Page
    {
        public MatchesView()
        {
            this.InitializeComponent();
        }

        private void ScrollViewer_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            var sv = sender as ScrollViewer;

            if (sv != null)
            {
                if (sv.ScrollableHeight - sv.VerticalOffset == 0)
                {
                    Task.Run(() =>
                    {
                        UserContextManager.GetPlayerMatches();
                    });
                }
            }
        }

        private async void btnRefreshMatches_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            var matchRecordsOutcome = await UserContextManager.PopulateMatchRecordsData();
            
            if (matchRecordsOutcome)
            {
                await UserContextManager.DispatcherWindow.DispatcherQueue.EnqueueAsync(() =>
                {
                    MatchesViewModel.Instance.MatchLoadingState = MetadataLoadingState.Completed;
                });
            }
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
