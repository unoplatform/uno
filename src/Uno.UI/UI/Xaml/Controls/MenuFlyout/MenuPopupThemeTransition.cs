using Microsoft.UI.Xaml.Controls.Primitives;
using Uno;

namespace Microsoft.UI.Xaml.Media.Animation;

[NotImplemented("__ANDROID__", "__IOS__", "IS_UNIT_TESTS", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
internal class MenuPopupThemeTransition : PopupThemeTransition
{
	public double OpenedLength
	{
		get => (double)GetValue(OpenedLengthProperty);
		set => SetValue(OpenedLengthProperty, value);
	}

	public static DependencyProperty OpenedLengthProperty =
		DependencyProperty.Register(nameof(OpenedLength), typeof(double), typeof(MenuPopupThemeTransition), new FrameworkPropertyMetadata(0.0));

	public double ClosedRatio
	{
		get => (double)GetValue(ClosedRatioProperty);
		set => SetValue(ClosedRatioProperty, value);
	}

	public static DependencyProperty ClosedRatioProperty { get; } =
		DependencyProperty.Register(nameof(ClosedRatio), typeof(double), typeof(MenuPopupThemeTransition), new FrameworkPropertyMetadata(0.0));

	public AnimationDirection Direction
	{
		get => (AnimationDirection)GetValue(DirectionProperty);
		set => SetValue(DirectionProperty, value);
	}

	public static readonly DependencyProperty DirectionProperty =
		DependencyProperty.Register(nameof(Direction), typeof(AnimationDirection), typeof(MenuPopupThemeTransition), new FrameworkPropertyMetadata(AnimationDirection.Left));
}
