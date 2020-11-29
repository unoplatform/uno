using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uno.UITest;
using Uno.UITest.Helpers;

namespace SamplesApp.UITests.Extensions
{
	public static class AppExtensions
	{
		public static void DragCoordinates(this IApp app, PointF from, PointF to) => app.DragCoordinates(from.X, from.Y, to.X, to.Y);

		private static float? _scaling;
		public static float GetDisplayScreenScaling(this IApp app)
		{
			return _scaling ?? (float)(_scaling = GetScaling());

			float GetScaling()
			{
				var scalingRaw = app.InvokeGeneric("browser:SampleRunner|GetDisplayScreenScaling", "0");

				if (float.TryParse(scalingRaw?.ToString(), NumberStyles.Float, NumberFormatInfo.InvariantInfo, out var scaling))
				{
					Console.WriteLine($"Display Scaling: {scaling}");
					return scaling / 100f;
				}
				else
				{
					return 1f;
				}
			}
		}
	}
}
