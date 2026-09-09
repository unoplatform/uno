using Microsoft.UI.Xaml.Controls.Primitives;
using Uno;
using Uno.UI.Helpers;

namespace Microsoft.UI.Xaml.Media.Animation;

[NotImplemented("__ANDROID__", "__IOS__", "IS_UNIT_TESTS", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
internal class MenuPopupThemeTransition : PopupThemeTransition
{
	public double OpenedLength
	{
		get => (double)GetValue(OpenedLengthProperty);
		set => SetValue(OpenedLengthProperty, Boxes.Box(value));
	}

	public static DependencyProperty OpenedLengthProperty { get; } =
		DependencyProperty.Register(nameof(OpenedLength), typeof(double), typeof(MenuPopupThemeTransition), new FrameworkPropertyMetadata(Boxes.DoubleBoxes.Zero));

	public double ClosedRatio
	{
		get => (double)GetValue(ClosedRatioProperty);
		set => SetValue(ClosedRatioProperty, Boxes.Box(value));
	}

	public static DependencyProperty ClosedRatioProperty { get; } =
		DependencyProperty.Register(nameof(ClosedRatio), typeof(double), typeof(MenuPopupThemeTransition), new FrameworkPropertyMetadata(Boxes.DoubleBoxes.Zero));

	public AnimationDirection Direction
	{
		get => (AnimationDirection)GetValue(DirectionProperty);
		set => SetValue(DirectionProperty, value);
	}

	public static readonly DependencyProperty DirectionProperty =
		DependencyProperty.Register(nameof(Direction), typeof(AnimationDirection), typeof(MenuPopupThemeTransition), new FrameworkPropertyMetadata(AnimationDirection.Left));
}
