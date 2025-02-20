using Android.App;
using Android.Widget;
using Android.OS;
using Android.Content.PM;
using Android.Views;

namespace UnoAppWinUI
{
	[Activity(
			MainLauncher = true,
			ConfigurationChanges = global::Uno.UI.ActivityHelper.AllConfigChanges,
			WindowSoftInputMode = SoftInput.AdjustPan | SoftInput.StateHidden
		)]
	public class MainActivity : Microsoft.UI.Xaml.ApplicationActivity
	{
	}
}

