#nullable enable

using Windows.Foundation;

namespace Windows.UI.Core;

public partial class CoreWindow
#if __ANDROID__ || __IOS__
	: ICoreWindowEvents
#endif
{
	public event TypedEventHandler<CoreWindow, KeyEventArgs>? KeyDown;

	public event TypedEventHandler<CoreWindow, KeyEventArgs>? KeyUp;

#if __ANDROID__ || __IOS__
	void ICoreWindowEvents.RaiseKeyDown(KeyEventArgs eventArgs) =>
		KeyDown?.Invoke(this, eventArgs);

	void ICoreWindowEvents.RaiseKeyUp(KeyEventArgs eventArgs) =>
		KeyUp?.Invoke(this, eventArgs);
#endif

	private IUnoKeyboardInputSource? _keyboardSource;

	internal void SetKeyboardInputSource(IUnoKeyboardInputSource source)
	{
		if (_keyboardSource is not null)
		{
			return;
		}

		_keyboardSource = source;
		_keyboardSource.KeyDown += (_, args) => KeyDown?.Invoke(this, args);
		_keyboardSource.KeyUp += (_, args) => KeyUp?.Invoke(this, args);
	}

	internal IUnoKeyboardInputSource? KeyboardSource => _keyboardSource;
}
