using System;
using Android.App;
using Android.Content.PM;
using Android.Views;
using Java.Interop;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Microsoft.Identity.Client;

namespace SamplesApp.Droid
{
	[Activity(
			MainLauncher = true,
			ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode,
			WindowSoftInputMode = SoftInput.AdjustPan | SoftInput.StateHidden
		)]
	[IntentFilter(
		new[] {
			Android.Content.Intent.ActionView
		},
		Categories = new[] {
			Android.Content.Intent.CategoryDefault,
			Android.Content.Intent.CategoryBrowsable
		},
		DataScheme = "uno-samples-test")]
	public class MainActivity : Windows.UI.Xaml.ApplicationActivity
	{
		[Export("RunTest")]
		public string RunTest(string metadataName) => App.RunTest(metadataName);

		[Export("IsTestDone")]
		public bool IsTestDone(string testId) => App.IsTestDone(testId);

		[Export("GetDisplayScreenScaling")]
		public string GetDisplayScreenScaling(string displayId) => App.GetDisplayScreenScaling(displayId);

		[Export("SetFullScreenMode")]
		public void SetFullScreenMode(bool fullscreen)
		{
			// workaround for #2747: force the app into fullscreen
			// to prevent status bar from reappearing when popup are shown.
			var activity = Uno.UI.ContextHelper.Current as Activity;
			if (fullscreen)
			{
				activity.Window.AddFlags(WindowManagerFlags.Fullscreen);
			}
			else
			{
				activity.Window.ClearFlags(WindowManagerFlags.Fullscreen);
			}
		}
		protected override void OnActivityResult(int requestCode, Result resultCode, Android.Content.Intent data)
		{
			base.OnActivityResult(requestCode, resultCode, data);
#if !NET6_0
			AuthenticationContinuationHelper.SetAuthenticationContinuationEventArgs(requestCode, resultCode, data);
#endif
		}
	}

#if !NET6_0
	[Activity]
	[IntentFilter(
		new[] {
			Android.Content.Intent.ActionView
		},
		Categories = new[] {
			Android.Content.Intent.CategoryDefault,
			Android.Content.Intent.CategoryBrowsable
		},
		DataScheme = "msauth")]
	public class MsalActivity : BrowserTabActivity
	{
	}
#endif

}

