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
		partial void ActivateScreenLock()
		{
			var activity = GetActivity();
			activity.Window!.AddFlags(WindowManagerFlags.KeepScreenOn);
		}

		partial void DeactivateScreenLock()
		{
			var activity = GetActivity();
			activity.Window!.ClearFlags(WindowManagerFlags.KeepScreenOn);
		}

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
