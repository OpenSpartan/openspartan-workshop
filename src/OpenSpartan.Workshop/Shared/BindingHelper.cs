using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using OpenSpartan.Workshop.Converters;

namespace OpenSpartan.Workshop.Shared
{
    public class BindingHelper
    {
        public static readonly DependencyProperty CellForegroundBindingPathProperty =
            DependencyProperty.RegisterAttached("CellForegroundBindingPath", typeof(string), typeof(BindingHelper), new PropertyMetadata(null, CellBindingPathPropertyChanged));

        public static readonly DependencyProperty CellBackgroundBindingPathProperty =
            DependencyProperty.RegisterAttached("CellBackgroundBindingPath", typeof(string), typeof(BindingHelper), new PropertyMetadata(null, CellBindingPathPropertyChanged));

        public static string GetCellForegroundBindingPath(DependencyObject obj)
        {
            return (string)obj.GetValue(CellForegroundBindingPathProperty);
        }

        public static void SetCellForegroundBindingPath(DependencyObject obj, string value)
        {
            obj.SetValue(CellForegroundBindingPathProperty, value);
        }

        public static string GetCellBackgroundBindingPath(DependencyObject obj)
        {
            return (string)obj.GetValue(CellBackgroundBindingPathProperty);
        }

        public static void SetCellBackgroundBindingPath(DependencyObject obj, string value)
        {
            obj.SetValue(CellBackgroundBindingPathProperty, value);
        }

        private static void CellBindingPathPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            string propertyPath = e.NewValue as string;

            if (propertyPath != null)
            {
                IValueConverter converter;

                DependencyProperty cellProperty;

                if (e.Property == CellForegroundBindingPathProperty)
                {
                    cellProperty = Control.ForegroundProperty;
                    converter = new OutcomeToForegroundConverter();
                }
                else
                {
                    cellProperty = Control.BackgroundProperty;
                    converter = new OutcomeToBackgroundConverter();
                }

                BindingOperations.SetBinding(
                    obj,
                    cellProperty,
                    new Binding { Path = new PropertyPath(propertyPath), Converter = converter });
            }
        }
    }
}
