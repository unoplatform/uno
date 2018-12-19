#if __ANDROID__
using Android.Graphics;
using Android.Views;
using SampleControl.Entities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Uno.Disposables;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Uno.UI;
using Uno.UI.Samples.Controls;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace SampleControl.Presentation
{

	public partial class SampleChooserViewModel
	{
		public void PrintViewHierarchy(Android.Views.ViewGroup vg, StringBuilder sb, int level = 0)
		{
			for (int i = 0; i < vg.ChildCount; i++)
			{
				var v = vg.GetChildAt(i);
				var vElement = v as IFrameworkElement;
				var desc = string.Concat(Enumerable.Repeat("    |", level)) + $" [{i+1}/{vg.ChildCount}] {v.Class.SimpleName}";
				if (vElement != null)
				{
					desc += $" -- ActualHeight:{vElement.ActualHeight}, ActualWidth:{vElement.ActualWidth}, Height:{vElement.Height}, Width:{vElement.Width}, DataContext:{vElement.DataContext?.GetType().FullName}";
					var vTextBlock = vElement as TextBlock;
					if (vTextBlock != null)
					{
						desc += $", Text: {vTextBlock.Text}";
					}
				}

				sb.AppendLine(desc);
				var childViewGroup = v as Android.Views.ViewGroup;
				if (childViewGroup != null)
				{
					PrintViewHierarchy(childViewGroup, sb, level + 1);
				}
			}
		}

		private async Task DumpOutputFolderName(CancellationToken ct, string folderName)
		{
			var fullPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), folderName);

			Console.WriteLine($"Output folder for tests: {fullPath}");
		}

		private async Task GenerateBitmap(CancellationToken ct, string folderName, string fileName, IFrameworkElement content)
		{
			var nativeView = content as View;

			try
			{
				SetSoftwareRendering(nativeView, enabled: true);

				content.MinWidth = 400;
				content.MinHeight = 400;
				nativeView.SetBackgroundColor(Colors.White);

				nativeView.Measure(
					ViewHelper.MakeMeasureSpec(1024, MeasureSpecMode.AtMost),
					ViewHelper.MakeMeasureSpec(1024, MeasureSpecMode.AtMost)
				);

				nativeView.Layout(0, 0, nativeView.MeasuredWidth, nativeView.MeasuredHeight);

				// Allow for the view to layout and render itself properly by allowing two 
				// dispatcher loops.
				await Task.Yield();
				await Task.Yield();

				var captureView = (nativeView?.Parent as View) ?? nativeView;

				var screenshot = CaptureView(captureView);

				var fullPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), folderName);
				var filePath = System.IO.Path.Combine(fullPath, fileName);

				Directory.CreateDirectory(fullPath);

				using (var stream = File.OpenWrite(filePath))
				{
					await screenshot.CompressAsync(Bitmap.CompressFormat.Png, 100, stream);
				}

				Console.WriteLine($"Wrote screenshot to: {filePath}");
			}
			finally
			{
				SetSoftwareRendering(nativeView, enabled: false);

				// Force re-rendering so that the software internally allocated memory is released.
				nativeView.Layout(0, 0, nativeView.MeasuredWidth, nativeView.MeasuredHeight);
			}
		}

		/// <summary>
		/// Forces all descendent views to render using the software renderer instead
		/// of the hardware renderer.
		/// </summary>
		/// <remarks>
		/// This allows for harware-only surfaces, like overlays, to be visible in the 
		/// the screenshots.
		/// </remarks>
		private static void SetSoftwareRendering(View nativeView, bool enabled)
		{
			var layerType = enabled ? LayerType.Software : LayerType.Hardware;

			nativeView.SetLayerType(layerType, null);

			foreach (var child in (nativeView as ViewGroup).EnumerateAllChildren(maxDepth: 1024))
			{
				child.SetLayerType(layerType, null);
			}
		}

		Bitmap renderSurface;

		private Bitmap CaptureView(View view)
		{
			if(renderSurface == null || renderSurface.Width != view.Width || renderSurface.Height != view.Height)
			{
				renderSurface?.Dispose();
				renderSurface = null;

				renderSurface = Bitmap.CreateBitmap(view.Width, view.Height, Bitmap.Config.Argb8888);
			}

			using (var canvas = new Android.Graphics.Canvas(renderSurface))
			{
				view.Draw(canvas);

				return renderSurface;
			}
		}
	}
}
#endif
