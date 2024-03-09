using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using OpenSpartan.Workshop.Core;
using OpenSpartan.Workshop.ViewModels;

namespace OpenSpartan.Workshop.Views
{
    public sealed partial class MedalsView : Page
    {
        public MedalsView()
        {
            InitializeComponent();
            this.Loaded += MedalsView_Loaded;
            this.Unloaded += MedalsView_Unloaded;
        }

        private void MedalsView_Unloaded(object sender, RoutedEventArgs e)
        {
            ((MedalsViewModel)this.DataContext).NavigationRequested -= ViewModel_NavigationRequested;
        }

        private void MedalsView_Loaded(object sender, RoutedEventArgs e)
        {
            ((MedalsViewModel)this.DataContext).NavigationRequested += ViewModel_NavigationRequested;
        }

        private void ViewModel_NavigationRequested(object sender, long e)
        {
            // Once navigation starts, it's safe to assume that the match loading begins, so
            // we want to make sure that the infobar is properly displayed once the view is rendered.
            MedalMatchesViewModel.Instance.MatchLoadingState = Models.MetadataLoadingState.Loading;

            Frame.Navigate(typeof(MedalMatchesView), e);
        }

        private async void btnRefreshMedals_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            await UserContextManager.PopulateServiceRecordData();
            await UserContextManager.PopulateMedalData();
        }

        private void MedalGridItem_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            var teachingTip = UserInterface.FindChildElement<TeachingTip>((DependencyObject)sender);
            if (teachingTip != null)
            {
                teachingTip.IsOpen = true;
            }
        }
    }
}
