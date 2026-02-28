using System;
using System.Threading.Tasks;
using System.IO;
using Uno.UI.Samples.Controls;
using Windows.Media.Capture;
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

	private async void CaptureImage_Click(object sender, RoutedEventArgs e)
	{
		if (!IsTargetSupported())
		{
			return;
		}

		try
		{
			var captureUI = new CameraCaptureUI();

			var file = await captureUI.CaptureFileAsync(CameraCaptureUIMode.Photo);

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
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine(ex);
		}
	}

	private async void CaptureVideo_Click(object sender, RoutedEventArgs e)
	{
		if (!IsTargetSupported())
		{
			return;
		}

		var captureUI = new CameraCaptureUI();

		var result = await captureUI.CaptureFileAsync(CameraCaptureUIMode.Video);

		if (result != null)
		{
			videoSize.Text = $"Captured file: {result.Path}, Size: {new FileInfo(result?.Path!).Length}";
		}
		else
		{
			videoSize.Text = "Nothing was selected";
		}
	}
}
