#nullable enable

using Windows.Foundation;
using Windows.UI.Core;

namespace Windows.UI.Core;

internal interface IUnoKeyboardInputSource
{
	event TypedEventHandler<object, KeyEventArgs>? KeyDown;
	event TypedEventHandler<object, KeyEventArgs>? KeyUp;
}
