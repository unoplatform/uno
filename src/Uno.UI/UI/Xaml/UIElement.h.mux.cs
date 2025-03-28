using Windows.UI.Xaml.Input;

namespace Windows.UI.Xaml;

partial class UIElement
{
	/// <summary>
	/// Called when a keyboard shortcut (or accelerator) is processed in your app.
	/// Override this method to handle how your app responds when a keyboard accelerator is invoked.
	/// </summary>
	/// <param name="args">The KeyboardAcceleratorInvokedEventArgs.</param>
	protected virtual void OnKeyboardAcceleratorInvoked(KeyboardAcceleratorInvokedEventArgs args)
	{
	}
}
