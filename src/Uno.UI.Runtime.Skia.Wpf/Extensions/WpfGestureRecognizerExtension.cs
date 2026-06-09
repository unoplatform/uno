using System.Runtime.InteropServices;
using Microsoft.UI.Input;

namespace Uno.UI.Runtime.Skia.Wpf.Extensions;

internal sealed class WpfGestureRecognizerExtension : IGestureRecognizerExtension
{
	public static WpfGestureRecognizerExtension Instance { get; } = new();

	public ulong? MultiTapMaxDelayMicroseconds => GetDoubleClickTime() * 1000UL;

	[DllImport("user32.dll", ExactSpelling = true)]
	private static extern uint GetDoubleClickTime();
}
