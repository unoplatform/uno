#nullable enable

#if IS_UNO_UI_PROJECT
namespace Microsoft.UI.Input;
#else
namespace Windows.UI.Input;
#endif

internal interface IGestureRecognizerExtension
{
	/// <summary>
	/// The platform-configured maximum delay (in microseconds) between two taps
	/// that should still be recognized as a multi-tap (double tap) gesture.
	/// Return null to fall back to the default value.
	/// </summary>
	ulong? MultiTapMaxDelayMicroseconds { get; }
}
