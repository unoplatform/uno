namespace Windows.UI.Xaml.Controls.Primitives;

/// <summary>
/// Defines constants that specify the position of tick marks
/// in a Slider in relation to the track that the control implements.
/// </summary>
public enum TickPlacement
{
	/// <summary>
	/// No tick marks appear.
	/// </summary>
	None,

	/// <summary>
	/// Tick marks appear above the track for a horizontal Slider,
	/// or to the left of the track for a vertical Slider.
	/// </summary>
	TopLeft,

	/// <summary>
	/// Tick marks appear below the track for a horizontal Slider,
	/// or to the right of the track for a vertical Slider.
	/// </summary>
	BottomRight,

	/// <summary>
	/// Tick marks appear on both sides of either a horizontal or vertical track.
	/// </summary>
	Outside,

	/// <summary>
	/// Tick marks appear directly on the track.
	/// </summary>
	Inline
}
