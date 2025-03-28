using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;
using Windows.UI.Xaml.Media.Imaging;

namespace UITests.Microsoft_UI_Xaml_Controls.ImageIconTests
{
	[Sample("Icons")]
	public sealed partial class ImageIconPage : Page
	{
		public ImageIconPage()
		{
			this.InitializeComponent();
		}
		private void ToggleButton_Checked(object sender, RoutedEventArgs e)
		{
			this.ImageIcon.Source = new BitmapImage(new Uri("ms-appx:///Assets/icon.png"));
		}

		private void ToggleButton_Unchecked(object sender, RoutedEventArgs e)
		{
			BitmapImage bitmapImage = new BitmapImage();
			Uri uri = new Uri("ms-appx:///Assets/ingredient2.png");
			bitmapImage.UriSource = uri;

			this.ImageIcon.Source = bitmapImage;
		}
	}
}
