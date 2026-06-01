using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Private.Infrastructure;
using Uno.Extensions;
using Uno.UI.RuntimeTests.Helpers;
using Windows.Graphics.Display;
using Windows.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Media;

[TestClass]
public class Given_XamlCompositionBrushBase
{
#if !__SKIA__
	[Ignore]
#endif
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_CompositionBrush_Changes()
	{
		var dpi = TestServices.WindowHelper.XamlRoot.RasterizationScale;

		var compositor = Window.Current.Compositor;
		var expected = new Grid
		{
			Width = 200,
			Height = 200
		};
		var sut = new ContentControl
		{
			Width = 200,
			Height = 200
		};

		var colorBrush = compositor.CreateColorBrush(Colors.Red);

		var brush = compositor.CreateNineGridBrush();
		brush.Source = colorBrush;
		brush.IsCenterHollow = true;
		brush.SetInsets(20);

		expected.Background = new TestBrush(brush);

		var spriteVisual = compositor.CreateSpriteVisual();
		spriteVisual.Size = new(200, 200);
		spriteVisual.Brush = brush;

		ElementCompositionPreview.SetElementChildVisual(sut, spriteVisual);

		var result = await RenderElements(expected, sut);
		if (dpi == 1.0d)
		{
			await ImageAssert.AreEqualAsync(result.actual, result.expected);
		}
		else
		{
			await ImageAssert.AreSimilarAsync(result.actual, result.expected, 0.0405d * (dpi / 1.25d));
		}
	}

	private class TestBrush : Microsoft.UI.Xaml.Media.XamlCompositionBrushBase
	{
		private CompositionBrush Brush;

		public TestBrush(CompositionBrush brush) => Brush = brush;

		protected override void OnConnected() => CompositionBrush = Brush;
	}

	private async Task<(RawBitmap expected, RawBitmap actual)> RenderElements(FrameworkElement expected, FrameworkElement sut)
	{
		await UITestHelper.Load(expected);
		var ss1 = await UITestHelper.ScreenShot(expected);

		await UITestHelper.Load(sut);
		var ss2 = await UITestHelper.ScreenShot(sut);

		return (ss1, ss2);
	}
}
