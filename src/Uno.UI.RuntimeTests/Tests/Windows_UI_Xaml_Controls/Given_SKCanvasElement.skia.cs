using System.Drawing;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using SkiaSharp;
using Uno.UI.RuntimeTests.Helpers;
using Uno.WinUI.Graphics2DSK;
using Size = Windows.Foundation.Size;
namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

[TestClass]
[RunsOnUIThread]
public class Given_SKCanvasElement
{
	[TestMethod]
	public async Task When_Clipped_Inside_ScrollViewer()
	{
		var SUT = new BlueFillSKCanvasElement
		{
			Height = 400,
			Width = 400
		};

		var border = new Border
		{
			BorderBrush = Windows.UI.Colors.Green,
			Height = 400,
			Child = new ScrollViewer
			{
				VerticalAlignment = VerticalAlignment.Top,
				Height = 100,
				Background = Windows.UI.Colors.Red,
				Content = SUT
			}
		};

		await UITestHelper.Load(border);

		var bitmap = await UITestHelper.ScreenShot(border);

		ImageAssert.HasColorInRectangle(bitmap, new Rectangle(0, 0, 400, 300), Windows.UI.Colors.Blue);
		ImageAssert.DoesNotHaveColorInRectangle(bitmap, new Rectangle(0, 101, 400, 299), Windows.UI.Colors.Blue);
	}

	private class BlueFillSKCanvasElement : SKCanvasElement
	{
		protected override void RenderOverride(SKCanvas canvas, Size area)
		{
			canvas.DrawRect(new SKRect(0, 0, (float)area.Width, (float)area.Height), new SKPaint { Color = SKColors.Blue });
		}
	}
}
