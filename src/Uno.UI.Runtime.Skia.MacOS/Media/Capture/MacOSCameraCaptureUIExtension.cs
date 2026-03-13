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
		string? nativePath;

		if (_mode == CameraCaptureUIMode.Video)
		{
			nativePath = NativeUno.uno_capture_video();
		}
		else
		{
			nativePath = NativeUno.uno_capture_photo(_photoFormat == CameraCaptureUIPhotoFormat.Jpeg);
		}

		if (string.IsNullOrEmpty(nativePath))
		{
			return null;
		}

		var ext = Path.GetExtension(nativePath);
		var tempPath = Path.Combine(ApplicationData.Current.TemporaryFolder.Path, Guid.NewGuid() + ext);
		File.Copy(nativePath, tempPath);
		File.Delete(nativePath);

		return await StorageFile.GetFileFromPathAsync(tempPath);
	}
}
