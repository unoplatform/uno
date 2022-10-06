#nullable disable

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Uno.UITest;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.Extensions
{
	public static class AppExtensions
	{
		public static void DragCoordinates(this IApp app, PointF from, PointF to) => app.DragCoordinates(from.X, from.Y, to.X, to.Y);

#if !IS_RUNTIME_UI_TESTS
		private static float? _scaling;
#endif

		public static float GetDisplayScreenScaling(this IApp app)
		{
#if IS_RUNTIME_UI_TESTS
			return 1f;
#else
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
#endif
		}

		public static FileInfo GetInAppScreenshot(this IApp app)
		{
#if IS_RUNTIME_UI_TESTS
			return null;
#else
			var byte64Image = app.InvokeGeneric("browser:SampleRunner|GetScreenshot", "0")?.ToString();

			var array = Convert.FromBase64String(byte64Image);

			var outputFile = Path.GetTempFileName();
			File.WriteAllBytes(outputFile, array);

			var finalPath = Path.ChangeExtension(outputFile, ".png");

			File.Move(outputFile, finalPath);

			return new(finalPath);
#endif
		}
	}
}
