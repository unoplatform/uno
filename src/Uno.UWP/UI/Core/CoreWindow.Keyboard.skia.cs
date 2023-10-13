#nullable enable

namespace Windows.UI.Core;

public partial class CoreWindow
{
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
