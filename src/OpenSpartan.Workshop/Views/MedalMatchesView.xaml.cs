using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using OpenSpartan.Workshop.Core;
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
            this.DataContext = MedalMatchesViewModel.Instance;

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

            base.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            ((MedalMatchesViewModel)this.DataContext).Dispose();
            this.DataContext = null;

            base.OnNavigatedFrom(e);
        }
    }
}
