using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.UI.Controls;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Navigation;
using OpenSpartan.Shared;
using System.Threading.Tasks;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace OpenSpartan.Views
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

            if (sv != null )
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
    }
}
