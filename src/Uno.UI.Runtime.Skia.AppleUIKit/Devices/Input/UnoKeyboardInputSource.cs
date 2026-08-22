using Windows.Foundation;
using Windows.UI.Core;

namespace Uno.UI.Runtime.Skia.AppleUIKit;

internal sealed class UnoKeyboardInputSource : IUnoKeyboardInputSource
{
	public static UnoKeyboardInputSource Instance { get; } = new();

	private UnoKeyboardInputSource()
	{
	}
#pragma warning disable CS0067
	public event TypedEventHandler<object, KeyEventArgs>? KeyDown;
	public event TypedEventHandler<object, KeyEventArgs>? KeyUp;
#pragma warning restore CS0067
	event TypedEventHandler<object, CharacterReceivedEventArgs>? IUnoKeyboardInputSource.CharacterReceived { add { } remove { } }
}
