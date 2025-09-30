using System;
using AwesomeAssertions.Execution;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Foundation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

#if __SKIA__
using SkiaSharp;
using Uno.Media;
#endif

using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Media
{
	[TestClass]
	[RunsOnUIThread]
	public class Given_Geometry
	{
		[TestMethod]
		public void RectangleGeometry_CheckBounds_CompositeTransform()
		{
			var geometry = new RectangleGeometry
			{
				Rect = new Rect(0, 0, 100, 100),
				Transform = new CompositeTransform { CenterX = 50, CenterY = 50, Rotation = 45 }
			};

			geometry.Bounds.Should().Be(new Rect(-20.7, -20.7, 141.4, 141.4), 0.1);
			WindowHelper.WindowContent = new PathIcon() { Data = geometry };
		}

		[TestMethod]
		public void RectangleGeometry_CheckBounds_TranslateTransform()
		{
			var geometry = new RectangleGeometry
			{
				Rect = new Rect(0, 0, 100, 100),
				Transform = new TranslateTransform { X = 20, Y = 40 }
			};

			geometry.Bounds.Should().Be(new Rect(20, 40, 100, 100));
		}

		[TestMethod]
		public void RectangleGeometry_CheckBounds_ScaleTransform()
		{
			var geometry = new RectangleGeometry
			{
				Rect = new Rect(0, 0, 100, 100),
				Transform = new ScaleTransform { CenterX = 50, CenterY = 150, ScaleX = 2, ScaleY = 0.5 }
			};

			geometry.Bounds.Should().Be(new Rect(-50, 75, 200, 50), 0.1);
		}

		[TestMethod]
		public void RectangleGeometry_CheckBounds_Origin()
		{
			var geometry = new RectangleGeometry
			{
				Rect = new Rect(100, 100, 100, 100)
			};

			geometry.Bounds.Should().Be(new Rect(100, 100, 100, 100), 0.1);
		}

		[TestMethod]
		public void RectangleGeometry_CheckBounds_Origin_CompositeTransform()
		{
			var geometry = new RectangleGeometry
			{
				Rect = new Rect(100, 100, 100, 100),
				Transform = new CompositeTransform { CenterX = 150, CenterY = 150, Rotation = 45 }
			};

			geometry.Bounds.Should().Be(new Rect(79.3, 79.3, 141.4, 141.4), 0.1);
		}

		[TestMethod]
		public void Composite_RectangleGeometry_CheckBounds_Origin()
		{
			var geometry1 = new RectangleGeometry
			{
				Rect = new Rect(100, 100, 100, 100)
			};
			var geometry2 = new RectangleGeometry
			{
				Rect = new Rect(10, 10, 10, 10)
			};

			var geometry = new GeometryGroup();
			geometry.Children.Add(geometry1);
			geometry.Children.Add(geometry2);

			using var _ = new AssertionScope();

			geometry1.Bounds.Should().Be(new Rect(100, 100, 100, 100), 0.1);
			geometry2.Bounds.Should().Be(new Rect(10, 10, 10, 10), 0.1);
			geometry.Bounds.Should().Be(new Rect(10, 10, 190, 190), 0.1);
		}

		[TestMethod]
		public void Composite_RectangleGeometry_CheckBounds_Origin_CompositeTransform()
		{
			var geometry1 = new RectangleGeometry
			{
				Rect = new Rect(100, 100, 100, 100),
				Transform = new CompositeTransform { CenterX = 150, CenterY = 150, Rotation = 45, TranslateX = 100, TranslateY = -100 }
			};
			var geometry2 = new RectangleGeometry
			{
				Rect = new Rect(200, 200, 100, 100),
				Transform = new CompositeTransform { CenterX = 350, CenterY = 350, ScaleX = 2, ScaleY = 0.5 }
			};

			var geometry = new GeometryGroup();
			geometry.Children.Add(geometry1);
			geometry.Children.Add(geometry2);

			using var _ = new AssertionScope();

			geometry1.Bounds.Should().Be(new Rect(179, -21, 141.5, 141.5), 0.5);
			geometry2.Bounds.Should().Be(new Rect(50, 275, 200, 50), 0.5);
			geometry.Bounds.Should().Be(new Rect(50, -21, 271, 346), 0.5);
		}

		[TestMethod]
		public void EmptyGeometryGroup_CheckBounds()
		{
			(new GeometryGroup()).Bounds.Should().Be(default(Rect));
		}

		[TestMethod]
		public void EmptyGeometry_CheckBounds()
		{
			// Catastrophic Failure on UWP
			Assert.Throws<Exception>(() => _ = Geometry.Empty.Bounds);
		}

#if __SKIA__
		[TestMethod]
		public void StreamGeometry_GetSKPath_CheckFillType()
		{
			var streamGeometry = new StreamGeometry();
			using (var context = streamGeometry.Open())
			{
				context.BeginFigure(new Point(0, 0), isFilled: true);
				context.LineTo(new Point(10, 10), isStroked: true, isSmoothJoin: true);
			}

			var skPath = streamGeometry.GetSKPath();
			skPath.FillType.Should().Be(SKPathFillType.EvenOdd);
		}
#endif
	}
}
