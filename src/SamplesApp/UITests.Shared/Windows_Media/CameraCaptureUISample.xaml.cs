using System;
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
	private async void CaptureButton_Click(object sender, RoutedEventArgs e)
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
#endif
}
