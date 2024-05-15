using Uno;
using Windows.Foundation;

namespace Windows.ApplicationModel.Activation;

/// <summary>
/// Provides a dismissal event and image location information for the app's splash screen.
/// </summary>
/// <remarks>This API is currently not connected to platform-specific splash screens.</remarks>
public sealed partial class SplashScreen
{
	internal SplashScreen()
	{
	}

#pragma warning disable CS0067 // The event 'SplashScreen.Dismissed' is never used
	/// <summary>
	/// Fires when the app's splash screen is dismissed.
	/// </summary>
	[NotImplemented("__ANDROID__", "__IOS__", "IS_UNIT_TESTS", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
	public event TypedEventHandler<SplashScreen, object> Dismissed;
#pragma warning restore CS0067 // The event 'SplashScreen.Dismissed' is never used

	/// <summary>
	/// The coordinates of the app's splash screen image relative to the window.
	/// </summary>
	[NotImplemented("__ANDROID__", "__IOS__", "IS_UNIT_TESTS", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
	public Rect ImageLocation { get; }
}
