using Windows.Foundation;
using Windows.UI.Core;

namespace Uno.UI.Runtime.Skia.AppleUIKit;

/// <summary>
/// One instance per window/scene, owned by its <see cref="Uno.UI.Runtime.Skia.AppleUIKit.RootViewController"/>.
/// </summary>
internal sealed class UnoKeyboardInputSource : IUnoKeyboardInputSource
{
#pragma warning disable CS0067
	public event TypedEventHandler<object, KeyEventArgs>? KeyDown;
	public event TypedEventHandler<object, KeyEventArgs>? KeyUp;
#pragma warning restore CS0067
	event TypedEventHandler<object, CharacterReceivedEventArgs>? IUnoKeyboardInputSource.CharacterReceived { add { } remove { } }
}
