using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.UI.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using OpenSpartan.Workshop.Models;
using OpenSpartan.Workshop.Shared;
using OpenSpartan.Workshop.ViewModels;
using System;
using System.Threading.Tasks;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace OpenSpartan.Workshop.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
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
            DataGridRow t = FindParent<DataGridRow>((UIElement)e.OriginalSource);
            if (t.DetailsVisibility == Visibility.Visible)
            {
                t.DetailsVisibility = Visibility.Collapsed;
            }
            else
            {
                t.DetailsVisibility = Visibility.Visible;
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
