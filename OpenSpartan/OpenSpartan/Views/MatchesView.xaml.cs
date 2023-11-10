using Microsoft.UI.Xaml.Controls;
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

        private void dgdMatches_AutoGeneratingColumn(object sender, CommunityToolkit.WinUI.UI.Controls.DataGridAutoGeneratingColumnEventArgs e)
        {
            switch (e.Column.Header.ToString())
            {
                case "MatchId":
                    {
                        e.Column.Header = "Match ID";
                        break;
                    }
                case "StartTime":
                    {
                        e.Column.Header = "Start Time";
                        break;
                    }
                case "GameVariant":
                    {
                        e.Column.Header = "Mode";
                        break;
                    }
                case "LastTeamId":
                    {
                        e.Cancel = true;
                        break;
                    }
                case "Teams":
                    {
                        e.Cancel = true;
                        break;
                    }
                case "PlayerTeamStats":
                    {
                        e.Cancel = true;
                        break;
                    }
                case "ParticipationInfo":
                    {
                        e.Cancel = true;
                        break;
                    }
                default:
                    {
                        // Do nothing.
                        break;
                    }
            }
        }
    }
}
