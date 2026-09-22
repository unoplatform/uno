using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;
using Windows.Foundation;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Media;

[TestClass]
[RunsOnUIThread]
public class Given_GeneralTransform_Inverse
{
	private const double Tolerance = 0.01;

	[TestMethod]
	public void When_Inverse_Of_MatrixTransform_Then_RoundTrips()
	{
		var transform = new MatrixTransform { Matrix = new Matrix(2, 0, 0, 4, 10, 20) };

		var forward = transform.TransformPoint(new Point(5, 7));
		var backward = transform.Inverse.TransformPoint(forward);

		Assert.AreEqual(5, backward.X, Tolerance);
		Assert.AreEqual(7, backward.Y, Tolerance);
	}

	[TestMethod]
	public void When_Inverse_Of_Identity_Then_PointIsUnchanged()
	{
		var transform = new MatrixTransform { Matrix = Matrix.Identity };

		var inverted = transform.Inverse.TransformPoint(new Point(5, 7));

		Assert.AreEqual(5, inverted.X, Tolerance);
		Assert.AreEqual(7, inverted.Y, Tolerance);
	}

#if HAS_UNO
	[TestMethod]
	public void When_TryTransformInverse_Then_MatchesInverseTransformPoint()
	{
		foreach (var transform in GetInvertibleTransforms())
		{
			var point = new Point(13, 29);
			var expected = transform.Inverse.TransformPoint(point);

			Assert.IsTrue(transform.TryTransformInverse(point, out var actual), $"{transform.GetType().Name} should be invertible");
			Assert.AreEqual(expected.X, actual.X, Tolerance, $"{transform.GetType().Name} X");
			Assert.AreEqual(expected.Y, actual.Y, Tolerance, $"{transform.GetType().Name} Y");
		}
	}

	[TestMethod]
	public void When_TryTransformBoundsInverse_Then_MatchesInverseTransformBounds()
	{
		foreach (var transform in GetInvertibleTransforms())
		{
			var rect = new Rect(3, 5, 40, 60);
			var expected = transform.Inverse.TransformBounds(rect);

			Assert.IsTrue(transform.TryTransformBoundsInverse(rect, out var actual), $"{transform.GetType().Name} should be invertible");
			Assert.AreEqual(expected.X, actual.X, Tolerance, $"{transform.GetType().Name} X");
			Assert.AreEqual(expected.Y, actual.Y, Tolerance, $"{transform.GetType().Name} Y");
			Assert.AreEqual(expected.Width, actual.Width, Tolerance, $"{transform.GetType().Name} Width");
			Assert.AreEqual(expected.Height, actual.Height, Tolerance, $"{transform.GetType().Name} Height");
		}
	}

	[TestMethod]
	public void When_TryTransformInverse_Identity_Then_PointIsUnchanged()
	{
		var transform = new MatrixTransform { Matrix = Matrix.Identity };

		Assert.IsTrue(transform.TryTransformInverse(new Point(5, 7), out var point));
		Assert.AreEqual(5, point.X, Tolerance);
		Assert.AreEqual(7, point.Y, Tolerance);

		Assert.IsTrue(transform.TryTransformBoundsInverse(new Rect(1, 2, 3, 4), out var rect));
		Assert.AreEqual(new Rect(1, 2, 3, 4), rect);
	}

	[TestMethod]
	public void When_TryTransformInverse_NonInvertible_Then_ReturnsFalse()
	{
		// A zero scale collapses everything onto a single point: the matrix determinant is 0.
		var transform = new ScaleTransform { ScaleX = 0, ScaleY = 0 };

		Assert.IsFalse(transform.TryTransformInverse(new Point(5, 7), out var point));
		Assert.AreEqual(5, point.X, Tolerance, "the input point should be returned untouched");
		Assert.AreEqual(7, point.Y, Tolerance, "the input point should be returned untouched");

		Assert.IsFalse(transform.TryTransformBoundsInverse(new Rect(1, 2, 3, 4), out var rect));
		Assert.AreEqual(new Rect(1, 2, 3, 4), rect, "the input rect should be returned untouched");
	}

	[TestMethod]
	public void When_TryTransformInverse_NonInvertibleMatrix_Then_ReturnsFalse()
	{
		// Both rows are colinear, the matrix maps the plane onto a line.
		var transform = new MatrixTransform { Matrix = new Matrix(1, 2, 2, 4, 0, 0) };

		Assert.IsFalse(transform.TryTransformInverse(new Point(5, 7), out _));
		Assert.IsFalse(transform.TryTransformBoundsInverse(new Rect(1, 2, 3, 4), out _));
	}

	[TestMethod]
	public void When_Inverse_PublicApi_Then_BehaviorIsPreserved()
	{
		var identity = new MatrixTransform { Matrix = Matrix.Identity };
		Assert.AreSame(identity, identity.Inverse, "an identity transform is its own inverse");

		var transform = new MatrixTransform { Matrix = new Matrix(2, 0, 0, 4, 10, 20) };
		var inverse = transform.Inverse;
		Assert.IsInstanceOfType<MatrixTransform>(inverse);
		Assert.AreNotSame(transform, inverse);
		Assert.AreEqual(0.5, ((MatrixTransform)inverse).Matrix.M11, Tolerance);
		Assert.AreEqual(0.25, ((MatrixTransform)inverse).Matrix.M22, Tolerance);
	}

	[TestMethod]
	public void When_CustomGeneralTransform_Then_FallsBackOnInverseCore()
	{
		var transform = new CustomGeneralTransform(new TranslateTransform { X = 10, Y = 20 });

		Assert.IsTrue(transform.TryTransformInverse(new Point(5, 7), out var point));
		Assert.AreEqual(15, point.X, Tolerance);
		Assert.AreEqual(27, point.Y, Tolerance);

		Assert.IsTrue(transform.TryTransformBoundsInverse(new Rect(5, 7, 3, 4), out var rect));
		Assert.AreEqual(15, rect.X, Tolerance);
		Assert.AreEqual(27, rect.Y, Tolerance);
	}

	[TestMethod]
	public void When_CustomGeneralTransform_WithoutInverse_Then_ReturnsFalse()
	{
		var transform = new CustomGeneralTransform(null);

		Assert.IsFalse(transform.TryTransformInverse(new Point(5, 7), out var point));
		Assert.AreEqual(new Point(5, 7), point);

		Assert.IsFalse(transform.TryTransformBoundsInverse(new Rect(5, 7, 3, 4), out var rect));
		Assert.AreEqual(new Rect(5, 7, 3, 4), rect);
	}

	[TestMethod]
	public async Task When_NestedVisualTransforms_Then_TryTransformInverse_RoundTrips()
	{
		var inner = new Border
		{
			Width = 40,
			Height = 30,
			RenderTransform = new RotateTransform { Angle = 30 },
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top,
		};
		var middle = new Border
		{
			Child = inner,
			Margin = new Thickness(17, 23, 0, 0),
			RenderTransform = new ScaleTransform { ScaleX = 2, ScaleY = 3 },
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top,
		};
		var root = new Border
		{
			Child = middle,
			Width = 300,
			Height = 300,
			RenderTransform = new TranslateTransform { X = 11, Y = 13 },
		};

		try
		{
			await UITestHelper.Load(root);

			var toRoot = inner.TransformToVisual(root);
			var localPoint = new Point(7, 9);
			var rootPoint = toRoot.TransformPoint(localPoint);

			Assert.IsTrue(toRoot.TryTransformInverse(rootPoint, out var roundTripped));
			Assert.AreEqual(localPoint.X, roundTripped.X, 0.5);
			Assert.AreEqual(localPoint.Y, roundTripped.Y, 0.5);

			var rootBounds = toRoot.TransformBounds(new Rect(0, 0, 40, 30));
			var expectedBounds = toRoot.Inverse.TransformBounds(rootBounds);

			Assert.IsTrue(toRoot.TryTransformBoundsInverse(rootBounds, out var actualBounds));
			Assert.AreEqual(expectedBounds.X, actualBounds.X, Tolerance);
			Assert.AreEqual(expectedBounds.Y, actualBounds.Y, Tolerance);
			Assert.AreEqual(expectedBounds.Width, actualBounds.Width, Tolerance);
			Assert.AreEqual(expectedBounds.Height, actualBounds.Height, Tolerance);
		}
		finally
		{
			TestServices.WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaDesktop)]
	public void When_TryTransformInverse_Then_AllocatesLessThanInverse()
	{
		const int Iterations = 2_000;
		var transform = new MatrixTransform { Matrix = new Matrix(2, 0, 0, 4, 10, 20) };
		var point = new Point(5, 7);

		// Warm-up so the JIT and the first-access statics don't pollute the measurement.
		_ = transform.Inverse.TransformPoint(point);
		transform.TryTransformInverse(point, out _);

		var before = GC.GetAllocatedBytesForCurrentThread();
		for (var i = 0; i < Iterations; i++)
		{
			_ = transform.Inverse.TransformPoint(point);
		}
		var inverseAllocations = GC.GetAllocatedBytesForCurrentThread() - before;

		before = GC.GetAllocatedBytesForCurrentThread();
		for (var i = 0; i < Iterations; i++)
		{
			transform.TryTransformInverse(point, out _);
		}
		var directAllocations = GC.GetAllocatedBytesForCurrentThread() - before;

		Assert.IsGreaterThan(0, inverseAllocations, "GeneralTransform.Inverse is expected to allocate a MatrixTransform");
		Assert.IsLessThan(inverseAllocations / 10, directAllocations,
			$"TryTransformInverse allocated {directAllocations} bytes vs {inverseAllocations} bytes for Inverse.TransformPoint");
	}

	private static GeneralTransform[] GetInvertibleTransforms()
		=> new GeneralTransform[]
		{
			new TranslateTransform { X = 10, Y = -20 },
			new ScaleTransform { ScaleX = 2, ScaleY = 0.5 },
			new RotateTransform { Angle = 37 },
			new SkewTransform { AngleX = 12, AngleY = 4 },
			new MatrixTransform { Matrix = new Matrix(2, 0.5, -0.25, 3, 10, 20) },
			new CompositeTransform { TranslateX = 4, TranslateY = 8, ScaleX = 1.5, ScaleY = 2.5, Rotation = 15 },
		};
#endif
}

#if HAS_UNO
/// <summary>
/// A <see cref="GeneralTransform"/> that is not a <see cref="Transform"/>, used to validate that the
/// inverse fast path still honors a custom <see cref="GeneralTransform.InverseCore"/>.
/// </summary>
internal partial class CustomGeneralTransform : GeneralTransform
{
	private readonly GeneralTransform _inverse;

	public CustomGeneralTransform(GeneralTransform inverse) => _inverse = inverse;

	protected override GeneralTransform InverseCore => _inverse;
}
#endif
