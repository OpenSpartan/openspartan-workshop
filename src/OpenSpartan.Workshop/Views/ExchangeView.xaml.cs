using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using OpenSpartan.Workshop.Core;
using OpenSpartan.Workshop.Models;
using OpenSpartan.Workshop.ViewModels;

namespace OpenSpartan.Workshop.Views
{
    public sealed partial class ExchangeView : Page
    {
        public ExchangeView()
        {
            this.InitializeComponent();
        }

        private void ExchangeItem_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var teachingTip = UserInterface.FindChildElement<TeachingTip>((DependencyObject)sender);
            if (teachingTip != null)
            {
                teachingTip.IsOpen = true;
            }
        }

        private async void btnRefreshExchange_Click(object sender, RoutedEventArgs e)
        {
            var matchRecordsOutcome = await UserContextManager.PopulateExchangeData();

            if (matchRecordsOutcome)
            {
                await UserContextManager.DispatcherWindow.DispatcherQueue.EnqueueAsync(() =>
                {
                    ExchangeViewModel.Instance.ExchangeLoadingState = MetadataLoadingState.Completed;
                });
            }
        }
    }
}
