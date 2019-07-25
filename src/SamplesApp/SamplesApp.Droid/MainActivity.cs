using Android.App;
using Android.Widget;
using Android.OS;
using Android.Content.PM;
using Android.Views;
using Java.Interop;

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

	}
}

