#nullable enable

using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Composition.Drawing;

namespace Uno.UI.Tests.CompositionTests;

// Boolean-combine edge cases on the SkiaSharp-free ManagedGeometry that used to misrender silently.
[TestClass]
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
}
