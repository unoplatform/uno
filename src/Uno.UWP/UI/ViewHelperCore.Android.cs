using System;
using System.Collections.Generic;
using Windows.Foundation;
using Uno.Logging;
using Uno.Extensions;
using Android.App;
using SizeF = System.Drawing.SizeF;
using System.Runtime.CompilerServices;
using Android.Views;
using System.Threading;
using System.Numerics;
using Android.Graphics;
using Point = Windows.Foundation.Point;
using Rect = Windows.Foundation.Rect;

namespace Uno.UI
{
	internal static class ViewHelperCore
	{
		/// <summary>
		/// The scale to be applied to all layout conversions from/to DIP (device-independent pixels) to physical pixels.
		/// </summary>
		public static double Scale { get; }

		static ViewHelperCore()
		{
			using (Android.Util.DisplayMetrics displayMetrics = Android.App.Application.Context.Resources.DisplayMetrics)
			{
				// WARNING: The Density value is not completely based on the DPI of the device.
				// On two 8" devices, the Density may not be consistent.
				Scale = displayMetrics.Density;
			}

			if (typeof(ViewHelperCore).Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
			{
				typeof(ViewHelperCore).Log().DebugFormat("Display Scale is {0}", Scale);
			}
		}
	}
}
