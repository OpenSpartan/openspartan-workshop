using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using OpenSpartan.Workshop.Models;

namespace OpenSpartan.Workshop.Controls
{
    // Shared loading-indicator banner used by every page that has a long-running
    // populator (Home / Matches / BattlePass / Exchange / SeasonCalendar / Ranked /
    // MedalMatches). Replaces seven near-identical Grid+ProgressRing+TextBlock
    // blocks of XAML that only differed in their two binding paths.
    public sealed partial class LoadingBanner : UserControl
    {
        public LoadingBanner()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty LoadingStateProperty = DependencyProperty.Register(
            nameof(LoadingState),
            typeof(MetadataLoadingState),
            typeof(LoadingBanner),
            new PropertyMetadata(MetadataLoadingState.Loading));

        public MetadataLoadingState LoadingState
        {
            get => (MetadataLoadingState)GetValue(LoadingStateProperty);
            set => SetValue(LoadingStateProperty, value);
        }

        public static readonly DependencyProperty LoadingTextProperty = DependencyProperty.Register(
            nameof(LoadingText),
            typeof(string),
            typeof(LoadingBanner),
            new PropertyMetadata(string.Empty));

        public string LoadingText
        {
            get => (string)GetValue(LoadingTextProperty);
            set => SetValue(LoadingTextProperty, value);
        }
    }
}
