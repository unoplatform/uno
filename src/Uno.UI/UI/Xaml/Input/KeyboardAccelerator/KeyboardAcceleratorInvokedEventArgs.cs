namespace Windows.UI.Xaml.Input;

/// <summary>
/// Provides event data for the Invoked event.
/// </summary>
public partial class KeyboardAcceleratorInvokedEventArgs
{
	internal KeyboardAcceleratorInvokedEventArgs(DependencyObject element, KeyboardAccelerator keyboardAccelerator)
	{
		Element = element;
		KeyboardAccelerator = keyboardAccelerator;
	}

	/// <summary>
	/// Gets the object associated with the keyboard shortcut (or accelerator).
	/// </summary>
	public DependencyObject Element { get; }

	/// <summary>
	/// Gets the keyboard shortcut (or accelerator) object associated with the Invoked event.
	/// </summary>
	public KeyboardAccelerator KeyboardAccelerator { get; }

	/// <summary>
	/// Gets or sets a value that marks the event as handled.
	/// </summary>
	public bool Handled { get; set; }
}
