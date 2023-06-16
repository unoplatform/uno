namespace Uno;

partial class WinRTFeatureConfiguration
{
	public static class Focus
	{
#if __IOS__ || __ANDROID__
		/// <summary>
		/// This value only has effect applies on iOS, keybopard focus is now always enabled on Android.
		/// </summary>
		public static bool EnableExperimentalKeyboardFocus { get; set; }
#endif
	}
}
