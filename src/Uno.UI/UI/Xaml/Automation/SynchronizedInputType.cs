namespace Windows.UI.Xaml.Automation;

/// <summary>
/// Provides a of set constants that identify, as used by the ISynchronizedInputProvider Microsoft UI Automation interface.
/// </summary>
public enum SynchronizedInputType
{
	/// <summary>
	/// A key has been released.
	/// </summary>
	KeyUp = 1,

	/// <summary>
	/// A key has been pressed.
	/// </summary>
	KeyDown = 2,

	/// <summary>
	/// The left mouse button has been released.
	/// </summary>
	LeftMouseUp = 4,

	/// <summary>
	/// The left mouse button has been pressed.
	/// </summary>
	LeftMouseDown = 8,

	/// <summary>
	/// The right mouse button has been released.
	/// </summary>
	RightMouseUp = 16,

	/// <summary>
	/// The right mouse button has been pressed.
	/// </summary>
	RightMouseDown = 32,
}
