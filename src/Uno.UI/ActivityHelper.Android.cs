using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Content.PM;

namespace Uno.UI
{
	public static class ActivityHelper
	{
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
#if __ANDROID_17__
			ConfigChanges.LayoutDirection |
#endif
			ConfigChanges.UiMode;
	}
}
