#nullable enable

using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Uno.Disposables;

namespace Uno.UI.Xaml.Controls;

/// <summary>
/// Provides properties required for border rendering.
/// </summary>
internal partial interface IBorderInfoProvider
{
	/// <summary>
	/// Gets the background brush.
	/// </summary>
	Brush? Background { get; }

	/// <summary>
	/// Gets the background sizing.
	/// </summary>
	BackgroundSizing BackgroundSizing { get; }

	/// <summary>
	/// Gets the border brush.
	/// </summary>
	Brush? BorderBrush { get; }

	/// <summary>
	/// Gets the border thickness.
	/// </summary>
	Thickness BorderThickness { get; }

	/// <summary>
	/// Gets the corner radius.
	/// </summary>
	CornerRadius CornerRadius { get; }

#if UNO_HAS_BORDER_VISUAL
	BorderVisual BorderVisual { get; }

	SerialDisposable BorderBrushSubscriptionDisposable { get; set; }
	SerialDisposable BackgroundBrushSubscriptionDisposable { get; set; }
#endif
}
