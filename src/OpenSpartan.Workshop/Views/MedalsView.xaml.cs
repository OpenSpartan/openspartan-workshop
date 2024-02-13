using Microsoft.UI.Xaml.Controls;
using OpenSpartan.Workshop.Shared;

namespace OpenSpartan.Workshop.Views
{
    public sealed partial class MedalsView : Page
    {
        public MedalsView()
        {
            InitializeComponent();
        }

        private async void btnRefreshMedals_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            await UserContextManager.PopulateServiceRecordData();
            await UserContextManager.PopulateMedalData();
        }
    }
}
