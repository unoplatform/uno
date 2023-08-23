using System;
using System.Threading;
using Android.App;
using Android.OS;
using Android.Views;
using Java.Interop;
using Uno.UI;

namespace SamplesApp.Droid
{
	[Activity(
			Exported = true,
			MainLauncher = true,
			ConfigurationChanges = ActivityHelper.AllConfigChanges,
			WindowSoftInputMode = SoftInput.AdjustPan | SoftInput.StateHidden
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
	public class MainActivity : Microsoft.UI.Xaml.ApplicationActivity
	{
		private HandlerThread _pixelCopyHandlerThread;

		[Export("RunTest")]
		public string RunTest(string metadataName) => App.RunTest(metadataName);

		[Export("IsTestDone")]
		public bool IsTestDone(string testId) => App.IsTestDone(testId);

		[Export("GetDisplayScreenScaling")]
		public string GetDisplayScreenScaling(string displayId) => App.GetDisplayScreenScaling(displayId);

		/// <summary>
		/// Returns a base64 encoded PNG file
		/// </summary>
		[Export("GetScreenshot")]
		public string GetScreenshot(string displayId)
		{
			var rootView = WinUIWindow.RootElement as View;

			var bitmap = Android.Graphics.Bitmap.CreateBitmap(rootView.Width, rootView.Height, Android.Graphics.Bitmap.Config.Argb8888);
			var locationOfViewInWindow = new int[2];
			rootView.GetLocationInWindow(locationOfViewInWindow);

			var xCoordinate = locationOfViewInWindow[0];
			var yCoordinate = locationOfViewInWindow[1];

			var scope = new Android.Graphics.Rect(
				xCoordinate,
				yCoordinate,
				xCoordinate + rootView.Width,
				yCoordinate + rootView.Height
			);

			if (_pixelCopyHandlerThread == null)
			{
				_pixelCopyHandlerThread = new Android.OS.HandlerThread("ScreenshotHelper");
				_pixelCopyHandlerThread.Start();
			}

			var listener = new PixelCopyListener();

			// PixelCopy.Request returns the actual rendering of the screen location
			// for the app, incliing OpenGL content.
			PixelCopy.Request(Window, scope, bitmap, listener, new Android.OS.Handler(_pixelCopyHandlerThread.Looper));

			listener.WaitOne();

			using var memoryStream = new System.IO.MemoryStream();
			bitmap.Compress(Android.Graphics.Bitmap.CompressFormat.Png, 100, memoryStream);

			return Convert.ToBase64String(memoryStream.ToArray());
		}


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
		}


		class PixelCopyListener : Java.Lang.Object, PixelCopy.IOnPixelCopyFinishedListener
		{
			private ManualResetEvent _event = new ManualResetEvent(false);

			public void WaitOne()
			{
				_event.WaitOne();
			}

			public void OnPixelCopyFinished(int copyResult)
			{
				_event.Set();
			}
		}
	}
}

