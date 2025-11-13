using Windows.Foundation;

namespace Microsoft.UI.Input;

/// <summary>
/// Provides data for pointer events that occur in a window's non-client area (for example the
/// caption, title bar buttons, or resize borders). Instances are provided by the framework when
/// processing non-client pointer input.
/// </summary>
public partial class NonClientPointerEventArgs
{
	/// <summary>
	/// Initializes a new instance of the <see cref="NonClientPointerEventArgs"/> class.
	/// The constructor is internal because instances are created by the framework.
	/// </summary>
	internal NonClientPointerEventArgs(
		bool isPointInRegion,
		Point point,
		PointerDeviceType pointerDeviceType,
		NonClientRegionKind regionKind)
	{
		IsPointInRegion = isPointInRegion;
		Point = point;
		PointerDeviceType = pointerDeviceType;
		RegionKind = regionKind;
	}

	/// <summary>
	/// Gets a value that indicates whether the pointer location is within the specified non-client region.
	/// </summary>
	public bool IsPointInRegion { get; }

	/// <summary>
	/// Gets the point, in window coordinates, where the pointer event occurred.
	/// </summary>
	public Point Point { get; }

	/// <summary>
	/// Gets the type of pointer device that generated the event (for example touch, pen, or mouse).
	/// </summary>
	public PointerDeviceType PointerDeviceType { get; }

	/// <summary>
	/// Gets the kind of non-client region that was hit by the pointer (for example Close, Caption, or RightBorder).
	/// </summary>
	public NonClientRegionKind RegionKind { get; }
}
