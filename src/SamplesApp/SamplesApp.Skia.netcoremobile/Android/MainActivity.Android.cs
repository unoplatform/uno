using System;
using System.Threading;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Java.Interop;
using Microsoft.UI.Xaml;
using Uno.UI;
using Windows.UI.ViewManagement;

namespace SamplesApp.Droid
{
	[Activity(
			Exported = true,
			MainLauncher = true,
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
	public class MainActivity : ApplicationActivity
	{
		// private HandlerThread _pixelCopyHandlerThread;


	}
}
