using Uno.Foundation.Logging;
using Windows.Foundation;

namespace Windows.UI.Core;

public partial class CoreWindow : ICoreWindowEvents
{
	public event TypedEventHandler<CoreWindow, KeyEventArgs> KeyDown;

	public event TypedEventHandler<CoreWindow, KeyEventArgs> KeyUp;

	void ICoreWindowEvents.RaiseKeyDown(KeyEventArgs eventArgs) =>
		KeyDown?.Invoke(this, eventArgs);

	void ICoreWindowEvents.RaiseKeyUp(KeyEventArgs eventArgs) =>
		KeyUp?.Invoke(this, eventArgs);


	internal void SetKeyboardInputSource(IUnoKeyboardInputSource source)
	{
		this.Log().Error($"SetKeyboardInputSource is not implemented");
	}

	internal void SetPointerInputSource(IUnoCorePointerInputSource source)
	{
		this.Log().Error($"SetPointerInputSource is not implemented");
	}
}
