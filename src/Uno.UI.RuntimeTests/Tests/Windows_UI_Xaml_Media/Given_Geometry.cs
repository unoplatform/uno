using System;
using FluentAssertions.Execution;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Foundation;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Uno.UI.RuntimeTests.Helpers;
using static Private.Infrastructure.TestServices;
using System.Threading.Tasks;
using Windows.Foundation.Metadata;
using Windows.UI.Xaml;
using Uno.UI.RuntimeTests.Extensions;

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
		[ExpectedException(typeof(Exception))] // Catastrophic Failure on UWP
		public void EmptyGeometry_CheckBounds()
		{
			Console.WriteLine(Geometry.Empty.Bounds);
		}

		[TestMethod]
		public async Task When_EllipseGeometry()
		{
#if __MACOS__
			Assert.Inconclusive(); // Test was not implemented on that platforms
#endif
#if __IOS__
			Assert.Inconclusive(); // Test was not implemented on that platforms
#endif
#if __ANDROID__
			Assert.Inconclusive(); // Test was not implemented on that platforms
#endif

			if (!ApiInformation.IsTypePresent("Windows.UI.Xaml.Media.Imaging.RenderTargetBitmap"))
			{
				Assert.Inconclusive(); // System.NotImplementedException: RenderTargetBitmap is not supported on this platform.;
			}

			const string Red = "#FFFF0000";
			var SUT = new Path_Ellipse_Geometry();
			
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			var scrn = await RawBitmap.TakeScreenshot(SUT);

			var rect = SUT.GetRelativeCoords(SUT.HostBorder);
			await WindowHelper.WaitForIdle();
			ImageAssert.HasColorAt(scrn, rect.CenterX, rect.CenterY, Red, tolerance: 10);
		}

		[TestMethod] 
		public async Task When_LineGeometry()
		{
			if (!ApiInformation.IsTypePresent("Windows.UI.Xaml.Media.Imaging.RenderTargetBitmap"))
			{
				Assert.Inconclusive(); // System.NotImplementedException: RenderTargetBitmap is not supported on this platform.;
			}

			const string Green = "#FF008000";
			var SUT = new Path_LineGeometry();
			var scrn = await RawBitmap.TakeScreenshot(SUT);

			var rect = SUT.GetRelativeCoords(SUT.HostBorder);
			ImageAssert.HasColorAt(scrn, rect.CenterX, rect.CenterY, Green, tolerance: 10);
		}
	}
}
