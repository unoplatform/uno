using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Private.Infrastructure;
using Uno.Extensions;
using Uno.UI.RuntimeTests.Helpers;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Media;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Composition;

[TestClass]
public class Given_CompositionVisualSurface
{
#if __SKIA__
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_SourceVisual_Changes()
	{
		var compositor = Window.Current.Compositor;
		var expected = new Image
		{
			Width = 200,
			Height = 200,
			Stretch = Stretch.UniformToFill,
			Source = ImageSource.TryCreateUriFromString("https://uno-assets.platform.uno/logos/uno.png")
		};
		var sut = new ContentControl
		{
			Width = 200,
			Height = 200
		};

		var visualSurface = compositor.CreateVisualSurface();
		visualSurface.SourceVisual = ElementCompositionPreview.GetElementVisual(expected);
		visualSurface.SourceSize = new(200, 200);

		var spriteVisual = compositor.CreateSpriteVisual();
		spriteVisual.Size = new(200, 200);
		spriteVisual.Brush = compositor.CreateSurfaceBrush(visualSurface);

		ElementCompositionPreview.SetElementChildVisual(sut, spriteVisual);

		var result = await Render(expected, sut);
		await ImageAssert.AreEqualAsync(result.actual, result.expected);
	}

	private async Task<(RawBitmap expected, RawBitmap actual)> Render(FrameworkElement expected, FrameworkElement sut)
	{
		await UITestHelper.Load(new Grid
		{
			ColumnDefinitions =
			{
				new ColumnDefinition(),
				new ColumnDefinition()
			},
			Children =
			{
				expected.Apply(e => Grid.SetColumn(e, 0)),
				sut.Apply(e => Grid.SetColumn(e, 1))
			}
		});

		return (await UITestHelper.ScreenShot(expected), await UITestHelper.ScreenShot(sut));
	}
#endif
}
