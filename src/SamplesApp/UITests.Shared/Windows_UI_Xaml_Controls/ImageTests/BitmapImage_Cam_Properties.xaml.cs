using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI.Samples.Controls;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Capture;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Uno.UI.Samples.UITests.ImageTestsControl
{
	[Sample]
	public sealed partial class BitmapImage_Cam_Properties : UserControl
    {
        public BitmapImage_Cam_Properties()
        {
            this.InitializeComponent();
        }

		private async void btnOpenCam_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				var captureUI = new CameraCaptureUI();
				StorageFile photo = await captureUI.CaptureFileAsync(CameraCaptureUIMode.Photo);

				if (photo == null)
				{
					return;
				}
				ImageSource source = new BitmapImage(new Uri(photo.Path));
				ImageViewer.Source = source;
				GetDetails((BitmapImage)source);
			}
			catch (Exception ex)
			{
				tbDetails.Text = $"Details of image [error]: {ex.Message}";
			}

		}

		private void btnAssets_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				ImageSource source = new BitmapImage(new Uri("ms-appx:/Assets/uno-logo.png"));
				ImageViewer.Source = source;
				GetDetails((BitmapImage)source);
			}
			catch (Exception ex)
			{
				throw ex;
			}
		}

		private void GetDetails(BitmapImage source)
		{
			try
			{
				tbDetails.Text = $"Details of image: \nPixelHeight: {source.PixelHeight} " +
					$"\nPixelWidth: {source.PixelWidth} " +
					$"\nDecodePixelHeight : {source.DecodePixelHeight } " +
					$"\nDecodePixelWidth : {source.DecodePixelWidth } ";
			}
			catch (Exception ex)
			{
				throw ex;
			}

		}
	}
}
