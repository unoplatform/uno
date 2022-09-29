#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System.Runtime.CompilerServices;
using Android.Util;

namespace Android.Content
{
	public static class ContextExtensions
	{
		private static float? _density;

		public static float ToPixels(this Android.Content.Context self, float dp)
		{
			Init(self);
			return (float)Math.Round(dp * _density.Value);
		}

		public static float FromPixels(this Android.Content.Context self, float pixels)
		{
			Init(self);
			return pixels / _density.Value;
		}

		private static void Init(Android.Content.Context context)
		{
			if (_density != null)
			{
				return;
			}

			using (var displayMetrics = context.Resources.DisplayMetrics)
			{
				_density = displayMetrics.Density;
			}
		}
	}
}