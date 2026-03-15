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
	private MacOSCameraCaptureUIExtension()
	{
	}

	public static void Register() =>
		ApiExtensibility.Register<CameraCaptureUI>(typeof(ICameraCaptureUIExtension), _ => new MacOSCameraCaptureUIExtension());

	private CameraCaptureUIMode _mode;
	private CameraCaptureUIPhotoFormat _photoFormat;
	private CameraCaptureUIVideoFormat _videoFormat;

	// Note: PhotoSettings.AllowCropping is not supported on macOS.
	// The captured photo is returned as-is without cropping UI.

	public void Customize(CameraCaptureUI picker, CameraCaptureUIMode mode)
	{
		_mode = mode;
		_photoFormat = picker.PhotoSettings.Format;
		_videoFormat = picker.VideoSettings.Format;

		if (_mode != CameraCaptureUIMode.Video)
		{
			switch (_photoFormat)
			{
				case CameraCaptureUIPhotoFormat.Jpeg:
				case CameraCaptureUIPhotoFormat.Png:
					break;

				default:
					throw new NotSupportedException(
						$"Only {CameraCaptureUIPhotoFormat.Jpeg} and {CameraCaptureUIPhotoFormat.Png} photo formats are supported on macOS.");
			}
		}

		if (_mode == CameraCaptureUIMode.Video && _videoFormat != CameraCaptureUIVideoFormat.Mp4)
		{
			throw new NotSupportedException($"Only {CameraCaptureUIVideoFormat.Mp4} video format is supported on macOS.");
		}
	}

	public async Task<StorageFile?> CaptureFileAsync(CancellationToken token)
	{
		token.ThrowIfCancellationRequested();

		string? nativePath = null;

		try
		{
			if (_mode == CameraCaptureUIMode.Video)
			{
				nativePath = NativeUno.uno_capture_video();
			}
			else
			{
				nativePath = NativeUno.uno_capture_photo(_photoFormat == CameraCaptureUIPhotoFormat.Jpeg);
			}

			token.ThrowIfCancellationRequested();

			// Note: The native implementation currently returns null both when the user cancels
			// the capture and when the capture fails due to permission or usage-description issues.
			// As a result, we cannot distinguish between these cases here and simply return null.
			if (nativePath is null)
			{
				return null;
			}

			var ext = Path.GetExtension(nativePath);
			var tempPath = Path.Combine(ApplicationData.Current.TemporaryFolder.Path, Guid.NewGuid() + ext);
			File.Move(nativePath, tempPath);

			// Prevent cleanup of the moved file in the finally block.
			nativePath = null;

			return await StorageFile.GetFileFromPathAsync(tempPath);
		}
		finally
		{
			if (!string.IsNullOrEmpty(nativePath))
			{
				try
				{
					if (File.Exists(nativePath))
					{
						File.Delete(nativePath);
					}
				}
				catch
				{
					// Ignore cleanup failures.
				}
			}
		}
	}
}
