using Windows.Foundation;
using Windows.UI.Xaml;

namespace Windows.UI.Xaml.Controls.Primitives
{
	public partial class AppBarTemplateSettings : DependencyObject
	{
		internal AppBarTemplateSettings()
		{

		}
		public Rect ClipRect
		{
			get => (Rect)GetValue(ClipRectProperty);
			internal set => SetValue(ClipRectProperty, value);
		}

		internal static DependencyProperty ClipRectProperty { get; } =
			DependencyProperty.Register(nameof(ClipRect), typeof(Rect), typeof(AppBarTemplateSettings), new FrameworkPropertyMetadata(new Rect()));

		public Thickness CompactRootMargin
		{
			get => (Thickness)GetValue(CompactRootMarginProperty);
			internal set => SetValue(CompactRootMarginProperty, value);
		}

		internal static DependencyProperty CompactRootMarginProperty { get; } =
			DependencyProperty.Register(nameof(CompactRootMargin), typeof(Thickness), typeof(AppBarTemplateSettings), new FrameworkPropertyMetadata(new Thickness(0)));

		public double CompactVerticalDelta
		{
			get => (double)GetValue(CompactVerticalDeltaProperty);
			internal set => SetValue(CompactVerticalDeltaProperty, value);
		}

		internal static DependencyProperty CompactVerticalDeltaProperty { get; } =
			DependencyProperty.Register(nameof(CompactVerticalDelta), typeof(double), typeof(AppBarTemplateSettings), new FrameworkPropertyMetadata(0.0));

		public Thickness HiddenRootMargin
		{
			get => (Thickness)GetValue(HiddenRootMarginProperty);
			internal set => SetValue(HiddenRootMarginProperty, value);
		}

		public static DependencyProperty HiddenRootMarginProperty { get; } =
			DependencyProperty.Register(nameof(HiddenRootMargin), typeof(Thickness), typeof(AppBarTemplateSettings), new FrameworkPropertyMetadata(new Thickness(0)));

		public double HiddenVerticalDelta
		{
			get => (double)GetValue(HiddenVerticalDeltaProperty);
			internal set => SetValue(HiddenVerticalDeltaProperty, value);
		}

		internal static DependencyProperty HiddenVerticalDeltaProperty { get; } =
			DependencyProperty.Register(nameof(HiddenVerticalDelta), typeof(double), typeof(AppBarTemplateSettings), new FrameworkPropertyMetadata(0.0));

		public Thickness MinimalRootMargin
		{
			get => (Thickness)GetValue(MinimalRootMarginProperty);
			internal set => SetValue(MinimalRootMarginProperty, value);
		}

		internal static DependencyProperty MinimalRootMarginProperty { get; } =
			DependencyProperty.Register(nameof(MinimalRootMargin), typeof(Thickness), typeof(AppBarTemplateSettings), new FrameworkPropertyMetadata(new Thickness(0)));

		public double MinimalVerticalDelta
		{
			get => (double)GetValue(MinimalVerticalDeltaProperty);
			internal set => SetValue(MinimalVerticalDeltaProperty, value);
		}

		internal static DependencyProperty MinimalVerticalDeltaProperty { get; } =
			DependencyProperty.Register(nameof(MinimalVerticalDelta), typeof(double), typeof(AppBarTemplateSettings), new FrameworkPropertyMetadata(0.0));

		public double NegativeCompactVerticalDelta
		{
			get => (double)GetValue(NegativeCompactVerticalDeltaProperty);
			internal set => SetValue(NegativeCompactVerticalDeltaProperty, value);
		}

		internal static DependencyProperty NegativeCompactVerticalDeltaProperty { get; } =
			DependencyProperty.Register(nameof(NegativeCompactVerticalDelta), typeof(double), typeof(AppBarTemplateSettings), new FrameworkPropertyMetadata(0.0));

		public double NegativeMinimalVerticalDelta
		{
			get => (double)GetValue(NegativeMinimalVerticalDeltaProperty);
			internal set => SetValue(NegativeMinimalVerticalDeltaProperty, value);
		}

		internal static DependencyProperty NegativeMinimalVerticalDeltaProperty { get; } =
			DependencyProperty.Register(nameof(NegativeMinimalVerticalDelta), typeof(double), typeof(AppBarTemplateSettings), new FrameworkPropertyMetadata(0.0));

		public double NegativeHiddenVerticalDelta
		{
			get => (double)GetValue(NegativeHiddenVerticalDeltaProperty);
			internal set => SetValue(NegativeHiddenVerticalDeltaProperty, value);
		}

		internal static DependencyProperty NegativeHiddenVerticalDeltaProperty { get; } =
			DependencyProperty.Register(nameof(NegativeHiddenVerticalDelta), typeof(double), typeof(AppBarTemplateSettings), new FrameworkPropertyMetadata(0.0));
	}
}
