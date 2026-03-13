using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Media.Capture;
using Uno.Extensions.Media.Capture;
using Uno.Foundation.Extensibility;

namespace Uno.UI.Runtime.Skia.MacOS;

internal class MacOSCameraCaptureUIExtension : ICameraCaptureUIExtension
{
	private static readonly MacOSCameraCaptureUIExtension _instance = new();

	private MacOSCameraCaptureUIExtension()
	{
	}

	public static void Register() => ApiExtensibility.Register<CameraCaptureUI>(typeof(ICameraCaptureUIExtension), _ => _instance);

	private CameraCaptureUIMode _mode;
	private CameraCaptureUIPhotoFormat _photoFormat;
	private bool _allowCropping;

	public void Customize(CameraCaptureUI picker, CameraCaptureUIMode mode)
	{
		_mode = mode;
		_photoFormat = picker.PhotoSettings.Format;
		_allowCropping = picker.PhotoSettings.AllowCropping;
	}

	public async Task<StorageFile?> CaptureFileAsync(CancellationToken token)
	{
		if (_mode == CameraCaptureUIMode.Video)
		{
			return null;
		}

		var imagePath = NativeUno.uno_capture_photo(_photoFormat == CameraCaptureUIPhotoFormat.Jpeg);

		if (string.IsNullOrEmpty(imagePath))
		{
			return null;
		}

		var extension = imagePath.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ? ".jpg" : ".png";
		var tempPath = Path.Combine(ApplicationData.Current.TemporaryFolder.Path, Guid.NewGuid() + extension);
		File.Copy(imagePath, tempPath);
		File.Delete(imagePath);

		return await StorageFile.GetFileFromPathAsync(tempPath);
	}
}
