using Windows.UI.Xaml.Controls.Primitives;

#if __IOS__
using Foundation;
using UIKit;
#endif

namespace Windows.UI.Xaml.Controls;

/// <summary>
/// Represents a control that lets the user select from a range of values by moving a Thumb control along a track.
/// </summary>
public partial class Slider : RangeBase
{
	/// <summary>
	/// Enables or disables tracking on the Slider container. <para />
	/// 
	/// When enabled, the Slider will intercept touch events on the entire container as well as the Thumb.
	/// This is the default value. <para />
	/// 
	/// When disabled, only the Thumb will intercept touch events. Therefore, the user cannot tap or drag
	/// on the bar to change the Slider's value. This is a better option in cases involving Sliders within
	/// a ScrollView, to avoid the Slider stealing the focus when the user tries to scroll.
	/// </summary>
	[Uno.UnoOnly]
	public bool IsTrackerEnabled
	{
		get => (bool)GetValue(IsTrackerEnabledProperty);
		set => SetValue(IsTrackerEnabledProperty, value);
	}

	/// <summary>
	/// Identifies the IsTrackerEnabled property.
	/// </summary>
	public static DependencyProperty IsTrackerEnabledProperty { get; } =
		DependencyProperty.Register(
			nameof(IsTrackerEnabled),
			typeof(bool),
			typeof(Slider),
			new FrameworkPropertyMetadata(true));
}

