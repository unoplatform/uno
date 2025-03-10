using System;
using System.Threading.Tasks;
using System.IO;
using Uno.UI.Samples.Controls;
using Windows.Media.Capture;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

namespace UITests.Windows_Media;

[Sample("CameraCapture", IsManualTest = true)]
public sealed partial class CameraCaptureUISample : Page
{
	public CameraCaptureUISample()
	{
		this.InitializeComponent();
	}

#if __ANDROID__ || __IOS__
	private async void CaptureImage_Click(object sender, RoutedEventArgs e)
	{
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
#endif
}
