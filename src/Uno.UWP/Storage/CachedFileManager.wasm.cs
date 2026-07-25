using System.Threading;
using System.Threading.Tasks;
using Windows.Storage.Provider;

namespace Windows.Storage
{
	public static partial class CachedFileManager
	{
		private static Task<FileUpdateStatus> CompleteUpdatesTaskAsync(IStorageFile file, CancellationToken token)
		{
			if (file is StorageFile { Implementation: StorageFile.DownloadPickerStorageFile downloadFile })
			{
				// The content was staged JS-side; the Blob is built there directly,
				// without materializing the payload in managed memory.
				downloadFile.TriggerDownload();
			}

			return Task.FromResult(FileUpdateStatus.Complete);
		}
	}
}
