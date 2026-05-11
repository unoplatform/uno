using Android.Views;
using Microsoft.UI.Input;

namespace Uno.WinUI.Runtime.Skia.Android;

internal sealed class AndroidGestureRecognizerExtension : IGestureRecognizerExtension
{
	public static AndroidGestureRecognizerExtension Instance { get; } = new();

	// Android's ViewConfiguration.getDoubleTapTimeout() is a system constant (300 ms by default).
	public ulong? MultiTapMaxDelayMicroseconds => (ulong)ViewConfiguration.DoubleTapTimeout * 1000UL;
}
