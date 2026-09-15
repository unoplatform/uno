#nullable enable

using Windows.Foundation;
using Windows.UI.Core;

namespace Windows.UI.Core;

internal interface IUnoKeyboardInputSource
{
	event TypedEventHandler<object, KeyEventArgs>? KeyDown;
	event TypedEventHandler<object, KeyEventArgs>? KeyUp;

	/// <summary>
	/// Raised for a composed character that cannot be delivered through a key press,
	/// e.g. a Windows Alt+numpad code composed when Alt is released. Characters produced
	/// by a key press are delivered through <see cref="KeyDown"/> instead.
	/// </summary>
	event TypedEventHandler<object, CharacterReceivedEventArgs>? CharacterReceived;
}
