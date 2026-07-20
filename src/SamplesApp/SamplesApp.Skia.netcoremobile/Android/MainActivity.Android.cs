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
		private HandlerThread _pixelCopyHandlerThread;

		protected override void OnCreate(Bundle bundle)
		{
			AndroidX.Core.SplashScreen.SplashScreen.InstallSplashScreen(this);

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
					"UITEST_RUNTIME_AUTOSTART_RESULT_FILE",
					"UITEST_RUNTIME_TESTS_FILTER"
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

		[Export("RunTest")]
		public string RunTest(string metadataName) => App.RunTest(metadataName);

		[Export("IsTestDone")]
		public bool IsTestDone(string testId) => App.IsTestDone(testId);

		[Export("GetDisplayScreenScaling")]
		public string GetDisplayScreenScaling(string displayId) => App.GetDisplayScreenScaling(displayId);

		[Export("GetScreenshot")]
		public string GetScreenshot(string displayId)
		{
			var rootView = Window.DecorView;
			var bitmap = Android.Graphics.Bitmap.CreateBitmap(
				rootView.Width,
				rootView.Height,
				Android.Graphics.Bitmap.Config.Argb8888);
			var scope = new Android.Graphics.Rect(0, 0, rootView.Width, rootView.Height);

			if (_pixelCopyHandlerThread is null)
			{
				_pixelCopyHandlerThread = new HandlerThread("ScreenshotHelper");
				_pixelCopyHandlerThread.Start();
			}

			var listener = new PixelCopyListener();
			PixelCopy.Request(Window, scope, bitmap, listener, new Handler(_pixelCopyHandlerThread.Looper));
			listener.WaitOne();

			using var memoryStream = new MemoryStream();
			bitmap.Compress(Android.Graphics.Bitmap.CompressFormat.Png, 100, memoryStream);
			return Convert.ToBase64String(memoryStream.ToArray());
		}

		[Export("SetFullScreenMode")]
		public void SetFullScreenMode(bool fullscreen)
		{
			if (fullscreen)
			{
				Window.AddFlags(WindowManagerFlags.Fullscreen);
			}
			else
			{
				Window.ClearFlags(WindowManagerFlags.Fullscreen);
			}
		}

		// Required for the MSAL sample "MsalLoginAndGraph"
		protected override void OnActivityResult(int requestCode, Result resultCode, Android.Content.Intent data)
		{
			base.OnActivityResult(requestCode, resultCode, data);
			AuthenticationContinuationHelper.SetAuthenticationContinuationEventArgs(requestCode, resultCode, data);
		}

		private sealed class PixelCopyListener : Java.Lang.Object, PixelCopy.IOnPixelCopyFinishedListener
		{
			private readonly ManualResetEvent _event = new(false);

			public void WaitOne() => _event.WaitOne();

			public void OnPixelCopyFinished(int copyResult) => _event.Set();
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
