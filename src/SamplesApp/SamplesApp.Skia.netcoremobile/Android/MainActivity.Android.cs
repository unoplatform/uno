using System;
using System.Threading;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Java.Interop;
using Microsoft.Identity.Client;
using Microsoft.UI.Xaml;
using Uno.UI;
using Windows.UI.ViewManagement;

namespace SamplesApp.Droid
{
	[Activity(
			Exported = true,
			MainLauncher = true,
			WindowSoftInputMode = SoftInput.AdjustPan | SoftInput.StateHidden,
			ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode
		)]
	// Ensure ActionMain intent filter is first in order, otherwise the app won't launch for debugging.
	[IntentFilter(
		new[] { Android.Content.Intent.ActionMain },
		Categories = new[] {
			Android.Content.Intent.CategoryLauncher,
			Android.Content.Intent.CategoryLeanbackLauncher
		})]
	[IntentFilter(
		new[] {
			Android.Content.Intent.ActionView
		},
		Categories = new[] {
			Android.Content.Intent.CategoryDefault,
			Android.Content.Intent.CategoryBrowsable,
		},
		DataScheme = "uno-samples-test")]
	public class MainActivity : ApplicationActivity
	{
		// Required for the MSAL sample "MsalLoginAndGraph"
		protected override void OnActivityResult(int requestCode, Result resultCode, Android.Content.Intent data)
		{
			base.OnActivityResult(requestCode, resultCode, data);
			AuthenticationContinuationHelper.SetAuthenticationContinuationEventArgs(requestCode, resultCode, data);
		}
	}

	// Required for the MSAL sample "MsalLoginAndGraph"
	[Activity(Exported = true)]
	[IntentFilter(
		[Android.Content.Intent.ActionView],
		Categories = [
			Android.Content.Intent.CategoryDefault,
			Android.Content.Intent.CategoryBrowsable
		],
		DataScheme = "msauth")]
	public class MsalActivity : BrowserTabActivity
	{
	}
}
