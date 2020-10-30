using Windows.UI.Xaml;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class InfoBarPanel
	{
		public static Thickness GetHorizontalMargin(DependencyObject obj) => (Thickness)obj.GetValue(HorizontalMarginProperty);

		public static void SetHorizontalMargin(DependencyObject obj, Thickness value) => obj.SetValue(HorizontalMarginProperty, value);

		public static DependencyProperty HorizontalMarginProperty { get; } =
			DependencyProperty.RegisterAttached("HorizontalMargin", typeof(Thickness), typeof(InfoBarPanel), new PropertyMetadata(default(Thickness)));

		public static Thickness GetVerticalMargin(DependencyObject obj) => (Thickness)obj.GetValue(VerticalMarginProperty);

		public static void SetVerticalMargin(DependencyObject obj, Thickness value) => obj.SetValue(VerticalMarginProperty, value);

		public static DependencyProperty VerticalMarginProperty { get; } =
			DependencyProperty.RegisterAttached("VerticalMargin", typeof(Thickness), typeof(InfoBarPanel), new PropertyMetadata(default(Thickness)));
	}
}
