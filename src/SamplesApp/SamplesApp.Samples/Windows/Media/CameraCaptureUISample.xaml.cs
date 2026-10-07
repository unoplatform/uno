using System;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using Uno.UI.Samples.Controls;
using Windows.Media.Capture;
using Windows.Storage;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Uno.UI.Helpers;

namespace UITests.Windows_Media;

[Sample("CameraCapture", IsManualTest = true, Description = "Available on iOS, Android, and macOS (Skia Desktop).")]
public sealed partial class CameraCaptureUISample : Page
{
	public CameraCaptureUISample()
	{
		this.InitializeComponent();
	}

	private bool IsTargetSupported() =>
#if HAS_UNO
		DeviceTargetHelper.IsMobile() || OperatingSystem.IsMacOS();
#else
		true;
#endif

	private void CaptureImage_Click(object sender, RoutedEventArgs e) => CaptureImage(cancelAfterDelay: false);

	private void CaptureImageCancelledFromCode_Click(object sender, RoutedEventArgs e) => CaptureImage(cancelAfterDelay: true);

	private void CaptureVideo_Click(object sender, RoutedEventArgs e) => CaptureVideo(cancelAfterDelay: false);

	private void CaptureVideoCancelledFromCode_Click(object sender, RoutedEventArgs e) => CaptureVideo(cancelAfterDelay: true);

	private async void CaptureImage(bool cancelAfterDelay)
	{
		if (!IsTargetSupported())
		{
			return;
		}

		try
		{
			var file = await CaptureAsync(CameraCaptureUIMode.Photo, cancelAfterDelay);

			if (file != null)
			{
				using var stream = await file.OpenReadAsync();
				var bitmapImage = new BitmapImage();
				await bitmapImage.SetSourceAsync(stream);
				ImageControl.Source = bitmapImage;
			}
			else
			{
				ImageControl.Source = null;
			}
		}
		catch (OperationCanceledException)
		{
			ImageControl.Source = null;
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine(ex);
		}
	}

	private async void CaptureVideo(bool cancelAfterDelay)
	{
		if (!IsTargetSupported())
		{
			return;
		}

		StorageFile result;
		try
		{
			result = await CaptureAsync(CameraCaptureUIMode.Video, cancelAfterDelay);
		}
		catch (OperationCanceledException)
		{
			return;
		}

		if (result != null)
		{
			videoSize.Text = $"Captured file: {result.Path}, Size: {new FileInfo(result?.Path!).Length}";
		}
		else
		{
			videoSize.Text = "Nothing was selected";
		}
	}

	private async Task<StorageFile> CaptureAsync(CameraCaptureUIMode mode, bool cancelAfterDelay)
	{
		var captureUI = new CameraCaptureUI();
		CaptureStatus.Text = $"{mode} capture in progress";

		try
		{
			StorageFile file;
			if (cancelAfterDelay)
			{
				using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
				file = await captureUI.CaptureFileAsync(mode).AsTask(cts.Token);
			}
			else
			{
				file = await captureUI.CaptureFileAsync(mode);
			}

			CaptureStatus.Text = file is null ? $"{mode} capture returned no file" : $"{mode} capture completed";
			return file;
		}
		catch (OperationCanceledException)
		{
			CaptureStatus.Text = $"{mode} capture cancelled";
			throw;
		}
	}
}
