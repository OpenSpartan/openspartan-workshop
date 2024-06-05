using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using OpenSpartan.Workshop.Core;
using OpenSpartan.Workshop.ViewModels;
using System.Linq;

namespace OpenSpartan.Workshop.Views
{
    public sealed partial class BattlePassDetailView : Page
    {
        public BattlePassDetailView()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            DataContext = (from c in BattlePassViewModel.Instance.BattlePasses where c.RewardTrack.RewardTrackPath == e.Parameter.ToString() select c).FirstOrDefault();

            base.OnNavigatedTo(e);
        }

        private void BattlePassItem_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            var teachingTip = UserInterface.FindChildElement<TeachingTip>((DependencyObject)sender);
            if (teachingTip != null)
            {
                teachingTip.IsOpen = true;
            }
        }
    }
}
