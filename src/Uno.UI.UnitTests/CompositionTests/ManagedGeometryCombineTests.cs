#nullable enable

using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Composition.Drawing;

namespace Uno.UI.Tests.CompositionTests;

// Boolean-combine edge cases on the SkiaSharp-free ManagedGeometry that used to misrender silently.
[UITestClass]
public class ManagedGeometryCombineTests
{
	private static readonly ManagedGeometryFactory _factory = new();

	private static IGeometry Rect(float x, float y, float w, float h)
	{
		var b = _factory.CreatePathBuilder();
		b.MoveTo(new Vector2(x, y));
		b.LineTo(new Vector2(x + w, y));
		b.LineTo(new Vector2(x + w, y + h));
		b.LineTo(new Vector2(x, y + h));
		b.Close();
		return b.Build();
	}

	private static IGeometry Empty() => _factory.CreatePathBuilder().Build();

	[TestMethod]
	public void When_Difference_Empty_Minus_B_Is_Empty()
	{
		// Difference is A\B; with A empty the result must be empty (the bug returned B).
		var result = Empty().Combine(Rect(0, 0, 10, 10), GeometryCombineMode.Difference);
		Assert.IsFalse(result.FillContains(new Vector2(5, 5)), "Difference(∅, B) must not contain B's interior");
		Assert.AreEqual(0, result.Bounds.Width);
		Assert.AreEqual(0, result.Bounds.Height);
	}

	[TestMethod]
	public void When_Difference_A_Minus_Empty_Is_A()
	{
		// A\∅ == A.
		var result = Rect(0, 0, 10, 10).Combine(Empty(), GeometryCombineMode.Difference);
		Assert.IsTrue(result.FillContains(new Vector2(5, 5)), "Difference(A, ∅) must still contain A's interior");
	}

	[TestMethod]
	public void When_Intersect_With_Empty_Is_Empty()
	{
		Assert.IsFalse(Empty().Combine(Rect(0, 0, 10, 10), GeometryCombineMode.Intersect).FillContains(new Vector2(5, 5)));
		Assert.IsFalse(Rect(0, 0, 10, 10).Combine(Empty(), GeometryCombineMode.Intersect).FillContains(new Vector2(5, 5)));
	}

	[TestMethod]
	public void When_Union_With_Empty_Is_The_Other()
	{
		Assert.IsTrue(Empty().Combine(Rect(0, 0, 10, 10), GeometryCombineMode.Union).FillContains(new Vector2(5, 5)));
		Assert.IsTrue(Rect(0, 0, 10, 10).Combine(Empty(), GeometryCombineMode.Union).FillContains(new Vector2(5, 5)));
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/26278")]
	public void When_Difference_Of_Identical_Rects_Is_Empty()
	{
		// Every edge of each operand lies on the other's boundary.
		var result = Rect(0, 0, 200, 200).Combine(Rect(0, 0, 200, 200), GeometryCombineMode.Difference);
		Assert.IsFalse(result.FillContains(new Vector2(100, 100)));
		Assert.IsFalse(result.FillContains(new Vector2(2, 100)));
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/26278")]
	public void When_Difference_Shares_Edges_With_A_Rounded_Rect()
	{
		var rounded = Primitive(b => b.AddRoundedRectangle(new Windows.Foundation.Rect(0, 0, 200, 200), 60, 60));
		var result = Rect(0, 0, 200, 200).Combine(rounded, GeometryCombineMode.Difference);
		Assert.IsFalse(result.FillContains(new Vector2(100, 100)), "The rounded rect's interior must be cut out.");
		Assert.IsFalse(result.FillContains(new Vector2(2, 100)), "Its straight sides lie on the rect's: nothing may remain there.");
		Assert.IsTrue(result.FillContains(new Vector2(3, 3)), "Outside the rounded corner, the rect remains.");
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/26278")]
	[DataRow("ellipse")]
	[DataRow("rounded")]
	[DataRow("rect")]
	public void When_Union_With_Its_Own_Stroke_Outline_Keeps_The_Interior(string shape)
	{
		// The stroke outline is a union of overlapping stamps: its edges inside itself must not survive.
		var painted = shape switch
		{
			"ellipse" => Primitive(b => b.AddEllipse(new Vector2(100, 100), 100, 100)),
			"rounded" => Primitive(b => b.AddRoundedRectangle(new Windows.Foundation.Rect(0, 0, 200, 200), 60, 60)),
			_ => Rect(0, 0, 200, 200),
		};
		var outline = painted.GetStrokeFillGeometry(new StrokeStyle { Thickness = 2, StartCap = StrokeCap.Round, EndCap = StrokeCap.Round, LineJoin = StrokeJoin.Round });

		var result = painted.Combine(outline, GeometryCombineMode.Union);

		Assert.IsTrue(result.FillContains(new Vector2(100, 100)), "The interior must stay filled.");
		Assert.IsTrue(result.FillContains(new Vector2(-0.5f, 100)), "The outline extends the shape by a pixel.");
		Assert.IsFalse(result.FillContains(new Vector2(-2, 100)), "Nothing beyond the outline.");
	}

	[TestMethod]
	public void When_Union_With_An_EvenOdd_Ring_Keeps_Its_Hole()
	{
		var ring = Primitive(b =>
		{
			b.FillRule = GeometryFillRule.EvenOdd;
			b.AddRectangle(new Windows.Foundation.Rect(0, 0, 200, 200));
			b.AddRectangle(new Windows.Foundation.Rect(10, 10, 180, 180));
		});

		var result = ring.Combine(Rect(50, 50, 100, 100), GeometryCombineMode.Union);

		Assert.IsTrue(result.FillContains(new Vector2(5, 5)));
		Assert.IsFalse(result.FillContains(new Vector2(30, 30)), "The ring's hole outside the added rect stays empty.");
		Assert.IsTrue(result.FillContains(new Vector2(100, 100)));
	}

	[TestMethod]
	public void When_Xor_Of_Overlapping_Ellipses()
	{
		var result = Primitive(b => b.AddEllipse(new Vector2(100, 100), 100, 100))
			.Combine(Primitive(b => b.AddEllipse(new Vector2(150, 100), 100, 100)), GeometryCombineMode.Xor);

		Assert.IsFalse(result.FillContains(new Vector2(125, 100)), "The overlap is excluded.");
		Assert.IsTrue(result.FillContains(new Vector2(20, 100)));
		Assert.IsTrue(result.FillContains(new Vector2(230, 100)));
	}

	private static IGeometry Primitive(System.Action<IPrimitiveGeometryBuilder> build)
	{
		var builder = _factory.CreatePrimitiveGeometryBuilder();
		build(builder);
		return builder.Build();
	}
}
