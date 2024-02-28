using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using OpenSpartan.Workshop.Shared;
using OpenSpartan.Workshop.ViewModels;

namespace OpenSpartan.Workshop.Views
{
    public sealed partial class MedalsView : Page
    {
        public MedalsView()
        {
            InitializeComponent();
            this.Loaded += MedalsView_Loaded;
        }

        private void MedalsView_Loaded(object sender, RoutedEventArgs e)
        {
            ((MedalsViewModel)this.DataContext).NavigationRequested += ViewModel_NavigationRequested;
        }

        private void ViewModel_NavigationRequested(object sender, long e)
        {
            Frame.Navigate(typeof(MedalMatchesView), e);
        }

        private async void btnRefreshMedals_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            await UserContextManager.PopulateServiceRecordData();
            await UserContextManager.PopulateMedalData();
        }

        private void MedalGridItem_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            var teachingTip = FindChildElement<TeachingTip>((DependencyObject)sender);
            if (teachingTip != null)
            {
                teachingTip.IsOpen = true;
            }
        }

        public static T FindChildElement<T>(DependencyObject parent) where T : FrameworkElement
        {
            if (parent == null)
                return null;

            int childCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childCount; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);

                if (child is T typedChild)
                {
                    return typedChild;
                }

                T result = FindChildElement<T>(child);
                if (result != null)
                    return result;
            }

            return null;
        }
    }
}
