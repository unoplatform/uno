#if __MACOS__
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.Storage;
using ObjCRuntime;

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
