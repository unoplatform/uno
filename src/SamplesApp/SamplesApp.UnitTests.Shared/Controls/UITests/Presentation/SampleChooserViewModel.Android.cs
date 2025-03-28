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
using Windows.Storage;

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
				var desc = string.Concat(Enumerable.Repeat("    |", level)) + $" [{i + 1}/{vg.ChildCount}] {v.Class.SimpleName}";
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

		private async Task<StorageFolder> GetStorageFolderFromNameOrCreate(CancellationToken ct, string folderName)
		{
			var root = new StorageFolder(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
			var folder = await root.CreateFolderAsync(folderName,
				CreationCollisionOption.OpenIfExists
				).AsTask(ct);
			return folder;
		}
	}
}
#endif
