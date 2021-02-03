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

		public IAsyncOperation<StorageFile> PickSaveFileAsync() => PickFilesTask().AsAsyncOperation();

		public FileSavePicker()
		{
			SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
			FileTypeChoices = new Dictionary<string, IList<string>>();
		}

		private async Task<StorageFile> PickFilesTask()
		{
			if (!FileTypeChoices.Any())
			{
				throw new InvalidOperationException();
			}

			if (SuggestedSaveFile == null)
			{
				var temporaryFolder = ApplicationData.Current.LocalCacheFolder;
				if (!Directory.Exists(temporaryFolder.Path))
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
