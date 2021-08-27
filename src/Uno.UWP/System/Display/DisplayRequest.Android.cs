#if __ANDROID__
using System;
using System.Collections.Generic;
using System.Text;
using Android.App;
using Android.Views;
using Uno.UI;

namespace Windows.System.Display
{
	public partial class DisplayRequest
	{
#pragma warning disable CA1822 // Mark members as static
		partial void ActivateScreenLock()
		{
			var activity = GetActivity();
			activity.Window.AddFlags(WindowManagerFlags.KeepScreenOn);
		}

		partial void DeactivateScreenLock()
		{
			var activity = GetActivity();
			activity.Window.ClearFlags(WindowManagerFlags.KeepScreenOn);
		}
#pragma warning restore CA1822 // Mark members as static

		private static Activity GetActivity()
		{
			if (ContextHelper.Current is Activity activity)
			{
				return activity;
			}
			else
			{
				throw new InvalidOperationException("Application Activity is not initialized.");
			}
		}
	}
}
#endif
