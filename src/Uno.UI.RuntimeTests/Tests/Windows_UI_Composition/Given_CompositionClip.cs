using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.RuntimeTests.Tests.Uno_UI_Xaml_Core;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Composition;

[TestClass]
internal class Given_CompositionClip
{
#if __SKIA__
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_TransformClippedElement_Then_ClippingAppliedPostRendering()
	{
		var sut = new Grid
		{
			Width = 300,
			Height = 300,
			Background = new SolidColorBrush(Colors.Chartreuse),
			Children =
			{
				new Grid
				{
					Width = 200,
					Height = 200,
					Background = new SolidColorBrush(Colors.DeepPink),
					Children =
					{
						new Grid
						{
							Width = 300,
							Height = 300,
							HorizontalAlignment = HorizontalAlignment.Center,
							VerticalAlignment = VerticalAlignment.Center,
							Background = new SolidColorBrush(Colors.DeepSkyBlue),
							RenderTransform = new TranslateTransform { Y = 100 },
						}
					}
				}
			}
		};

		await UITestHelper.Load(sut);

		var result = await UITestHelper.ScreenShot(sut);

		ImageAssert.HasColorAt(result, 150, 1, Colors.Chartreuse);
		ImageAssert.HasColorAt(result, 150, 49, Colors.Chartreuse);
		ImageAssert.HasColorAt(result, 150, 51, Colors.DeepPink);
		ImageAssert.HasColorAt(result, 150, 99, Colors.DeepPink);
		ImageAssert.HasColorAt(result, 150, 101, Colors.DeepSkyBlue);
		ImageAssert.HasColorAt(result, 150, 249, Colors.DeepSkyBlue);
		ImageAssert.HasColorAt(result, 150, 299, Colors.Chartreuse);
	}
#endif
}
