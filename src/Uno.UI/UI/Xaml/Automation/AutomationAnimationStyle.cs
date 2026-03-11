namespace Microsoft.UI.Xaml.Automation;

/// <summary>
/// Contains values that specify the style of text animation.
/// </summary>
public enum AutomationAnimationStyle
{
	/// <summary>
	/// No animation is applied.
	/// </summary>
	None = 0,

	/// <summary>
	/// Text is displayed with Las Vegas Lights animation.
	/// </summary>
	LasVegasLights = 1,

	/// <summary>
	/// Text has a blinking background animation.
	/// </summary>
	BlinkingBackground = 2,

	/// <summary>
	/// Text is displayed with a sparkle effect.
	/// </summary>
	SparkleText = 3,

	/// <summary>
	/// Text is outlined by a marching black ants effect.
	/// </summary>
	MarchingBlackAnts = 4,

	/// <summary>
	/// Text is outlined by a marching red ants effect.
	/// </summary>
	MarchingRedAnts = 5,

	/// <summary>
	/// Text is displayed with a shimmer effect.
	/// </summary>
	Shimmer = 6,

	/// <summary>
	/// An animation style not covered by the other values.
	/// </summary>
	Other = 7,
}
