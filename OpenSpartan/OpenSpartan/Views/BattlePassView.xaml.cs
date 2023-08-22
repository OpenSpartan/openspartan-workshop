using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using OpenSpartan.Shared;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace OpenSpartan.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class BattlePassView : Page
    {
        public BattlePassView()
        {
            this.InitializeComponent();
            this.Loaded += BattlePassView_Loaded;
        }

        private async void BattlePassView_Loaded(object sender, RoutedEventArgs e)
        {
            var result = await UserContextManager.LoadBattlePassData();
        }
    }
}
