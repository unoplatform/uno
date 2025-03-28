#if __SKIA__
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.Storage;
using Uno.UI.Extensions;
using System.Collections.Immutable;

namespace SampleControl.Presentation
{

	public partial class SampleChooserViewModel
	{
		public void PrintViewHierarchy(UIElement c, StringBuilder sb, int level = 0)
		{
			var children = c.GetChildren().ToImmutableArray();
			for (int i = 0; i < children.Length; i++)
			{
				var v = children[i];
				var vElement = (FrameworkElement)v;
				var desc = v.GetDebugIdentifier();
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
	}
}
#endif
