using System.Linq;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using OpenSpartan.Workshop.Shared;
using OpenSpartan.Workshop.ViewModels;

namespace OpenSpartan.Workshop.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MedalMatchesView : Page
    {
        public MedalMatchesView()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // Access the parameter using QueryParameter
            if (e.Parameter != null && e.Parameter is long parameter)
            {
                // Make sure to reset the match list.
                MedalMatchesViewModel.Instance.MatchList = new CommunityToolkit.WinUI.IncrementalLoadingCollection<Data.MedalMatchesSource, Models.MatchTableEntity>();

                UserContextManager.PopulateMedalMatchData(parameter);

                MedalMatchesViewModel.Instance.Medal = MedalsViewModel.Instance.Medals.SelectMany(group => group).FirstOrDefault(i => i.NameId == parameter);
            }
        }
    }
}
