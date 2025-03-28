using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI.Samples.Controls;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UITests.Shared.Windows_UI_Xaml_Controls.ImageTests
{
	[SampleControlInfo("Image", "Image_ImageSource_PixelSize")]
	public sealed partial class Image_ImageSource_PixelSize : UserControl
	{
		public Image_ImageSource_PixelSize()
		{
			this.InitializeComponent();

			resultImage.ImageOpened += (s, e) => loadState.Text = "ImageOpened";
			resultImage.ImageFailed += (s, e) => loadState.Text = "ImageFailed";
		}

		private void OnImage01(object sender, object args)
		{
			resultImage.Source = new BitmapImage(new Uri("ms-appx:///Assets/test_image_1252_836.png"));
		}
	}
}
