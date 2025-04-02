using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Content.PM;

namespace Uno.UI;

/// <summary>
/// Provides helpers for Android Activity.
/// </summary>
public static class ActivityHelper
{
	/// <summary>
	/// Represents all configuration changes handled by Uno Platform.
	/// </summary>
	public const ConfigChanges AllConfigChanges =
		ConfigChanges.Orientation |
		ConfigChanges.KeyboardHidden |
		ConfigChanges.FontScale |
		ConfigChanges.Keyboard |
		ConfigChanges.Locale |
		ConfigChanges.Mcc |
		ConfigChanges.Mnc |
		ConfigChanges.Navigation |
		ConfigChanges.ScreenLayout |
		ConfigChanges.ScreenSize |
		ConfigChanges.SmallestScreenSize |
		ConfigChanges.Touchscreen |
		ConfigChanges.LayoutDirection |
		ConfigChanges.UiMode;
}
