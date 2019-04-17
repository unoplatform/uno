#if __MACOS__
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using System.Linq;
using Uno.UI.Samples.Controls;
using System.Collections;
using System.Collections.Generic;
using SampleControl.Entities;
using Uno.Disposables;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using AppKit;
using System.IO;
using Foundation;
using Windows.UI;
using Uno.Extensions;
using Uno.Logging;
using Microsoft.Extensions.Logging;

namespace SampleControl.Presentation
{

	public partial class SampleChooserViewModel
	{
		public void PrintViewHierarchy(AppKit.NSView vg, System.Text.StringBuilder sb, int level = 0)
		{
			for (int i = 0; i < vg.Subviews.Length; i++)
			{
				var v = vg.Subviews[i];
				var vElement = v as IFrameworkElement;
				var desc = string.Concat(Enumerable.Repeat("    |", level)) + $" [{i + 1}/{vg.Subviews.Length}] {v.Class.Name}";
				if (vElement != null)
				{
					desc += $" -- ActualHeight:{vElement.ActualHeight}, ActualWidth:{vElement.ActualWidth}, Height:{vElement.Height}, Width:{vElement.Width}, DataContext:{(vElement as IDataContextProvider)?.DataContext?.GetType().FullName}";
					var vTextBlock = vElement as TextBlock;
					if (vTextBlock != null)
					{
						desc += $", Text: {vTextBlock.Text}";
					}
				}

				sb.AppendLine(desc);
				var childViewGroup = v as AppKit.NSView;
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
			throw new NotSupportedException($"GenerateBitmap is not supported by this platform");

			//content.MinWidth = 400;
			//content.MinHeight = 400;
			//(content as AppKit.NSView).BackgroundColor = Colors.White;

			//var size = (content as AppKit.NSView).SizeThatFits(new CoreGraphics.CGSize(1024, 1024));

			//if (size.Width == nfloat.NaN || size.Height == nfloat.NaN)
			//{
			//	size = new CoreGraphics.CGSize(1024, 1024);
			//}

			//NSGraphics.BeginImageContextWithOptions(size, true, 0);
			//var ctx = NSGraphics.GetCurrentContext();

			//(content as AppKit.NSView)?.Layer.RenderInContext(ctx);

			//using (var img = UIGraphics.GetImageFromCurrentImageContext())
			//{
			//	NSGraphics.EndImageContext();

			//	var fullPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), folderName);
			//	var filePath = Path.Combine(fullPath, fileName);

			//	Directory.CreateDirectory(fullPath);

			//	NSError error;
			//	img.AsPNG().Save(filePath, true, out error);

			//	if (error != null)
			//	{
			//		this.Log().Error(error.ToString());
			//	}

			//	if (this.Log().IsEnabled(LogLevel.Debug))
			//	{
			//		this.Log().Debug($"Wrote screenshot to {filePath}");
			//	}
			//}
		}
	}
}
#endif
