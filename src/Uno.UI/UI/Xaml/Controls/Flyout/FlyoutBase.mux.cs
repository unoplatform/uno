using Windows.UI.Xaml.Input;

namespace Windows.UI.Xaml.Controls.Primitives;

public partial class FlyoutBase
{
	/// <summary>
	/// Attempts to invoke a keyboard shortcut (accelerator).
	/// </summary>
	/// <param name="args">The ProcessKeyboardAcceleratorEventArgs.</param>
	public void TryInvokeKeyboardAccelerator(ProcessKeyboardAcceleratorEventArgs args) => OnProcessKeyboardAccelerators(args);

	/// <summary>
	/// Called just before a keyboard shortcut (accelerator) is processed in your app. Invoked whenever application code or internal 
	/// processes call ProcessKeyboardAccelerators. Override this method to influence the default accelerator handling.
	/// </summary>
	/// <param name="args">The ProcessKeyboardAcceleratorEventArgs.</param>
	protected virtual void OnProcessKeyboardAccelerators(ProcessKeyboardAcceleratorEventArgs args)
	{
	}
}
