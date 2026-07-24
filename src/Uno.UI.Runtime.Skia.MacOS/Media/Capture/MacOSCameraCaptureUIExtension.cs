using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Media.Capture;
using Uno.Extensions.Media.Capture;
using Uno.Foundation.Extensibility;
using Uno.UI.Dispatching;

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
			nativePath = await CaptureNativeAsync(token);

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

	private async Task<string?> CaptureNativeAsync(CancellationToken token)
	{
		string? Capture() => _mode == CameraCaptureUIMode.Video
			? NativeUno.uno_capture_video()
			: NativeUno.uno_capture_photo(_photoFormat == CameraCaptureUIPhotoFormat.Jpeg);

		// Run the blocking native modal from the main *run loop*, not the GCD main queue (which is
		// what NativeDispatcher.Main.Enqueue uses). [NSApp runModalForWindow:] blocks the thread it
		// runs on; if that thread is executing a serial GCD main-queue work item, the queue cannot
		// drain for the modal's lifetime, and AVFoundation delivers its capture/recording completion
		// through the main queue — so the capture never finishes and the Capture/Record buttons
		// freeze. A CFRunLoop-scheduled block leaves the serial main queue free while the modal pumps.
		// It also runs after the current event (the pointer dispatch that started the capture) has
		// unwound, so the modal does not re-enter the pointer pipeline.
		var tcs = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
		using var cancelRegistration = token.CanBeCanceled
			? token.Register(() =>
			{
				NativeUno.uno_capture_cancel();
				tcs.TrySetCanceled(token);
			})
			: default;

		Action work = () =>
		{
			if (token.IsCancellationRequested)
			{
				tcs.TrySetCanceled(token);
				return;
			}
			try
			{
				tcs.TrySetResult(Capture());
			}
			catch (Exception ex)
			{
				tcs.TrySetException(ex);
			}
		};

		ScheduleOnMainRunLoop(work);

		return await tcs.Task;
	}

	// Schedules the work item on the main run loop via the native helper. The GCHandle is freed by
	// MacOSDispatcher.NativeToManaged after the work runs (same trampoline used by the GCD dispatcher).
	private static unsafe void ScheduleOnMainRunLoop(Action work) =>
		NativeUno.uno_perform_on_main_runloop((nint)GCHandle.Alloc(work), &MacOSDispatcher.NativeToManaged);
}
