namespace Windows.UI.Xaml.Automation.Provider;

/// <summary>
/// Exposes methods and properties to support access by a Microsoft UI Automation
/// client to controls that provide fundamental window-based functionality within 
/// a traditional graphical user interface (GUI). Implement this interface in order
/// to support the capabilities that an automation client requests with a GetPattern 
/// call and PatternInterface.Window.
/// </summary>
partial interface IWindowProvider
{
	/// <summary>
	/// Gets the interaction state of the window.
	/// </summary>
	WindowInteractionState InteractionState { get; }

	/// <summary>
	/// Gets a value that specifies whether the window is modal.
	/// </summary>
	bool IsModal { get; }

	/// <summary>
	/// Gets a value that specifies whether the window is the topmost
	/// element in the z-order of layout.
	/// </summary>
	bool IsTopmost { get; }

	/// <summary>
	/// Gets a value that specifies whether the window can be maximized.
	/// </summary>
	bool Maximizable { get; }

	/// <summary>
	/// Gets a value that specifies whether the window can be minimized.
	/// </summary>
	bool Minimizable { get; }

	/// <summary>
	/// Gets the visual state of the window.
	/// </summary>
	WindowVisualState VisualState { get; }

	/// <summary>
	/// Closes the window.
	/// </summary>
	void Close();

	/// <summary>
	/// Changes the visual state of the window (such as minimizing or maximizing it).
	/// </summary>
	/// <param name="state">The visual state of the window to change to, as a value 
	/// of the enumeration.</param>
	void SetVisualState(WindowVisualState state);

	/// <summary>
	/// Blocks the calling code for the specified time or until the associated
	/// process enters an idle state, whichever completes first.
	/// </summary>
	/// <param name="milliseconds">The amount of time, in milliseconds, to wait for
	/// the associated process to become idle.</param>
	/// <returns>true if the window has entered the idle state; false if the timeout
	/// occurred.</returns>
	bool WaitForInputIdle(int milliseconds);
}
