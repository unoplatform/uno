using Microsoft.UI.Input;
using Windows.Win32;

namespace Uno.UI.Runtime.Win32;

internal sealed class Win32GestureRecognizerExtension : IGestureRecognizerExtension
{
	public static Win32GestureRecognizerExtension Instance { get; } = new();

	public ulong? MultiTapMaxDelayMicroseconds => PInvoke.GetDoubleClickTime() * 1000UL;
}
