using Windows.System;

namespace Windows.UI.Xaml.Input;

/// <summary>
/// Provides event data for the ProcessKeyboardAccelerators event.
/// </summary>
public partial class ProcessKeyboardAcceleratorEventArgs
{
	internal ProcessKeyboardAcceleratorEventArgs(VirtualKey key, VirtualKeyModifiers modifiers)
	{
		Key = key;
		Modifiers = modifiers;
	}

	/// <summary>
	/// Gets or sets a value that marks the event as handled.
	/// </summary>
	public bool Handled { get; set; }

	/// <summary>
	/// Gets the virtual key (used in conjunction with one or more modifier keys) for a keyboard shortcut (accelerator).
	/// A keyboard shortcut is invoked when the modifier keys asssociated with the shortcut are pressed and then the non-modifier
	/// key is pressed at the same time.For example, Ctrl+C for copy and Ctrl+S for save.
	/// </summary>
	public VirtualKey Key { get; }

	/// <summary>
	/// Gets the virtual key used to modify another keypress for a keyboard shortcut (accelerator).
	/// A keyboard shortcut is invoked when the modifier keys asssociated with the shortcut are pressed and then 
	/// the non-modifier key is pressed at the same time.For example, Ctrl+C for copy and Ctrl+S for save.
	/// </summary>
	public VirtualKeyModifiers Modifiers { get; }

	internal bool HandledShouldNotImpedeTextInput { get; set; }
}
