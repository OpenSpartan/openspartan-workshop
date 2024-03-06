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
            this.Loaded += MedalMatchesView_Loaded;
        }

        private void MedalMatchesView_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            ((MedalMatchesViewModel)this.DataContext).NavigationRequested += MedalMatchesView_NavigationRequested;
        }

        private void MedalMatchesView_NavigationRequested(object sender, long e)
        {
            // Once navigation starts, it's safe to assume that the match loading begins, so
            // we want to make sure that the infobar is properly displayed once the view is rendered.
            MedalMatchesViewModel.Instance.MatchLoadingState = Models.MetadataLoadingState.Loading;

            Frame.Navigate(typeof(MedalMatchesView), e);
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
            ((MedalMatchesViewModel)this.DataContext).NavigationRequested -= MedalMatchesView_NavigationRequested;
            ((MedalMatchesViewModel)this.DataContext).Dispose();
            this.DataContext = null;

            base.OnNavigatedFrom(e);
        }
    }
}
