using System.Threading;
using System.Threading.Tasks;
using Windows.Storage.Provider;

namespace Windows.Storage
{
	public static partial class CachedFileManager
	{
		private static async Task<FileUpdateStatus> CompleteUpdatesTaskAsync(IStorageFile file, CancellationToken token)
		{
			// Triggering the download is a user-visible side effect - don't start one for a cancelled commit.
			token.ThrowIfCancellationRequested();

			if (file is StorageFile { Implementation: StorageFile.DownloadPickerStorageFile downloadFile })
			{
				// The content is handed to the browser JS-side, without materializing
				// the payload in managed memory.
				await downloadFile.TriggerDownloadAsync();
			}

			return FileUpdateStatus.Complete;
		}
	}
}
