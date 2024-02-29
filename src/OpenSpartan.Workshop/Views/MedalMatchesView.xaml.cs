using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.WinUI;
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

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.NavigationMode != NavigationMode.Back)
            {
                // Access the parameter using QueryParameter
                if (e.Parameter != null && e.Parameter is long parameter)
                {
                    await Task.Run(() => {
                        UserContextManager.DispatcherWindow.DispatcherQueue.EnqueueAsync(() =>
                        {
                            MedalMatchesViewModel.Instance.Medal = MedalsViewModel.Instance.Medals
                            .SelectMany(group => group)
                            .FirstOrDefault(i => i.NameId == parameter);

                            MedalMatchesViewModel.Instance.MatchList = [];
                        });
                    });
                }
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            MedalMatchesViewModel.Instance.MatchList.Clear();
        }
    }
}
