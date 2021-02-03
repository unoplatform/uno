using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Uno.Foundation;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.Storage.Provider;

namespace Windows.Storage
{
	public partial class CachedFileManager
	{
		public static void DeferUpdates(IStorageFile file)
		{
			// The method does nothing in wasm since we don't really acces the filesystem.
		}


		public static IAsyncOperation<FileUpdateStatus> CompleteUpdatesAsync(IStorageFile file) => DownloadFile(file).AsAsyncOperation();

		private async static Task<FileUpdateStatus> DownloadFile(IStorageFile file)
		{
			var stream = await file.OpenStreamForReadAsync();
			byte[] data;

			using (var reader = new BinaryReader(stream))
			{
				data = reader.ReadBytes((int)stream.Length);
			}

			var gch = GCHandle.Alloc(data, GCHandleType.Pinned);
			var pinnedData = gch.AddrOfPinnedObject();

			try
			{
				WebAssemblyRuntime.InvokeJS($"Windows.Storage.Pickers.FileSavePicker.SaveAs('{file.Name}', {pinnedData}, {data.Length})");
			}
			finally
			{
				gch.Free();
			}

			return FileUpdateStatus.Complete;
		}
	}
}
