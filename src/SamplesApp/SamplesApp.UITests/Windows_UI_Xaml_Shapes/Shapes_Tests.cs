using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;
using Uno.UITests.Helpers;

namespace SamplesApp.UITests.Windows_UI_Xaml_Shapes
{
	public partial class Shapes_Tests : SampleControlUITestBase
	{
		[Test]
		[AutoRetry]
		public void Draw_polyline()
		{
			Run("SamplesApp.Windows_UI_Xaml_Shapes.PolylinePage");
			_app.WaitForElement("DPolyline");
			TakeScreenshot($"PolylinePage");
			TabWaitAndThenScreenshot("ChangeShape");

			void TabWaitAndThenScreenshot(string buttonName)
			{
				_app.Marked(buttonName).FastTap();
				_app.WaitForElement("DPolyline");
				TakeScreenshot($"PolylinePage - {buttonName}");
			}
		}

		[Test]
		[AutoRetry]
		public void Draw_polygon()
		{
			Run("SamplesApp.Windows_UI_Xaml_Shapes.PolygonPage");
			_app.WaitForElement("DPolygon");
			TakeScreenshot($"PolygonPage");
			TabWaitAndThenScreenshot("ChangeShape");

			void TabWaitAndThenScreenshot(string buttonName)
			{
				_app.Marked(buttonName).FastTap();
				_app.WaitForElement("DPolygon");
				TakeScreenshot($"PolygonPage - {buttonName}");
			}
		}

		[Test]
		[AutoRetry]
		public void Affect_Measurement_polygon()
		{
			Run("SamplesApp.Windows_UI_Xaml_Shapes.PolygonPage");

			_app.WaitForElement("DPolygon");
			_app.Marked("ClearShape").FastTap();
			TakeScreenshot($"PolygonPage - ClearShape");

			_app.Marked("ChangeShape").FastTap();
			var widthzize = _app.Query(_app.Marked("DPolygon")).First().Rect.Width;

			if (widthzize == 0)
				Assert.Fail("Shape not changed");

			TakeScreenshot($"PolygonPage - ChangeShape-After clear");
		}

		[Test]
		[AutoRetry]
		public void Affect_Measurement_polyline()
		{
			Run("SamplesApp.Windows_UI_Xaml_Shapes.PolylinePage");

			_app.WaitForElement("DPolyline");
			_app.Marked("ClearShape").FastTap();
			TakeScreenshot($"PolylinePage - ClearShape");

			_app.Marked("ChangeShape").FastTap();
			var widthzize = _app.Query(_app.Marked("DPolyline")).First().Rect.Width;

			if (widthzize == 0)
				Assert.Fail("Shape not changed");

			TakeScreenshot($"PolylinePage - ChangeShape-After clear");
		}

		[Test]
		[AutoRetry]
		public void Ellipse_Page()
		{
			Run("SamplesApp.Windows_UI_Xaml_Shapes.EllipsePage");
			_app.WaitForElement("ellipse0");

			var ellipse1 = _app.Marked("ellipse1").FirstResult().Rect;
			var ellipse2 = _app.Marked("ellipse2").FirstResult().Rect;
			var ellipse3 = _app.Marked("ellipse3").FirstResult().Rect;
			var ellipse4 = _app.Marked("ellipse4").FirstResult()?.Rect;

			ellipse2.Width.Should().Be(ellipse1.Width, "Invalid ellipse2 width");
			ellipse3.Width.Should().Be(ellipse1.Width, "Invalid ellipse3 width");
			ellipse4?.Width.Should().Be(0, "Invalid ellipse4 width");

			ellipse2.Height.Should().Be(ellipse1.Height, "Invalid ellipse2 height");
			ellipse3.Height.Should().Be(ellipse1.Height, "Invalid ellipse3 height");
			ellipse4?.Height.Should().Be(ellipse1.Height, "Invalid ellipse4 height");
		}

		[Test]
		[AutoRetry]
		public void Draw_line()
		{
			Run("SamplesApp.Windows_UI_Xaml_Shapes.LinePage");
			_app.WaitForElement("DLinePage");
			TakeScreenshot($"LinePage");
		}

		[Test]
		[AutoRetry]
		public void Check_Bound_Color()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Shapes.Rectangle_Color_Bound");

			_app.WaitForText("StatusTextBlock", "Is bound");

			var screenshot = TakeScreenshot("Complete");

			var bounds = _app.GetPhysicalRect("TargetRectangle");

			ImageAssert.HasColorAt(screenshot, bounds.CenterX, bounds.CenterY, Color.Blue);
		}

		[Test]
		[AutoRetry]
		public void Default_StrokeThickness()
		{
			const string red = "#FF0000";
			string reddish = GetReddish();

			var shapeExpectations = new[]
		   {
				new ShapeExpectation
				{
					Name = "MyLine",
					Offsets = new [] {0, 0, 0, 0},
					Colors = red,
				},
				new ShapeExpectation
				{
					Name = "MyRect",
					Offsets = new [] {0, 0, -1, -1},
					Colors = red,
				},
				new ShapeExpectation
				{
					Name = "MyPolyline",
					Offsets = new [] {2, 2, -1, -1},
					Colors = reddish,
				},
				new ShapeExpectation
				{
					Name = "MyPolygon",
					Offsets = new [] {2, 2, -1, -1},
					Colors = reddish,
				},
				new ShapeExpectation
				{
					Name = "MyEllipse",
					Offsets = new [] {0, 0, -1, -1},
					Colors = red,
				},
				new ShapeExpectation
				{
					Name = "MyPath",
					Offsets = new [] {0, 0, 0, 0},
					Colors = red,
				},
			};
			Run("UITests.Windows_UI_Xaml_Shapes.Shapes_Default_StrokeThickness");

			_app.WaitForElement("TestZone");

			foreach (var expectation in shapeExpectations)
			{
				_app.Marked($"{expectation.Name}Selector").FastTap();

				using var screenshot = TakeScreenshot($"{expectation}");
				if (expectation.Name == "MyLine" || expectation.Name == "MyPath")
				{
					var targetRect = _app.GetPhysicalRect($"{expectation.Name}Target");
					ImageAssert.DoesNotHaveColorAt(screenshot, targetRect.CenterX, targetRect.CenterY, Color.White);

					_app.Marked("StrokeThicknessButton").FastTap();

					using var zeroStrokeThicknessScreenshot = TakeScreenshot($"{expectation.Name}_0_StrokeThickness");
					ImageAssert.HasColorAt(zeroStrokeThicknessScreenshot, targetRect.CenterX, targetRect.CenterY, Color.White);
				}
				else
				{
					var shapeContainer = _app.GetPhysicalRect($"{expectation}Grid");

					ImageAssert.HasColorAt(screenshot, shapeContainer.X + expectation.Offsets[0], shapeContainer.CenterY, expectation.Colors, tolerance: 15);
					ImageAssert.HasColorAt(screenshot, shapeContainer.CenterX, shapeContainer.Y + expectation.Offsets[1], expectation.Colors, tolerance: 15);
					ImageAssert.HasColorAt(screenshot, shapeContainer.Right + expectation.Offsets[2], shapeContainer.CenterY, expectation.Colors, tolerance: 15);
					ImageAssert.HasColorAt(screenshot, shapeContainer.CenterX, shapeContainer.Bottom + expectation.Offsets[3], expectation.Colors, tolerance: 15);

					_app.Marked("StrokeThicknessButton").FastTap();

					using var zeroStrokeThicknessScreenshot = TakeScreenshot($"{expectation.Name}_0_StrokeThickness");

					ImageAssert.DoesNotHaveColorAt(zeroStrokeThicknessScreenshot, shapeContainer.X + expectation.Offsets[0], shapeContainer.CenterY, expectation.Colors);
					ImageAssert.DoesNotHaveColorAt(zeroStrokeThicknessScreenshot, shapeContainer.CenterX, shapeContainer.Y + expectation.Offsets[1], expectation.Colors);
					ImageAssert.DoesNotHaveColorAt(zeroStrokeThicknessScreenshot, shapeContainer.Right + expectation.Offsets[2], shapeContainer.CenterY, expectation.Colors);
					ImageAssert.DoesNotHaveColorAt(zeroStrokeThicknessScreenshot, shapeContainer.CenterX, shapeContainer.Bottom + expectation.Offsets[3], expectation.Colors);
				}
			}

		}

		[Test]
		[AutoRetry]
		public void Setting_ImageBrush_In_Code_Behind()
		{
			Run("UITests.Windows_UI_Xaml_Shapes.Setting_ImageBrush_In_Code_Behind");
			_app.FastTap("AssignBothImagesButton");
			var rect1 = _app.GetPhysicalRect("myShape1").ToRectangle();
			var rect2 = _app.GetPhysicalRect("myShape2").ToRectangle();
			using var screenshot = TakeScreenshot("Setting_ImageBrush_In_Code_Behind");
			ImageAssert.HasColorAt(screenshot, rect1.X, rect1.Y, Color.FromArgb(255, 236, 197, 175), tolerance: 5);
			ImageAssert.HasColorAt(screenshot, rect2.X, rect2.Y, Color.FromArgb(255, 236, 197, 175), tolerance: 5);
		}

		[Test]
		[AutoRetry]
		public void Shape_StrokeColor_ShouldRerenderWithChange()
		{
			Run("UITests.Windows_UI_Xaml_Shapes.Shape_StrokeTest");

			var rect = _app.GetPhysicalRect("TestTarget").ToRectangle();
			var center = rect.Location + new Size(rect.Size.Width / 2, rect.Size.Height / 2);
			var initialColor = Color.Red;
			var invertedColor = Color.FromArgb(255, 0, 255, 255); // UpdateBrushColor: flip (x => x^255) all rgb channels, except alpha

			// check color before
			using var before = TakeScreenshot("Shape_StrokeTest_Before");
			ImageAssert.HasColorAt(before, center.X, center.Y, initialColor);

			// update brush color
			_app.FastTap("UpdateBrushColorButton");

			// check color after
			using var after = TakeScreenshot("Shape_StrokeTest_After");
			ImageAssert.HasColorAt(after, center.X, center.Y, invertedColor);
		}

		private static string GetReddish() =>
			AppInitializer.GetLocalPlatform() switch
			{
				Platform.Browser => "#FF8080",
				Platform.Android => "#FF7F7F",
				_ => "#FF0000",
			};

		private struct ShapeExpectation
		{
			public string Name { get; set; }
			public int[] Offsets { get; set; }
			public string Colors { get; set; }

			public override string ToString() => $"{Name}";
		}

	}
}
