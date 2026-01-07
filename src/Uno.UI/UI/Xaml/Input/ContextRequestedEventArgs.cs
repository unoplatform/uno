using Windows.Foundation;
using Uno.UI.Xaml.Input;

namespace Microsoft.UI.Xaml.Input;

/// <summary>
/// Provides event data for the ContextRequested event.
/// </summary>
public partial class ContextRequestedEventArgs : RoutedEventArgs, IHandleableRoutedEventArgs
{
	private Point _globalPoint = new Point(-1, -1);

	/// <summary>
	/// Initializes a new instance of the ContextRequestedEventArgs class.
	/// </summary>
	public ContextRequestedEventArgs()
	{
	}

	/// <summary>
	/// Gets or sets a value that marks the routed event as handled.
	/// A true value for Handled prevents most handlers along the event route from handling the same event again.
	/// </summary>
	public bool Handled { get; set; }

	bool IHandleableRoutedEventArgs.Handled
	{
		get => Handled;
		set => Handled = value;
	}

	/// <summary>
	/// Gets the x- and y-coordinates of the pointer position, optionally evaluated against a coordinate origin of a supplied UIElement.
	/// </summary>
	/// <param name="relativeTo">Any UIElement-derived object that is connected to the same object tree. To use the app window as the reference object, specify a null value.</param>
	/// <param name="point">A Point that represents the current x- and y-coordinates of the mouse pointer position. If null was passed as relativeTo, this coordinate is for the app window. If a relativeTo value other than null was passed, this coordinate is relative to the object referenced by relativeTo.</param>
	/// <returns>true if the context request was initiated by a pointer device; false if the request was initiated by a keyboard or a pen barrel button.</returns>
	public bool TryGetPosition(UIElement relativeTo, out Point point)
	{
		// WinUI: If point is (-1, -1), it means keyboard invocation (no position available)
		if (_globalPoint.X == -1 && _globalPoint.Y == -1)
		{
			point = default;
			return false;
		}

		if (relativeTo != null)
		{
			var transform = relativeTo.TransformToVisual(null);
			var inverse = transform.Inverse;
			if (inverse != null)
			{
				point = inverse.TransformPoint(_globalPoint);
				return true;
			}
		}

		point = _globalPoint;
		return true;
	}

	/// <summary>
	/// Sets the global point position. Used internally by ContextMenuProcessor.
	/// </summary>
	/// <param name="point">The global point, or (-1, -1) for keyboard invocation.</param>
	internal void SetGlobalPoint(Point point) => _globalPoint = point;
}
