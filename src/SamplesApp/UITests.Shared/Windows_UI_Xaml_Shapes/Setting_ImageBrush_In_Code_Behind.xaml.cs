using System;
using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace UITests.Windows_UI_Xaml_Shapes
{
	[Sample("Shapes")]
	public sealed partial class Setting_ImageBrush_In_Code_Behind : Page
	{
		public Setting_ImageBrush_In_Code_Behind()
		{
			this.InitializeComponent();
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			myShape1.Fill = new ImageBrush
			{
				ImageSource = new BitmapImage { UriSource = new Uri("ms-appx:///Assets/Formats/uno-overalls.jpg") }
			};

			var imageBrush = new ImageBrush();
			myShape2.Fill = imageBrush;
			imageBrush.ImageSource = new BitmapImage { UriSource = new Uri("ms-appx:///Assets/Formats/uno-overalls.jpg") };
		}
	}
}
