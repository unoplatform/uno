using System;
using System.Numerics;
using System.Threading.Tasks;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Composition;

[TestClass]
[RunsOnUIThread]
public class Given_CompositionGeometry
{
	// WinUI doesn't expose Compositor.GetSharedCompositor; load a throwaway Border and pull the
	// compositor off its visual so these tests also compile and run against native WinUI.
	private static async Task<Compositor> GetCompositorAsync()
	{
		var anchor = new Border { Width = 1, Height = 1 };
		await UITestHelper.Load(anchor);
		return ElementCompositionPreview.GetElementVisual(anchor).Compositor;
	}

	// Regression for a crash surfaced by LottieGen-generated output (fireworks): a shape's Offset is
	// bound to an expression referencing the geometry's own Size — "my.Position-(my.Size/Vector2(2,2))".
	// CompositionRectangleGeometry did not expose Size/Offset as animatable properties, so evaluating
	// the expression threw "Unable to get property 'Size'".
	[TestMethod]
	// WinUI evaluates expressions in the compositor process, so the animated Offset isn't readable back.
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public async Task When_Rectangle_Size_Referenced_By_Expression()
	{
		var compositor = await GetCompositorAsync();
		var rectangle = compositor.CreateRectangleGeometry();
		rectangle.Size = new Vector2(40, 20);

		var animation = compositor.CreateExpressionAnimation("my.Size / Vector2(2, 2)");
		animation.SetReferenceParameter("my", rectangle);
		rectangle.StartAnimation("Offset", animation);

		try
		{
			Assert.AreEqual(new Vector2(20, 10), rectangle.Offset);

			// Changing Size re-evaluates the expression bound to Offset.
			rectangle.Size = new Vector2(80, 40);
			Assert.AreEqual(new Vector2(40, 20), rectangle.Offset);
		}
		finally
		{
			rectangle.StopAnimation("Offset");
		}
	}

	// Regression: CompositionSpriteShape.StrokeDashArray returned null until assigned, so
	// LottieGen output doing `shape.StrokeDashArray.Add(...)` threw a NullReferenceException.
	// WinUI returns a live, mutable collection.
	[TestMethod]
	public async Task When_StrokeDashArray_Is_Non_Null_And_Mutable()
	{
		var compositor = await GetCompositorAsync();
		var shape = compositor.CreateSpriteShape();

		Assert.IsNotNull(shape.StrokeDashArray);

		shape.StrokeDashArray.Add(2f);
		shape.StrokeDashArray.Add(10f);
		Assert.AreEqual(2, shape.StrokeDashArray.Count);
	}

	// The docs say TrimEnd defaults to 0, but WinUI reports (and renders) 1: an untrimmed geometry.
	[TestMethod]
	public async Task When_Default_Trim_Values()
	{
		var compositor = await GetCompositorAsync();
		var ellipse = compositor.CreateEllipseGeometry();

		Assert.AreEqual(0f, ellipse.TrimStart);
		Assert.AreEqual(1f, ellipse.TrimEnd);
		Assert.AreEqual(0f, ellipse.TrimOffset);
	}

	// A zero-length trim window draws nothing; TrimEnd = 0 must not read as "untrimmed".
	[TestMethod]
	[DataRow(0f, 0f, 0f)]
	[DataRow(0.3f, 0.3f, 0f)]
	[DataRow(0f, 0f, 0.25f)]
	public async Task When_Trim_Window_Is_Empty_Then_Nothing_Drawn(float trimStart, float trimEnd, float trimOffset)
	{
		var screenshot = await RenderStrokedEllipse(e =>
		{
			e.TrimStart = trimStart;
			e.TrimEnd = trimEnd;
			e.TrimOffset = trimOffset;
		});

		ImageAssert.DoesNotHaveColorInRectangle(screenshot, new System.Drawing.Rectangle(0, 0, 100, 100), Microsoft.UI.Colors.Red, tolerance: 100);
	}

	// With TrimEnd left at its default (1), TrimStart = 0.5 keeps the second half: 6 o'clock clockwise to 12.
	[TestMethod]
	public async Task When_Only_TrimStart_Set_Then_Draws_To_End()
	{
		var screenshot = await RenderStrokedEllipse(e => e.TrimStart = 0.5f);

		ImageAssert.HasColorAt(screenshot, 10, 50, Microsoft.UI.Colors.Red, tolerance: 30);
		ImageAssert.HasColorAt(screenshot, 90, 50, Microsoft.UI.Colors.White, tolerance: 30);
	}

	// WinUI wraps a window whose start is past its end through the path origin, even without TrimOffset.
	[TestMethod]
	public async Task When_TrimStart_Greater_Than_TrimEnd_Then_Wraps()
	{
		var screenshot = await RenderStrokedEllipse(e =>
		{
			e.TrimStart = 0.9f;
			e.TrimEnd = 0.1f;
		});

		ImageAssert.HasColorAt(screenshot, 50, 10, Microsoft.UI.Colors.Red, tolerance: 30);
		ImageAssert.HasColorAt(screenshot, 50, 90, Microsoft.UI.Colors.White, tolerance: 30);
	}

	[TestMethod]
	[DataRow(-0.1f, 1f)]
	[DataRow(0f, 1.2f)]
	public async Task When_Trim_Out_Of_Range_Then_Throws(float trimStart, float trimEnd)
	{
		var compositor = await GetCompositorAsync();
		var ellipse = compositor.CreateEllipseGeometry();

		Assert.ThrowsExactly<ArgumentException>(() =>
		{
			ellipse.TrimStart = trimStart;
			ellipse.TrimEnd = trimEnd;
		});
	}

	private static async Task<RawBitmap> RenderStrokedEllipse(Action<CompositionEllipseGeometry> configure)
	{
		var compositor = await GetCompositorAsync();
		var ellipse = compositor.CreateEllipseGeometry();
		ellipse.Center = new Vector2(50, 50);
		ellipse.Radius = new Vector2(40, 40);
		configure(ellipse);

		var shape = compositor.CreateSpriteShape(ellipse);
		shape.StrokeThickness = 8f;
		shape.StrokeBrush = compositor.CreateColorBrush(Microsoft.UI.Colors.Red);

		var shapeVisual = compositor.CreateShapeVisual();
		shapeVisual.Shapes.Add(shape);
		shapeVisual.Size = new Vector2(100, 100);

		var host = new Border
		{
			Width = 100,
			Height = 100,
			Background = new SolidColorBrush(Microsoft.UI.Colors.White),
		};
		ElementCompositionPreview.SetElementChildVisual(host, shapeVisual);

		await UITestHelper.Load(host);
		await UITestHelper.WaitForIdle();
		return await UITestHelper.ScreenShot(host);
	}
}
