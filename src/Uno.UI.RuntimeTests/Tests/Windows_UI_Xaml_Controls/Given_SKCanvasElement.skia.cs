using System.Drawing;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Private.Infrastructure;
using SkiaSharp;
using Uno.UI.RuntimeTests.Helpers;
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
			BorderBrush = Colors.Green,
			Height = 400,
			Child = new ScrollViewer
			{
				VerticalAlignment = VerticalAlignment.Top,
				Height = 100,
				Background = Colors.Red,
				Content = SUT
			}
		};

		await UITestHelper.Load(border);

		var bitmap = await UITestHelper.ScreenShot(border);

		ImageAssert.HasColorInRectangle(bitmap, new Rectangle(0, 0, 400, 300), Colors.Blue);
		ImageAssert.DoesNotHaveColorInRectangle(bitmap, new Rectangle(0, 101, 400, 299), Colors.Blue);
	}

	[TestMethod]
	[Ignore("RenderTargetBitmap doesn't account for FlowDirection")]
	public async Task When_RespectFlowDirection()
	{
		var SUT = new BlueAndRedFillSKCanvasElement
		{
			Width = 200,
			MirroredWhenRightToLeft = true
		};

		await UITestHelper.Load(SUT);

		var bitmap = await UITestHelper.ScreenShot(SUT);

		ImageAssert.DoesNotHaveColorInRectangle(bitmap, new Rectangle(0, 0, bitmap.Width / 2, bitmap.Height), Colors.Red);
		ImageAssert.DoesNotHaveColorInRectangle(bitmap, new Rectangle(bitmap.Width / 2, 0, bitmap.Width / 2, bitmap.Height), Colors.Blue);

		SUT.FlowDirection = FlowDirection.RightToLeft;
		await TestServices.WindowHelper.WaitForIdle();

		bitmap = await UITestHelper.ScreenShot(SUT);

		ImageAssert.DoesNotHaveColorInRectangle(bitmap, new Rectangle(0, 0, bitmap.Width / 2, bitmap.Height), Colors.Blue);
		ImageAssert.DoesNotHaveColorInRectangle(bitmap, new Rectangle(bitmap.Width / 2, 0, bitmap.Width / 2, bitmap.Height), Colors.Red);
	}

	private class BlueFillSKCanvasElement : SKCanvasElement
	{
		protected override void RenderOverride(SKCanvas canvas, Size area)
		{
			canvas.DrawRect(new SKRect(0, 0, (float)area.Width, (float)area.Height), new SKPaint { Color = SKColors.Blue });
		}
	}

	private class BlueAndRedFillSKCanvasElement : SKCanvasElement
	{
		protected override void RenderOverride(SKCanvas canvas, Size area)
		{
			canvas.DrawRect(new SKRect(0, 0, (float)area.Width / 2, (float)area.Height), new SKPaint { Color = SKColors.Blue });
			canvas.DrawRect(new SKRect((float)area.Width / 2, 0, (float)area.Width, (float)area.Height), new SKPaint { Color = SKColors.Red });
		}
	}
}
