using System.Numerics;
using System.Threading.Tasks;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Composition;

[TestClass]
[RunsOnUIThread]
public class Given_CompositionGeometry
{
	// Regression for a crash surfaced by LottieGen-generated output (fireworks): a shape's Offset is
	// bound to an expression referencing the geometry's own Size — "my.Position-(my.Size/Vector2(2,2))".
	// CompositionRectangleGeometry did not expose Size/Offset as animatable properties, so evaluating
	// the expression threw "Unable to get property 'Size'".
	[TestMethod]
#if !__SKIA__
	[Ignore("ExpressionAnimation evaluation is Skia-only")]
#endif
	public void When_Rectangle_Size_Referenced_By_Expression()
	{
		var compositor = Compositor.GetSharedCompositor();
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
	public void When_StrokeDashArray_Is_Non_Null_And_Mutable()
	{
		var compositor = Compositor.GetSharedCompositor();
		var shape = compositor.CreateSpriteShape();

		Assert.IsNotNull(shape.StrokeDashArray);

		shape.StrokeDashArray.Add(2f);
		shape.StrokeDashArray.Add(10f);
		Assert.AreEqual(2, shape.StrokeDashArray.Count);
	}
}
