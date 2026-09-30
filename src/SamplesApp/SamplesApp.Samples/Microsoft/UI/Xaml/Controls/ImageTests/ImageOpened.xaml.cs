using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI.Samples.Controls;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using SamplesApp;

namespace Uno.UI.Samples.UITests.Image
{
	[Sample("Image", Name = "ImageOpened")]
	public sealed partial class ImageOpened : UserControl
	{
		public ImageOpened()
		{
			this.InitializeComponent();

			MyImage.ImageOpened += (s, e) => Show("MyImage.ImageOpened");
			MyImage.ImageFailed += (s, e) => Show("MyImage.ImageFailed");
			MyImageBrush.ImageOpened += (s, e) => Show("MyImageBrush.ImageOpened");
			MyImageBrush.ImageFailed += (s, e) => Show("MyImageBrush.ImageFailed");
		}

		private void UpdateSource(object sender, RoutedEventArgs e)
		{
			var button = sender as Button;
			var content = button.Content;
			var url = content.ToString();
			if (url == "null")
			{
				MyImage.Source = null;
				MyImageBrush.ImageSource = null;
			}
			else
			{
				var uri = new Uri(url);
				var bitmapImage = new BitmapImage(uri);
				MyImage.Source = bitmapImage;
				MyImageBrush.ImageSource = bitmapImage;
			}
		}

		private void Show(string text)
		{
			var dialog = new Windows.UI.Popups.MessageDialog(text);
#if HAS_UNO
			dialog.AssociatedWindow = App.MainWindow;
#endif
			_ = dialog.ShowAsync();
		}
	}
}
