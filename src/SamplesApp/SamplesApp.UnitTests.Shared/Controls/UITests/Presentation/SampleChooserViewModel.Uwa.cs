#if NETFX_CORE
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using System.Linq;
using Uno.UI.Samples.Controls;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using SampleControl.Entities;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using Uno.Extensions;
using Uno.Logging;
using Microsoft.Extensions.Logging;
using Uno.UI.Extensions;

#if NETFX_CORE
using Windows.UI.Xaml;
using Windows.Graphics.Imaging;
using Windows.Graphics.Display;
using Windows.Storage;
using Windows.UI.Xaml.Media;
using Windows.UI;
using Windows.UI.Xaml.Controls;
#else
using FrameworkElement = Windows.UI.Xaml.IFrameworkElement;
#endif

namespace SampleControl.Presentation
{

	public partial class SampleChooserViewModel
	{
		public void PrintViewHierarchy(FrameworkElement c, StringBuilder sb, int level = 0)
		{
			var children = c.GetChildren().ToImmutableArray();
            for (int i = 0; i < children.Length; i++)
			{
				var v = children[i];
				var vElement = (FrameworkElement)v;
				var desc = string.Concat(Enumerable.Repeat("    |", level)) + $" [{i + 1}/{children.Length}] {v.GetType().Name}";
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
				var childViewGroup = v as Control;
				if (childViewGroup != null)
				{
					PrintViewHierarchy(childViewGroup, sb, level + 1);
				}
			}
		}

		private async Task DumpOutputFolderName(CancellationToken ct, string folderName)
		{
			var folder = await Windows.Storage.ApplicationData.Current.LocalFolder.CreateFolderAsync(
				folderName,
				Windows.Storage.CreationCollisionOption.OpenIfExists
			).AsTask(ct);

			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"Output folder for tests: {folder.Path}");
			}
		}

		private static async Task GenerateBitmap(CancellationToken ct, string folderName, string fileName, FrameworkElement content)
		{
			var parent = content.Parent as FrameworkElement;

			if (parent != null)
			{
				parent.MinWidth = 400;
				parent.MinHeight = 400;
			}

			content.MinWidth = 400;
			content.MinHeight = 400;

			var border = content.FindFirstChild<Border>();

			if (border != null)
			{
				border.Background = new SolidColorBrush(Colors.White);
			}

			Windows.UI.Xaml.Media.Imaging.RenderTargetBitmap bmp = new Windows.UI.Xaml.Media.Imaging.RenderTargetBitmap();

			await bmp.RenderAsync(content.Parent as FrameworkElement).AsTask(ct);

			content.DataContext = null;

			var pixels = await bmp.GetPixelsAsync().AsTask(ct);

			var folder = await Windows.Storage.ApplicationData.Current.LocalFolder.CreateFolderAsync(
				folderName,
				Windows.Storage.CreationCollisionOption.OpenIfExists
			).AsTask(ct);

			if (folder == null)
			{
				folder = await Windows.Storage.ApplicationData.Current.LocalFolder.CreateFolderAsync(folderName).AsTask(ct);
			}

			var file = await folder.CreateFileAsync(
				fileName,
				Windows.Storage.CreationCollisionOption.ReplaceExisting
			).AsTask(ct);

			using (var fileStream = await file.OpenAsync(FileAccessMode.ReadWrite).AsTask(ct))
			{
				var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, fileStream).AsTask(ct);

				encoder.SetPixelData(
					BitmapPixelFormat.Bgra8,
					BitmapAlphaMode.Ignore,
					(uint)bmp.PixelWidth,
					(uint)bmp.PixelHeight,
					DisplayInformation.GetForCurrentView().RawDpiX,
					DisplayInformation.GetForCurrentView().RawDpiY,
					pixels.ToArray()
				);

				await encoder.FlushAsync().AsTask(ct);
			}
		}
	}
}
#endif
