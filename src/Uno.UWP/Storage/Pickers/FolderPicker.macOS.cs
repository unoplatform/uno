#if __MACOS__
using System;
using System.Threading.Tasks;
using AppKit;
using Windows.Foundation;

namespace Windows.Storage.Pickers
{
	public partial class FolderPicker
	{
		public FolderPicker()
		{	
		}

		public IAsyncOperation<StorageFolder> PickSingleFolderAsync() =>
			PickSingleFolderImplAsync().AsAsyncOperation();

		private async Task<StorageFolder> PickSingleFolderImplAsync()
		{
			var openPanel = new NSOpenPanel();
			openPanel.AllowedFileTypes = new[] { "none" };
			openPanel.AllowsOtherFileTypes = false;
			openPanel.CanChooseDirectories = true;
			openPanel.CanChooseFiles = false;
			var response = openPanel.RunModal();
			if (response == 1 && openPanel.Urls.Length > 0)
			{
				var path = openPanel.Urls[0]?.Path;
				if (path != null)
				{
					return new StorageFolder(path);
				}
			}
			return null;
		}
	}
}
#endif
