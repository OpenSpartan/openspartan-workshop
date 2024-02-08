using Microsoft.UI.Xaml.Controls;
using OpenSpartan.Workshop.Shared;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace OpenSpartan.Workshop.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MedalsView : Page
    {
        public MedalsView()
        {
            this.InitializeComponent();
        }

        private async void btnRefreshMedals_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            await UserContextManager.PopulateServiceRecordData();
            await UserContextManager.PopulateMedalData();
        }
    }
}
