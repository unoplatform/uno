using Windows.Foundation;
using Windows.System;
using Windows.UI.Core;

namespace Uno.UI.Runtime.AppleUIKit;

/// <summary>
/// One instance per window/scene, owned by its <see cref="Uno.UI.Runtime.AppleUIKit.RootViewController"/>.
/// </summary>
internal sealed class UnoKeyboardInputSource : IUnoKeyboardInputSource
{
	public event TypedEventHandler<object, KeyEventArgs>? KeyDown;
	public event TypedEventHandler<object, KeyEventArgs>? KeyUp;
	event TypedEventHandler<object, CharacterReceivedEventArgs>? IUnoKeyboardInputSource.CharacterReceived { add { } remove { } }

	/// <returns>Whether the app handled the key.</returns>
	internal bool RaiseKey(VirtualKey key, bool isDown)
	{
		var args = new KeyEventArgs("keyboard", key, VirtualKeyModifiers.None, default(CorePhysicalKeyStatus));

		(isDown ? KeyDown : KeyUp)?.Invoke(this, args);

		return args.Handled;
	}
}
