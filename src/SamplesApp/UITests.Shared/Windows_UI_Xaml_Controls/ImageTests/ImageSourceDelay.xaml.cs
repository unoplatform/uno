using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Uno.UI.Samples.Controls;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

namespace Uno.UI.Samples.UITests.Image
{
	[SampleControlInfo("Image", "ImageSourceDelay")]
	public sealed partial class ImageSourceDelay : UserControl
    {
        public ImageSourceDelay()
        {
            this.InitializeComponent(); 
        }

		private void btnLoadBmp1_Click(object sender, RoutedEventArgs e)
		{
			imgControl.Source = GetBitmap("ms-appx:///Assets/search.png");
			txtStatus.Text = "Bmp1";
		}
		
		private void btnLoadBmp2_Click(object sender, RoutedEventArgs e)
		{
			imgControl.Source = GetBitmap("ms-appx:///Assets/cart.png");
			txtStatus.Text = "Bmp2";
		}

		private BitmapImage GetBitmap(string fname)
		{
			var bmp = new BitmapImage();
			LoadImage();
			return bmp;

			async void LoadImage()
			{ 
				var file = await StorageFile.GetFileFromApplicationUriAsync(new Uri(fname));
				using (var fs = await file.OpenStreamForReadAsync())
				{
					await Task.Delay(100);
					await bmp.SetSourceAsync(fs.AsRandomAccessStream()); 
				}

			}
		}  
    }
}
