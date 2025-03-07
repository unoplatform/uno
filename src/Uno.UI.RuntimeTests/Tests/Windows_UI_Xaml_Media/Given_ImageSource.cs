using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Media;

[RunsOnUIThread]
[TestClass]
public class Given_ImageSource
{
	[TestMethod]
	public async Task When_ImageSource()
	{
		var container = new Grid()
		{
			Width = 110,
			Height = 110,
		};
		var image = new Image
		{
			Width = 100,
			Height = 100,
		};

		container.Children.Add(image);

		WindowHelper.WindowContent = image;

		await WindowHelper.WaitForLoaded(image);

		await WindowHelper.WaitForIdle();

		bool opened = false;
		var uri = new Uri("https://images.google.com/images/branding/googleg/1x/googleg_standard_color_128dp.png");
		var bitmapImage = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(uri);
		image.ImageOpened += (s, e) => opened = true;
		image.Source = bitmapImage;

		await WindowHelper.WaitFor(() => opened);

		opened = false;
		bitmapImage = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(uri);
		image.Source = bitmapImage;

		await WindowHelper.WaitFor(() => opened);

		// This method should successfully run and not crash.
	}
}
