using Microsoft.UI.Xaml.Controls;
using OpenSpartan.Workshop.Core;

namespace OpenSpartan.Workshop.Views
{
    public sealed partial class RankedView : Page
    {
        public RankedView()
        {
            this.InitializeComponent();
        }

        private async void btnRankedRefresh_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            await UserContextManager.PopulateServiceRecordData();
        }
    }
}
