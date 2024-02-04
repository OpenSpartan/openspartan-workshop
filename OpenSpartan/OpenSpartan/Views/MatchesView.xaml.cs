using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml.Controls;
using OpenSpartan.Workshop.Data;
using OpenSpartan.Workshop.Models;
using OpenSpartan.Workshop.Shared;
using OpenSpartan.Workshop.ViewModels;
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
            await UserContextManager.PopulateMatchRecordsData();
        }
    }
}
