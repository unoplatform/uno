using System;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.Storage.Provider;

namespace Windows.Storage
{
	public  partial class CachedFileManager 
	{
		public static void DeferUpdates(IStorageFile file) {
			// The method does nothing in wasm since we don't really acces the filesystem.
		}
			

		public static IAsyncOperation<FileUpdateStatus> CompleteUpdatesAsync(IStorageFile file)
		{
			// Make it download the file.
			return FileUpdateStatus.Complete;
		}
	}
}
