namespace Windows.UI.Xaml.Automation.Provider;

/// <summary>
/// Exposes methods and properties to support access by a Microsoft UI Automation
/// client to controls or elements that can be moved, resized, or rotated within 
/// a two-dimensional space. Implement this interface in order to support the 
/// capabilities that an automation client requests with a GetPattern call and 
/// PatternInterface.Transform.
/// </summary>
public partial interface ITransformProvider
{
	/// <summary>
	/// Gets a value that indicates whether the element can be moved.
	/// </summary>
	bool CanMove { get; }

	/// <summary>
	/// Gets a value that indicates whether the element can be resized.
	/// </summary>
	bool CanResize { get; }

	/// <summary>
	/// Gets a value that indicates whether the element can be rotated.
	/// </summary>
	bool CanRotate { get; }

	/// <summary>
	/// Moves the control.
	/// </summary>
	/// <param name="x">The absolute screen coordinates of the left side of the control.</param>
	/// <param name="y">The absolute screen coordinates of the top of the control.</param>
	void Move(double x, double y);

	/// <summary>
	/// Resizes the control.
	/// </summary>
	/// <param name="width">The new width of the window, in pixels.</param>
	/// <param name="height">The new height of the window, in pixels.</param>
	void Resize(double width, double height);

	/// <summary>
	/// Rotates the control.
	/// </summary>
	/// <param name="degrees">The number of degrees to rotate the control. A positive number 
	/// rotates the control clockwise. A negative number rotates the control counterclockwise.</param>
	void Rotate(double degrees);
}
