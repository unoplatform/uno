using System;
using Android.App;
using Android.Content.PM;
using Android.Views;
using Java.Interop;
using Windows.UI.Core;
using Windows.UI.ViewManagement;

namespace SamplesApp.Droid
{
	[Activity(
			MainLauncher = true,
			ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize,
			WindowSoftInputMode = SoftInput.AdjustPan | SoftInput.StateHidden
		)]
	public class MainActivity : Windows.UI.Xaml.ApplicationActivity
	{
		[Export("RunTest")]
		public string RunTest(string metadataName) => App.RunTest(metadataName);

		[Export("IsTestDone")]
		public bool IsTestDone(string testId) => App.IsTestDone(testId);

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
	}
}

