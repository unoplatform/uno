#if WINAPPSDK
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

#if WINAPPSDK
using Microsoft.UI.Xaml;
using Windows.Graphics.Imaging;
using Windows.Graphics.Display;
using Windows.Storage;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using Microsoft.UI.Xaml.Controls;
#else
using FrameworkElement = Microsoft.UI.Xaml.IFrameworkElement;
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

		private async Task<StorageFolder> GetStorageFolderFromNameOrCreate(CancellationToken ct, string folderName)
		{
			var root = ApplicationData.Current.LocalFolder;
			var folder = await root.CreateFolderAsync(folderName,
				CreationCollisionOption.OpenIfExists
				).AsTask(ct);
			return folder;
		}

		private (double MinWidth, double MinHeight, double Width, double Height) GetScreenshotConstraints()
			=> (400, 400, 1200, 800);
	}
}
#endif
