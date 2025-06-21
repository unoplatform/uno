using System;
using System.Collections.Generic;
using System.Diagnostics;
using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using SamplesApp.UITests;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace UITests.Windows_UI_Xaml.Performance
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	[Sample("Performance", IsManualTest = true, Description = "Make sure the numbers do not regress between different Uno versions")]
	public sealed partial class Performance_ImageLoading : Page
	{
		private static readonly List<string> _images =
		[
			"ms-appx:/Assets/LargeWisteria.jpg",
			"ms-appx:///Assets/BackArrow.png",
			"ms-appx:///Assets/test_image_125_125.png",
			"ms-appx:///Assets/test_image_150_100.png",
			"ms-appx:///Assets/Icons/search.png",
			"ms-appx:///Assets/ThemeTestImage.png",
			"ms-appx:///Assets/square100.png",
			"ms-appx:///Assets/Icons/menu.png",
			"ms-appx:///Assets/Icons/star_full.png",
			"ms-appx:///Assets/ingredient1.png",
			"ms-appx:///Assets/ingredient2.png",
			"ms-appx:///Assets/ingredient3.png",
			"ms-appx:///Assets/ingredient4.png",
			"ms-appx:///Assets/ingredient5.png"
		];

		public Performance_ImageLoading()
		{
			var start = Stopwatch.GetTimestamp();

			var sp = new StackPanel();
			Content = sp;

			foreach (var image in _images)
			{
				var tb = new TextBlock();
				sp.Children.Add(new StackPanel
				{
					Orientation = Orientation.Horizontal,
					Children =
					{
						tb,
						new Image
						{
							Width = 10,
							Height = 10,
							Source = new BitmapImage { UriSource = new Uri(image) },
						}.Apply(img => img.Loaded += (_, _) => tb.Text = $"Image {image} loaded in {Stopwatch.GetElapsedTime(start).TotalMilliseconds}ms from sample creation")
					}
				});
			}
		}
	}
}
