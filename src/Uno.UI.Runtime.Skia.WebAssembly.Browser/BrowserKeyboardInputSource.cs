using Windows.Foundation;
using Windows.UI.Core;

namespace Uno.UI.Runtime.Skia;

internal class BrowserKeyboardInputSource : IUnoKeyboardInputSource
{
#pragma warning disable CS0067
	public event TypedEventHandler<object, KeyEventArgs>? KeyDown;
	public event TypedEventHandler<object, KeyEventArgs>? KeyUp;
#pragma warning restore CS0067
}
