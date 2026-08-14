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
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
		public void EmptyGeometry_CheckBounds()
		{
			// PathGeometry.ComputeBounds is implemented under __SKIA__ only, so this test is restricted to Skia heads.
			// (Native WinUI also throws "Catastrophic Failure" on UWP/WinAppSDK for Geometry.Empty.Bounds.)
			Geometry.Empty.Bounds.Should().Be(default(Rect));
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
		public void Geometry_Empty_Returns_Empty_PathGeometry()
		{
			// Native WinUI's GeometryFactory::get_EmptyImpl creates the instance as a raw
			// ctl::ComObject<PathGeometry>, bypassing BetterCoreObjectActivationFactory and
			// the DXamlCore peer setup. Property accessors (get_Figures, get_Bounds, ...)
			// route through GetValueByKnownIndex, which needs the core peer and fails with
			// E_UNEXPECTED ("Catastrophic failure") on this naked instance. The reference
			// type is observable, but the empty-figures invariant can only be verified on
			// Uno's Skia implementation. Same root cause as EmptyGeometry_CheckBounds above.
			var empty = Geometry.Empty;

			Assert.IsInstanceOfType(empty, typeof(PathGeometry));
			Assert.AreEqual(0, ((PathGeometry)empty).Figures.Count);
		}

		[TestMethod]
		public void Geometry_Empty_Returns_Distinct_Instances()
		{
			Assert.AreNotSame(Geometry.Empty, Geometry.Empty);
		}

		[TestMethod]
		public void Geometry_StandardFlatteningTolerance_Is_QuarterPixel()
		{
			Assert.AreEqual(0.25, Geometry.StandardFlatteningTolerance);
		}

#if __SKIA__
		[TestMethod]
		public void StreamGeometry_GetGeometry_CheckFillType()
		{
			var streamGeometry = new StreamGeometry();
			using (var context = streamGeometry.Open())
			{
				context.BeginFigure(new Point(0, 0), isFilled: true);
				context.LineTo(new Point(10, 10), isStroked: true, isSmoothJoin: true);
			}

			// Backend-neutral: verify the default EvenOdd fill rule regardless of the active geometry backend.
			var geometry = streamGeometry.GetGeometry()!;
			if (geometry is Microsoft.UI.Composition.SkiaGeometrySource2D skiaGeometry)
			{
				skiaGeometry.Geometry.FillType.Should().Be(SKPathFillType.EvenOdd);
			}
			else if (geometry is Uno.UI.Composition.Drawing.ManagedGeometry managedGeometry)
			{
				managedGeometry.FillRule.Should().Be(Uno.UI.Composition.Drawing.GeometryFillRule.EvenOdd);
			}
			else
			{
				Assert.Fail($"Unexpected geometry backend type {geometry.GetType()}");
			}
		}

		[TestMethod]
		public void RectangleGeometry_Transform_Applies_To_Geometry()
		{
			var geometry = new RectangleGeometry
			{
				Rect = new Rect(0, 0, 100, 50),
				Transform = new TranslateTransform { X = 30, Y = 20 }
			};

			var untransformed = geometry.GetGeometry()!.Bounds;
			var transformed = geometry.GetTransformedGeometry()!.Bounds;

			// Untransformed geometry should have bounds at origin
			Assert.AreEqual(0, untransformed.Left, 0.1f);
			Assert.AreEqual(0, untransformed.Top, 0.1f);

			// Transformed geometry should be offset by the translation
			Assert.AreEqual(30, transformed.Left, 0.1f);
			Assert.AreEqual(20, transformed.Top, 0.1f);
			Assert.AreEqual(130, transformed.Right, 0.1f);
			Assert.AreEqual(70, transformed.Bottom, 0.1f);
		}

		[TestMethod]
		public void RectangleGeometry_NoTransform_Returns_Same_Geometry()
		{
			var geometry = new RectangleGeometry
			{
				Rect = new Rect(10, 20, 100, 50)
			};

			var untransformed = geometry.GetGeometry()!.Bounds;
			var transformed = geometry.GetTransformedGeometry()!.Bounds;

			// Without a transform, both should have the same bounds
			Assert.AreEqual(untransformed.Left, transformed.Left, 0.1f);
			Assert.AreEqual(untransformed.Top, transformed.Top, 0.1f);
			Assert.AreEqual(untransformed.Right, transformed.Right, 0.1f);
			Assert.AreEqual(untransformed.Bottom, transformed.Bottom, 0.1f);
		}

		[TestMethod]
		public void RectangleGeometry_Transform_Updates_TransformedGeometry()
		{
			var geometry = new RectangleGeometry
			{
				Rect = new Rect(0, 0, 100, 50),
				Transform = new TranslateTransform { X = 50, Y = 0 }
			};

			var bounds = geometry.GetTransformedGeometry()!.Bounds;

			// The transformed geometry should include the transform
			Assert.AreEqual(50, bounds.Left, 0.1f);
			Assert.AreEqual(150, bounds.Right, 0.1f);
		}

		[TestMethod]
		public void GlyphRun_Shaping_Produces_NonEmpty_Geometry()
		{
			// Exercises the font-shaping -> glyph-outline -> IGeometry path text rendering uses
			// (SkiaFont.BuildGlyphRunOutline).
			using var skFont = new SkiaSharp.SKFont { Size = 14 };
			const string text = "120.0";
			var glyphs = skFont.GetGlyphs(text);
			Assert.IsTrue(glyphs.Length > 0, "expected the font to produce glyphs");

			var positions = System.Runtime.InteropServices.MemoryMarshal.Cast<SkiaSharp.SKPoint, System.Numerics.Vector2>(
				skFont.GetGlyphPositions(text, new SkiaSharp.SKPoint(0, 0)));
			var font = new Uno.UI.Composition.Drawing.SkiaFont(skFont);
			using var geometry = font.BuildGlyphRunOutline(glyphs, positions, 0f);
			var bounds = geometry.Bounds;
			Assert.IsTrue(bounds.Width > 0 && bounds.Height > 0, $"expected non-empty glyph geometry, got {bounds}");
		}

		[TestMethod]
		public void ColorGlyph_Rasterizes_To_Image()
		{
			// Color glyphs (emoji: COLR/CBDT/sbix/SVG) have no outline; SkiaFont must rasterize them to images.
			using var typeface = SkiaSharp.SKFontManager.Default.MatchCharacter(0x1F600);
			if (typeface is null)
			{
				// No emoji font in this environment — nothing to exercise.
				return;
			}

			using var skFont = new SkiaSharp.SKFont(typeface, 32);
			var glyphs = skFont.GetGlyphs("\U0001F600"); // grinning face
			if (glyphs.Length == 0 || glyphs[0] == 0)
			{
				return; // the matched font doesn't actually carry this emoji glyph
			}

			var font = new Uno.UI.Composition.Drawing.SkiaFont(skFont);
			var positions = System.Runtime.InteropServices.MemoryMarshal.Cast<SkiaSharp.SKPoint, System.Numerics.Vector2>(
				skFont.GetGlyphPositions("\U0001F600", new SkiaSharp.SKPoint(0, 0)));

			if (!font.HasColorGlyphs)
			{
				return; // matched a non-color font
			}

			var images = new System.Collections.Generic.List<Uno.UI.Composition.Drawing.PositionedGlyphImage>();
			using (font.BuildGlyphRunOutline(glyphs, positions, 0f, images)) { }

			Assert.IsTrue(images.Count > 0, "expected the color emoji glyph to rasterize to at least one image");
			Assert.IsTrue(images[0].Width > 0 && images[0].Height > 0, "expected the rasterized glyph image to have a positive size");

			foreach (var g in images)
			{
				g.Image.Dispose();
			}
		}
#endif
	}
}
