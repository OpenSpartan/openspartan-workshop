using Den.Dev.Orion.Models.HaloInfinite;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using OpenSpartan.Models;
using OpenSpartan.Shared;
using OpenSpartan.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;

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
