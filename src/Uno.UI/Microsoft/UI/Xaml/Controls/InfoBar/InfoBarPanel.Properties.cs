// MUX reference InfoBarPanel.properties.cpp, commit 533c6b1

using Windows.UI.Xaml;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class InfoBarPanel
	{
		public static Thickness GetHorizontalOrientationMargin(DependencyObject obj) => (Thickness)obj.GetValue(HorizontalOrientationMarginProperty);

		public static void SetHorizontalOrientationMargin(DependencyObject obj, Thickness value) => obj.SetValue(HorizontalOrientationMarginProperty, value);

		public static DependencyProperty HorizontalOrientationMarginProperty { get; } =
			DependencyProperty.RegisterAttached("HorizontalOrientationMargin", typeof(Thickness), typeof(InfoBarPanel), new PropertyMetadata(default(Thickness)));

		public Thickness HorizontalOrientationPadding
		{
			get => (Thickness)GetValue(HorizontalOrientationPaddingProperty);
			set => SetValue(HorizontalOrientationPaddingProperty, value);
		}

		public static DependencyProperty HorizontalOrientationPaddingProperty { get; } =
			DependencyProperty.Register(nameof(HorizontalOrientationPadding), typeof(Thickness), typeof(InfoBarPanel), new PropertyMetadata(default(Thickness)));

		public static Thickness GetVerticalOrientationMargin(DependencyObject obj) => (Thickness)obj.GetValue(VerticalOrientationMarginProperty);

		public static void SetVerticalOrientationMargin(DependencyObject obj, Thickness value) => obj.SetValue(VerticalOrientationMarginProperty, value);

		public static DependencyProperty VerticalOrientationMarginProperty { get; } =
			DependencyProperty.RegisterAttached("VerticalOrientationMargin", typeof(Thickness), typeof(InfoBarPanel), new PropertyMetadata(default(Thickness)));

		public Thickness VerticalOrientationPadding
		{
			get => (Thickness)GetValue(VerticalOrientationPaddingProperty);
			set => SetValue(VerticalOrientationPaddingProperty, value);
		}

		public static DependencyProperty VerticalOrientationPaddingProperty { get; } =
			DependencyProperty.Register(nameof(VerticalOrientationPadding), typeof(Thickness), typeof(InfoBarPanel), new PropertyMetadata(default(Thickness)));
	}
}
