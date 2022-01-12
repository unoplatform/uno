using System;
using ElmSharp;
using SkiaSharp;
using SkiaSharp.Views.Tizen;
using WUX = Windows.UI.Xaml;

namespace Uno.UI.Runtime.Skia
{
	public class UnoCanvas : SKCanvasView
	{
		readonly static Action<SKSurface, string> doNothing = (_, _) => { };
		Action<SKSurface, string> MakeScreenshot = doNothing;
		string? screenshotFilePath = default;

		public UnoCanvas(EvasObject parent) : base(parent)
		{
			PaintSurface += UnoCanvas_PaintSurface;
			Resized += UnoCanvas_Resized;

			WUX.Window.InvalidateRender += () => Invalidate();
		}

		private void UnoCanvas_Resized(object sender, EventArgs e)
		{
			var c = (SKCanvasView)sender;

			var geometry = c.Geometry;

			// control is not yet fully initialized
			if (geometry.Width <= 0 || geometry.Height <= 0)
			{
				return;
			}

			var scale = (float)ScalingInfo.ScalingFactor;

			WUX.Window.Current.OnNativeSizeChanged(
				new Windows.Foundation.Size(
				geometry.Width / scale,
				geometry.Height / scale));
		}

		private void UnoCanvas_PaintSurface(object sender, SKPaintSurfaceEventArgs e)
		{
			e.Surface.Canvas.Clear(SKColors.Blue);

			var scale = (float)ScalingInfo.ScalingFactor;
			e.Surface.Canvas.Scale(scale);

			WUX.Window.Current.Compositor.Render(e.Surface, e.Info);
			MakeScreenshot(e.Surface, screenshotFilePath);
		}

		internal System.Threading.Tasks.Task TakeScreenshot(string filePath)
		{
			screenshotFilePath = filePath;
			var tcs = new System.Threading.Tasks.TaskCompletionSource<bool>();
			MakeScreenshot = (surface, fullpath) =>
			{
				MakeScreenshot = doNothing;
				try
				{
					using var image = surface.Snapshot();
					using (var data = image.Encode(SKEncodedImageFormat.Png, 100))
					{
						// save the data to a stream
						using (var stream = System.IO.File.OpenWrite(fullpath))
						{
							data.SaveTo(stream);
						}
					}
					tcs.SetResult(true);
				}
				catch (Exception ex)
				{
					tcs.SetException(ex);
				}
			};
			// Force invalidate to make screenshot
			Invalidate();
			return tcs.Task;
		}
	}
}
