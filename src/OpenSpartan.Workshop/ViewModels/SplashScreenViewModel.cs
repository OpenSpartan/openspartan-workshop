using OpenSpartan.Workshop.Core;
using System.Runtime.CompilerServices;

namespace OpenSpartan.Workshop.ViewModels
{
    internal sealed class SplashScreenViewModel : Observable
    {
        public static SplashScreenViewModel Instance { get; } = new SplashScreenViewModel();

        private SplashScreenViewModel()
        { 
            IsBlocking = true;
        }

        public bool IsBlocking
        {
            get => _isBlocking;
            set
            {
                if (_isBlocking != value)
                {
                    _isBlocking = value;
                    NotifyPropertyChanged();
                }
            }
        }

        private bool _isBlocking;

        public void NotifyPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            OnPropertyChanged(propertyName);
        }
    }
}
