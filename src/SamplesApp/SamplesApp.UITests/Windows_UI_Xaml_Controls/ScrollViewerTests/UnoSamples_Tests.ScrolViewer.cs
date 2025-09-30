using System;
using System.Collections.Generic;
using System.Diagnostics.SymbolStore;
using System.Drawing;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AwesomeAssertions.Execution;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.Windows_UI_Xaml_Controls.ScrollViewerTests
{
	[TestFixture]
	public partial class UnoSamples_Tests : SampleControlUITestBase
	{
		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.iOS, Platform.Android)] // IsIntermediate is not supported on Wasm yet
		public void ScrollViewer_WhenSync_RunNormalAndCompletesWithNonIntermediate()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.ScrollViewerTests.ScrollViewer_UpdatesMode");

			var sut = _app.WaitForElement(_app.Marked("_scroll")).Single();
			var selectMode = _app.Marked("_setSync");
			var validate = _app.Marked("_validate");
			var result = _app.Marked("_result");

			selectMode.FastTap();

			//Drag upward
			_app.DragCoordinates(sut.Rect.X + 10, sut.Rect.Y + 110, sut.Rect.X + 10, sut.Rect.Y + 10);

			validate.FastTap();
			TakeScreenshot("Result", ignoreInSnapshotCompare: true);
			_app.WaitForDependencyPropertyValue(result, "Text", "SUCCESS");
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.iOS, Platform.Android)] // IsIntermediate is not supported on Wasm yet
		public void ScrollViewer_WhenAsync_RunIdleAndCompletesWithNonIntermediate()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.ScrollViewerTests.ScrollViewer_UpdatesMode");

			var sut = _app.WaitForElement(_app.Marked("_scroll")).Single();
			var selectMode = _app.Marked("_setAsync");
			var validate = _app.Marked("_validate");
			var result = _app.Marked("_result");

			selectMode.FastTap();

			//Drag upward
			_app.DragCoordinates(sut.Rect.X + 10, sut.Rect.Y + 110, sut.Rect.X + 10, sut.Rect.Y + 10);

			validate.FastTap();
			TakeScreenshot("Result", ignoreInSnapshotCompare: true);
			_app.WaitForDependencyPropertyValue(result, "Text", "SUCCESS");
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.iOS, Platform.Android)] // IsIntermediate is not supported on Wasm yet
		public void ScrollViewer_Clipping()
		{
			Run("UITests.Windows_UI_Xaml_Controls.ScrollViewerTests.ScrollViewer_Clipping");

			var sut = _app.WaitForElement(_app.Marked("scrollViewer1")).Single();
			var updateButton = _app.Marked("UpdateButton");

			var updateButtonRect = _app.WaitForElement(_app.Marked("intentionallyBlank")).Single().Rect;

			updateButton.FastTap();

			//Drag upward
			_app.DragCoordinates(sut.Rect.CenterX, sut.Rect.CenterY, sut.Rect.CenterX, sut.Rect.CenterY - (sut.Rect.Height / 2));

			using var res = TakeScreenshot("Result", ignoreInSnapshotCompare: true);

			ImageAssert.AreNotEqual(
				expected: res,
				expectedRect: new Rectangle((int)sut.Rect.X, (int)sut.Rect.Bottom - 20, (int)updateButtonRect.Width, (int)updateButtonRect.Height),
				actual: res,
				actualRect: new Rectangle((int)updateButtonRect.X, (int)updateButtonRect.Y, (int)updateButtonRect.Width, (int)updateButtonRect.Height)
			);
		}

		[Test]
		[AutoRetry]
		// Issue needs to be fixed first for WASM for Right and Bottom Margin missing
		// Details here: https://github.com/unoplatform/uno/issues/7000
		[ActivePlatforms(Platform.iOS, Platform.Android)]
		public void ScrollViewer_Content_Margin()
		{
			Run("UITests.Windows_UI_Xaml_Controls.ScrollViewerTests.ScrollViewer_Content_Margin");
			_app.WaitForElement("ChildStatusTextBlock");
			var rect = _app.GetPhysicalRect("OuterBorder");
			var outsideColor = Color.LightBlue;
			var insideColor = Color.Pink;

			AssertCurrentColors("Before-Scrolled", insideColor, outsideColor, outsideColor, outsideColor, insideColor);

			_app.FastTap("ScrollToRightBottomButton");
			_app.Wait(TimeSpan.FromSeconds(5));
			_app.WaitForText("ChildStatusTextBlock", "Scrolled");

			AssertCurrentColors("After-Scrolled", insideColor, insideColor, outsideColor, outsideColor, outsideColor);

			void AssertCurrentColors(string description, Color centerColor, Color topLeftColor, Color topRightColor, Color bottomLeftColor, Color bottomRightColor)
			{
				var screenshot = TakeScreenshot(description);

				// Top / Left
				ImageAssert.HasColorAt(screenshot, rect.X, rect.Y, topLeftColor);
				// Top / Right
				ImageAssert.HasColorAt(screenshot, rect.Right - LogicalToPhysical(1), rect.Y, topRightColor);
				// Center
				ImageAssert.HasColorAt(screenshot, rect.CenterX, rect.CenterY, centerColor);
				// Bottom / Left
				ImageAssert.HasColorAt(screenshot, rect.X, rect.Bottom - LogicalToPhysical(1), bottomLeftColor);
				// Bottom / Right
				ImageAssert.HasColorAt(screenshot, rect.Right - LogicalToPhysical(1), rect.Bottom - LogicalToPhysical(1), bottomRightColor);
			}
		}

		[Test]
		[AutoRetry]
		public void ScrollViewer_Removed_And_Added()
		{
			Run("UITests.Windows_UI_Xaml_Controls.ScrollViewerTests.ScrollViewer_Add_Remove");
			_app.WaitForElement("ViewfinderRectangle");
			var rect = _app.GetPhysicalRect("ViewfinderRectangle");
			AssertCurrentColor("Initial", Color.Red);

			AdvanceToStep(buttonName: "ScrollToBottomButton", stepName: "Scrolled");
			AssertCurrentColor("Initial-Scrolled", Color.Indigo);

			AdvanceToStep(buttonName: "YoinkButton", stepName: "Gone");
			AssertCurrentColor("Gone", Color.Beige);

			AdvanceToStep(buttonName: "YoinkButton", stepName: "Present");
			AssertCurrentColor("Restored", Color.Indigo);

			void AdvanceToStep(string buttonName, string stepName)
			{
				_app.FastTap(buttonName);
				_app.WaitForText("ChildStatusTextBlock", stepName);
			}

			void AssertCurrentColor(string description, Color color)
			{
				var scrn = TakeScreenshot(description);
				ImageAssert.HasColorAt(scrn, rect.CenterX, rect.CenterY, color);
			}
		}

		[Test]
		[AutoRetry]
		public void ScrollViewer_Margin()
		{
			Run("UITests.Windows_UI_Xaml_Controls.ScrollViewerTests.ScrollViewer_Margin");

			_app.Marked("shapeWidth").SetDependencyPropertyValue("Value", "80");
			_app.Marked("shapeHeight").SetDependencyPropertyValue("Value", "80");

			_app.WaitForElement("ctl5");

			using var screenshot = TakeScreenshot("test", ignoreInSnapshotCompare: true);

			for (byte i = 1; i <= 5; i++)
			{
				using var _ = new AssertionScope();

				var logicalRect = _app.GetLogicalRect($"ctl{i}");
				logicalRect.Width.Should().Be(80);
				logicalRect.Height.Should().Be(80);

				var physicalRect = _app.GetPhysicalRect($"ctl{i}");

				// Left / Top
				ImageAssert.HasColorAt(screenshot, physicalRect.X, physicalRect.Y, Color.Blue);
				ImageAssert.HasColorAt(screenshot, physicalRect.X + LogicalToPhysical(1), physicalRect.Y + LogicalToPhysical(1), Color.Blue);
				ImageAssert.HasColorAt(screenshot, physicalRect.CenterX, physicalRect.Y + 21, Color.Orange);
				ImageAssert.HasColorAt(screenshot, physicalRect.X + LogicalToPhysical(11), physicalRect.Y + LogicalToPhysical(11), Color.Orange);
				ImageAssert.HasColorAt(screenshot, physicalRect.X + LogicalToPhysical(23), physicalRect.Y + LogicalToPhysical(23), Color.Red);
				// Right / Top
				ImageAssert.HasColorAt(screenshot, physicalRect.Right - LogicalToPhysical(1), physicalRect.Y, Color.Blue);
				ImageAssert.HasColorAt(screenshot, physicalRect.Right - LogicalToPhysical(2), physicalRect.Y + LogicalToPhysical(1), Color.Blue);
				ImageAssert.HasColorAt(screenshot, physicalRect.Right - LogicalToPhysical(21), physicalRect.CenterY, Color.Orange);
				ImageAssert.HasColorAt(screenshot, physicalRect.Right - LogicalToPhysical(12), physicalRect.Y + LogicalToPhysical(11), Color.Orange);
				ImageAssert.HasColorAt(screenshot, physicalRect.Right - LogicalToPhysical(24), physicalRect.Y + LogicalToPhysical(23), Color.Red);
				// Middle
				ImageAssert.HasColorAt(screenshot, physicalRect.CenterX, physicalRect.CenterY, Color.Red);
				// Left / Bottom
				ImageAssert.HasColorAt(screenshot, physicalRect.X, physicalRect.Bottom - LogicalToPhysical(1), Color.Blue);
				ImageAssert.HasColorAt(screenshot, physicalRect.X + LogicalToPhysical(1), physicalRect.Bottom - LogicalToPhysical(2), Color.Blue);
				ImageAssert.HasColorAt(screenshot, physicalRect.X + LogicalToPhysical(21), physicalRect.CenterY, Color.Orange);
				ImageAssert.HasColorAt(screenshot, physicalRect.X + LogicalToPhysical(11), physicalRect.Bottom - LogicalToPhysical(12), Color.Orange);
				ImageAssert.HasColorAt(screenshot, physicalRect.X + LogicalToPhysical(23), physicalRect.Bottom - LogicalToPhysical(24), Color.Red);
				// Right / Bottom
				ImageAssert.HasColorAt(screenshot, physicalRect.Right - LogicalToPhysical(1), physicalRect.Bottom - LogicalToPhysical(1), Color.Blue);
				ImageAssert.HasColorAt(screenshot, physicalRect.Right - LogicalToPhysical(2), physicalRect.Bottom - LogicalToPhysical(2), Color.Blue);
				ImageAssert.HasColorAt(screenshot, physicalRect.CenterX, physicalRect.Bottom - LogicalToPhysical(21), Color.Orange);
				ImageAssert.HasColorAt(screenshot, physicalRect.Right - LogicalToPhysical(12), physicalRect.Bottom - LogicalToPhysical(12), Color.Orange);
				ImageAssert.HasColorAt(screenshot, physicalRect.Right - LogicalToPhysical(24), physicalRect.Bottom - LogicalToPhysical(24), Color.Red);

			}
		}

		[Test]
		[AutoRetry]
		public void ScrollViewer_Margin_Centered()
		{
			Run("UITests.Windows_UI_Xaml_Controls.ScrollViewerTests.ScrollViewer_Margin_Centered");

			_app.Marked("shapeWidth").SetDependencyPropertyValue("Value", "100");
			_app.Marked("shapeHeight").SetDependencyPropertyValue("Value", "340");

			_app.WaitForElement("ctl2");

			using var screenshot = TakeScreenshot("test", ignoreInSnapshotCompare: true);

			for (byte i = 1; i <= 2; i++)
			{
				using var _ = new AssertionScope();

				var logicalRect = _app.GetLogicalRect($"ctl{i}");
				logicalRect.Width.Should().Be(100);
				logicalRect.Height.Should().Be(340);

				var physicalRect = _app.GetPhysicalRect($"ctl{i}");

				// Left / Top
				ImageAssert.HasColorAt(screenshot, physicalRect.X + LogicalToPhysical(10), physicalRect.Y + LogicalToPhysical(10), Color.Orange);
				ImageAssert.HasColorAt(screenshot, physicalRect.X + LogicalToPhysical(20), physicalRect.Y + LogicalToPhysical(20), Color.Red);
				// Right / Top
				ImageAssert.HasColorAt(screenshot, physicalRect.Right - LogicalToPhysical(11), physicalRect.Y + LogicalToPhysical(10), Color.Orange);
				ImageAssert.HasColorAt(screenshot, physicalRect.Right - LogicalToPhysical(21), physicalRect.Y + LogicalToPhysical(20), Color.Red);
				// Middle
				ImageAssert.HasColorAt(screenshot, physicalRect.CenterX, physicalRect.CenterY, Color.Red);
				// Left / Bottom
				ImageAssert.HasColorAt(screenshot, physicalRect.X + LogicalToPhysical(10), physicalRect.Bottom - LogicalToPhysical(11), Color.Orange);
				ImageAssert.HasColorAt(screenshot, physicalRect.X + LogicalToPhysical(20), physicalRect.Bottom - LogicalToPhysical(21), Color.Red);
				// Right / Bottom
				ImageAssert.HasColorAt(screenshot, physicalRect.Right - LogicalToPhysical(11), physicalRect.Bottom - LogicalToPhysical(11), Color.Orange);
				ImageAssert.HasColorAt(screenshot, physicalRect.Right - LogicalToPhysical(21), physicalRect.Bottom - LogicalToPhysical(21), Color.Red);
			}
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Browser)] // Only applicable for mouse input since we're testing mouse-over state
		public void ScrollViewer_Fluent_ScrollBar_Appears()
		{
			Run("UITests.Windows_UI_Xaml_Controls.ScrollViewerTests.ScrollViewer_Fluent");

			_app.WaitForElement("ScrollViewerVisibilityCheckBox");

			_app.FastTap("ScrollViewerVisibilityCheckBox");

			_app.WaitForText("ScrollViewerVisibilityTextBlock", "True");

			var rect = _app.GetPhysicalRect("HostBorder");

			using var noScrollIndicator = TakeScreenshot("No scroll indicators");

			_app.FastTap("ButtonInScrollViewer"); // Put pointer over ScrollViewer so scroll bars appear

			_app.WaitForText("ButtonStatusTextBlock", "Clicked");

			using var scrollIndicator = TakeScreenshot("Scroll indicators visible"); // If this takes a *really* long time the scroll indicators might have
																					 // disappeared already... hopefully that doesn't happen

			ImageAssert.AreNotEqual(noScrollIndicator, scrollIndicator, rect);

		}
	}
}
