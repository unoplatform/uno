using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;

namespace Windows.Media.Capture
{
	public partial class CameraCaptureUI
	{
		public global::Windows.Media.Capture.CameraCaptureUIPhotoCaptureSettings PhotoSettings
		{
			get;
		} = new CameraCaptureUIPhotoCaptureSettings();

		public global::Windows.Media.Capture.CameraCaptureUIVideoCaptureSettings VideoSettings
		{
			get;
		} = new CameraCaptureUIVideoCaptureSettings();

		public CameraCaptureUI()
		{
			VideoSettings.Format = CameraCaptureUIVideoFormat.Mp4;
			InitializePlatform();
		}

		partial void InitializePlatform();

		public global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.StorageFile> CaptureFileAsync(global::Windows.Media.Capture.CameraCaptureUIMode mode)
		{
			return AsyncOperation.FromTask(ct => CaptureFile(ct, mode));
		}

#if __ANDROID__ || __IOS__
		private static async Task<StorageFile> CreateTempImage(Stream source, string extension)
		{
			var filePath = Path.Combine(ApplicationData.Current.TemporaryFolder.Path, Guid.NewGuid() + extension);

			using (var file = File.OpenWrite(filePath))
			{
				using (source)
				{
					source.CopyTo(file);
				}
			}

			return await StorageFile.GetFileFromPathAsync(filePath);
		}
#endif
	}
}
