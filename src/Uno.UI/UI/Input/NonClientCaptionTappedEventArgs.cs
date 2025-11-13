using Windows.Foundation;

namespace Microsoft.UI.Input;

/// <summary>
/// Provides data for the event that occurs when the window caption (title bar) is tapped by a pointer.
/// Instances are provided by the framework when caption tap input is raised.
/// </summary>
public partial class NonClientCaptionTappedEventArgs
{
	/// <summary>
	/// Initializes a new instance of the <see cref="NonClientCaptionTappedEventArgs"/> class.
	/// The constructor is internal because instances are created by the framework.
	/// </summary>
	internal NonClientCaptionTappedEventArgs(Point point, PointerDeviceType pointerDeviceType)
	{
		Point = point;
		PointerDeviceType = pointerDeviceType;
	}

	/// <summary>
	/// Gets the point, in window coordinates, where the caption was tapped.
	/// </summary>
	public Point Point { get; }

	/// <summary>
	/// Gets the type of pointer device that generated the tap (for example touch, pen, or mouse).
	/// </summary>
	public PointerDeviceType PointerDeviceType { get; }
}
