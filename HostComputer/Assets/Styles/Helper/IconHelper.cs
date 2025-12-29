using System.Windows;

namespace HostComputer.Assets.Styles.Helper
{
    public static class IconHelper
    {
        public static readonly DependencyProperty IconFontSizeProperty =
            DependencyProperty.RegisterAttached(
                "IconFontSize",
                typeof(double),
                typeof(IconHelper),
                new FrameworkPropertyMetadata(36d, FrameworkPropertyMetadataOptions.Inherits));

        public static void SetIconFontSize(DependencyObject obj, double value)
            => obj.SetValue(IconFontSizeProperty, value);

        public static double GetIconFontSize(DependencyObject obj)
            => (double)obj.GetValue(IconFontSizeProperty);
    }
}