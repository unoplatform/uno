#if __ANDROID__
using Android.Views;
using Microsoft.UI.Input;

namespace Uno.UI.Input;

internal sealed class AndroidGestureRecognizerExtension : IGestureRecognizerExtension
{
	public static AndroidGestureRecognizerExtension Instance { get; } = new();

	// ViewConfiguration.getDoubleTapTimeout() is a system constant (300 ms by default).
	public ulong? MultiTapMaxDelayMicroseconds => (ulong)ViewConfiguration.DoubleTapTimeout * 1000UL;
}
#endif
