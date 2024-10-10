#if __SKIA__
using System;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Composition;

[TestClass]
[RunsOnUIThread]
public class Given_RelativeTransform
{
	[TestMethod]
	public async Task When_RotateTransform()
	{
		var sp = new StackPanel
		{
			Children =
			{
				new Border
				{
					Width = 150,
					Height = 200,
					Background = new ImageBrush
					{
						ImageSource = new BitmapImage(new Uri("ms-appx:///Assets/ingredient2.png")),
						Stretch = Stretch.UniformToFill,
						RelativeTransform = new RotateTransform
						{
							Angle = 45,
							CenterX = 0.5,
							CenterY = 0.5
						}
					},
				},
				new Image
				{
					Width = 150,
					Height = 200,
					Source = new BitmapImage(new Uri("ms-appx:///Uno.UI.RuntimeTests/Assets/rotate_transform.png"))
				}
			}
		};

		await UITestHelper.Load(sp);
		await ImageAssert.AreSimilarAsync(await UITestHelper.ScreenShot((FrameworkElement)sp.Children[1]), await UITestHelper.ScreenShot((FrameworkElement)sp.Children[0]));
	}

	[TestMethod]
	public async Task When_ScaleTransform()
	{
		var sp = new StackPanel
		{
			Children =
			{
				new Border
				{
					BorderThickness = new Thickness(30),
					Width = 200,
					Height = 100,
					Background = new ImageBrush
					{
						ImageSource = new BitmapImage(new Uri("ms-appx:///Assets/ingredient2.png")),
						Stretch = Stretch.UniformToFill,
						RelativeTransform = new ScaleTransform
						{
							ScaleX = 2,
							ScaleY = 3,
							CenterX = 0.5,
							CenterY = 0.5
						}
					},
				},
				new Image
				{
					Width = 200,
					Height = 100,
					Source = new BitmapImage(new Uri("ms-appx:///Uno.UI.RuntimeTests/Assets/scale_transform.png"))
				}
			}
		};

		await UITestHelper.Load(sp);
		await ImageAssert.AreEqualAsync(await UITestHelper.ScreenShot((FrameworkElement)sp.Children[1]), await UITestHelper.ScreenShot((FrameworkElement)sp.Children[0]));
	}
}
#endif
