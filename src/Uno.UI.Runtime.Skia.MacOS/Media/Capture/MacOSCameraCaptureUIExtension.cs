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
			// Video capture not yet supported on macOS
			return null;
		}

		// Capture a photo using native macOS camera
		var imagePath = NativeUno.uno_capture_photo(_photoFormat == CameraCaptureUIPhotoFormat.Jpeg);

		if (string.IsNullOrEmpty(imagePath))
		{
			// User cancelled or error occurred
			return null;
		}

		// Copy the captured image to a temporary file
		var tempPath = Path.Combine(ApplicationData.Current.TemporaryFolder.Path, Guid.NewGuid() + (imagePath.EndsWith(".jpg") ? ".jpg" : ".png"));
		File.Copy(imagePath, tempPath);

		// Delete the original captured image
		File.Delete(imagePath);

		return await StorageFile.GetFileFromPathAsync(tempPath);
	}
}
