using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using OpenSpartan.ViewModels;
using System.Linq;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace OpenSpartan.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class BattlePassDetailView : Page
    {
        public BattlePassDetailView()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            this.DataContext = (from c in BattlePassViewModel.Instance.BattlePasses where c.RewardTrack.RewardTrackPath == e.Parameter.ToString() select c).FirstOrDefault();

            base.OnNavigatedTo(e);
        }
    }
}
