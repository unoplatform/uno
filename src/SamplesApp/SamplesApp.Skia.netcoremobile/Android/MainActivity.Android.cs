using System;
using System.IO;
using System.Linq;
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
			WindowSoftInputMode = SoftInput.AdjustNothing | SoftInput.StateHidden,
			ConfigurationChanges = global::Uno.UI.ActivityHelper.AllConfigChanges
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

		protected override void OnCreate(Bundle bundle)
		{
			var externalFilesDir = Microsoft.UI.Xaml.NativeApplication.Context.GetExternalFilesDir(null);
			if (externalFilesDir != null)
			{
				string fullPath = Path.Combine(externalFilesDir.AbsolutePath, "primestorage.txt");
				File.WriteAllText(fullPath, "Primed so the folder is useable");
			}

			var extras = Intent.Extras;
			if (extras != null)
			{
				string[] knownVariables = [
					"UITEST_RUNTIME_TEST_GROUP",
					"UITEST_RUNTIME_TEST_GROUP_COUNT",
					"UITEST_RUNTIME_AUTOSTART_RESULT_FILE"
				];

				foreach (var key in extras.KeySet())
				{
					if (knownVariables.Contains(key))
					{
						var value = extras.GetString(key);
						System.Environment.SetEnvironmentVariable(key, value);
					}
				}
			}

			base.OnCreate(bundle);
		}
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
