using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Uno.Foundation;
using Uno.Storage.Internal;
using Windows.Storage.Provider;

using NativeMethods = __Windows.Storage.Pickers.FileSavePicker.NativeMethods;

namespace Windows.Storage
{
	public static partial class CachedFileManager
	{
		private static async Task<FileUpdateStatus> CompleteUpdatesTaskAsync(IStorageFile file, CancellationToken token)
		{
			if (file is StorageFile storageFile && storageFile.Provider == StorageProviders.WasmDownloadPicker)
			{
				var stream = await file.OpenStreamForReadAsync();

				if (stream.Length > int.MaxValue)
				{
					// The download is materialized as a single managed array before being
					// handed to the browser, which caps it at 2 GB.
					throw new NotSupportedException("Files larger than 2 GB cannot be saved using the download-based picker.");
				}

				byte[] data;

				using (var reader = new BinaryReader(stream))
				{
					data = reader.ReadBytes((int)stream.Length);
				}

				var gch = GCHandle.Alloc(data, GCHandleType.Pinned);
				var pinnedData = gch.AddrOfPinnedObject();

				try
				{
					NativeMethods.SaveAs(file.Name, pinnedData, data.Length);
				}
				finally
				{
					gch.Free();
				}
			}

			return FileUpdateStatus.Complete;
		}
	}
}
