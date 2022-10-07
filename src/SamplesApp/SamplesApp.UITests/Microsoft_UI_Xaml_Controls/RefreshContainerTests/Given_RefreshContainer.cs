using System;
using System.Collections.Generic;
using System.Drawing;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.Microsoft_UI_Xaml_Controls.RefreshContainerTests
{
	public partial class Given_RefreshContainer : SampleControlUITestBase
	{
		[Test]
		[AutoRetry]
		public void DoesNotInterfereWithHorizontalDrag()
		{
			Run("UITests.Microsoft_UI_Xaml_Controls.RefreshContainerTests.RefreshContainerHorizontalScroll");

			var refreshContainerResult = _app.WaitForElement("RefreshContainer");
			var refreshContainer = _app.Marked("RefreshContainer");
			var containerRect = ToPhysicalRect(refreshContainerResult[0].Rect).ToRectangle();
			var containerCenter = new Point(containerRect.Left + containerRect.Width / 2, containerRect.Top + containerRect.Height / 2);

			using var beforeScreenshot = TakeScreenshot("Before horizontal scroll", new ScreenshotOptions() { IgnoreInSnapshotCompare = true });
			ImageAssert.HasColorAt(beforeScreenshot, containerCenter.X, containerCenter.Y, Color.Red);

			_app.TapCoordinates(
				(float)(containerRect.Left + containerRect.Width * 0.9),
				(float)(containerRect.Top + containerRect.Height / 2));
			
			// Imperfect horizontal drag with vertical offset
			_app.DragCoordinates(
				(float)(containerRect.Left + containerRect.Width * 0.9),
				(float)(containerRect.Top + containerRect.Height / 2),
				(float)(containerRect.Left + containerRect.Width * 0.2),
				(float)(containerRect.Top + containerRect.Height * 0.8));			

			using var afterScreenshot = TakeScreenshot("After horizontal scroll", new ScreenshotOptions() { IgnoreInSnapshotCompare = true });
			ImageAssert.DoesNotHaveColorAt(afterScreenshot, containerCenter.X, containerCenter.Y, Color.Red);
		}
	}
}
