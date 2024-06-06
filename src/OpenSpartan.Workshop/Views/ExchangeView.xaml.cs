using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using OpenSpartan.Workshop.Core;

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
    }
}
