using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
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


namespace Uno.UI.RuntimeTests.Images
{

	public sealed partial class ImageUriVariationsPage : Page
	{
		private int totalImages;
		private int imageLoadCount;
		private TaskCompletionSource<bool> _tcs = new TaskCompletionSource<bool>();
		public ImageUriVariationsPage()
		{
			this.InitializeComponent();

			Loaded += ImageUriVariationsPage_Loaded;
		}

		private void ImageUriVariationsPage_Loaded(object sender, RoutedEventArgs e)
		{

			var list = new List<Image>();
			ImageSearch(this, list);
			totalImages = list.Count;
		}

		private void ImageSearch(DependencyObject root, List<Image> images)
		{
			var children = VisualTreeHelper.GetChildrenCount(root);

			for (int i = 0; i < children; i++)
			{
				var child = VisualTreeHelper.GetChild(root, i);

				if (child is Image image)
				{
					images.Add(image);
				}
				else
				{
					ImageSearch(child, images);
				}
			}
		}

		private void ImageLoaded(object sender, RoutedEventArgs e)
		{
			HandleImageEvent(sender as Image, e, true);
		}

		private void ImageFailed(object sender, ExceptionRoutedEventArgs e)
		{
			HandleImageEvent(sender as Image, e, false);
		}

		private void HandleImageEvent(Image sender, RoutedEventArgs e, bool success)
		{
			var tagBits = (sender.Tag as string).Split(',');
			var originalSource = tagBits[0];
			var shouldLoad = bool.Parse(tagBits[1]);

			Assert.AreEqual(shouldLoad, success, $"Image {(shouldLoad ? "SHOULD" : "should NOT")} load but {(success ? "DID" : "did NOT")} load - " +
					$"Original Source '{originalSource}' Actual Source '{((sender as Image)!.Source as BitmapImage)?.UriSource}'");

			imageLoadCount++;
			if (totalImages > 0 && imageLoadCount == totalImages)
			{
				_tcs.SetResult(true);
			}
		}

		public Task WaitForImagesToLoad()
		{
			var delay = TimeSpan.FromSeconds(2);
			Task.Run(async () =>
			{
				await Task.Delay(delay);
				_tcs.TrySetCanceled();
			});

			return _tcs.Task;
		}
	}
}
