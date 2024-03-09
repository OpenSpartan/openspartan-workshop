using Microsoft.UI.Xaml;
using System;

namespace OpenSpartan.Workshop.Controls
{
    public class AutoClosingTeachingTip : Microsoft.UI.Xaml.Controls.TeachingTip
    {
        private DispatcherTimer _timer;
        private long _token;

        public AutoClosingTeachingTip() : base()
        {
            this.Loaded += AutoCloseTeachingTip_Loaded;
            this.Unloaded += AutoCloseTeachingTip_Unloaded;
        }

        public int AutoCloseInterval { get; set; } = 5000;

        private void AutoCloseTeachingTip_Loaded(object sender, RoutedEventArgs e)
        {
            _token = this.RegisterPropertyChangedCallback(IsOpenProperty, IsOpenChanged);
            if (IsOpen)
            {
                Open();
            }
        }

        private void AutoCloseTeachingTip_Unloaded(object sender, RoutedEventArgs e)
        {
            this.UnregisterPropertyChangedCallback(IsOpenProperty, _token);
        }

        private void IsOpenChanged(DependencyObject dependencyObject, DependencyProperty dependencyProperty)
        {
            var tip = dependencyObject as AutoClosingTeachingTip;
            if (tip == null)
            {
                return;
            }

            if (dependencyProperty != IsOpenProperty)
            {
                return;
            }

            if (tip.IsOpen)
            {
                tip.Open();
            }
            else
            {
                tip.Close();
            }
        }

        private void Open()
        {
            _timer = new DispatcherTimer();
            _timer.Tick += Timer_Tick;
            _timer.Interval = TimeSpan.FromMilliseconds(AutoCloseInterval);
            _timer.Start();
        }

        private void Close()
        {
            if (_timer == null)
            {
                return;
            }

            _timer.Stop();
            _timer.Tick -= Timer_Tick;
        }

        private void Timer_Tick(object sender, object e)
        {
            this.IsOpen = false;
        }
    }
}
