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
            IsErrorMessageDisplayed = false;
        }

        public bool IsErrorMessageDisplayed
        {
            get => _isErrorMessageDisplayed;
            set
            {
                if (_isErrorMessageDisplayed != value)
                {
                    _isErrorMessageDisplayed = value;
                    NotifyPropertyChanged();
                }
            }
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

        private bool _isErrorMessageDisplayed;
        private bool _isBlocking;

        public void NotifyPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            OnPropertyChanged(propertyName);
        }
    }
}
