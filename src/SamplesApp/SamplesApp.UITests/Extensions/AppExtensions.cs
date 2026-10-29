using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Uno.UITest;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.Extensions
{
	public static class AppExtensions
	{
#if !IS_RUNTIME_UI_TESTS
		public static async Task<FileInfo> TakeScreenshotAsync(this IApp app, string title)
			=> app.Screenshot(title);

		public static async ValueTask DragCoordinatesAsync(this IApp app, float fromX, float fromY, float toX, float toY, CancellationToken ct = default)
			=> app.DragCoordinates(fromX, fromY, toX, toY);
#endif

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
			var byte64Image = GetInAppScreenshotData(
				() => app.InvokeGeneric("browser:SampleRunner|GetScreenshot", "0")?.ToString(),
				TimeSpan.FromSeconds(15));

			var array = Convert.FromBase64String(byte64Image);

			var outputFile = Path.GetTempFileName();
			File.WriteAllBytes(outputFile, array);

			var finalPath = Path.ChangeExtension(outputFile, ".png");

			File.Move(outputFile, finalPath);

			return new(finalPath);
#endif
		}

#if !IS_RUNTIME_UI_TESTS
		internal static string GetInAppScreenshotData(Func<string> getScreenshot, TimeSpan timeout)
		{
			var elapsed = Stopwatch.StartNew();
			while (true)
			{
				// WaitFor swallows predicate exceptions; a failed capture must not start another request.
				var response = getScreenshot();
				if (response != "pending")
				{
					if (string.IsNullOrEmpty(response))
					{
						throw new InvalidOperationException("The screenshot bridge returned no capture result.");
					}

					return response;
				}

				if (elapsed.Elapsed >= timeout)
				{
					throw new TimeoutException("The screenshot bridge did not complete within the polling timeout.");
				}

				Thread.Sleep(50);
			}
		}
#endif
	}
}
