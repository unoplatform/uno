#if __WASM__
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Uno.Extensions;
using Uno.Foundation;
using Windows.Foundation;
using Windows.Storage.Provider;

namespace Windows.Storage.Pickers
{
	public partial class FileSavePicker
	{
		public PickerLocationId SuggestedStartLocation { get; set; }

		private const string LocalCachePath = "/LocalCache";

		public IAsyncOperation<StorageFile> PickSaveFileAsync() => PickFilesTask().AsAsyncOperation();

		private async Task<StorageFile> PickFilesTask()
		{
			if (!FileTypeChoices.Any())
			{
				throw new COMException();
			}

			if (SuggestedSaveFile == null)
			{
				var temporaryFolder = new StorageFolder(LocalCachePath);
				if (!Directory.Exists(LocalCachePath))
				{
					temporaryFolder.MakePersistent();
				}
				// The mime type is chosen by the extension, and we cannot reliably send multiple mime type in the browser
				var fileName = SuggestedFileName + FileTypeChoices.First().Value[0];
				SuggestedSaveFile = await temporaryFolder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);
			}
			return SuggestedSaveFile;
		}
	}
}
#endif
