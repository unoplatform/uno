using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.Windows_UI_Xaml_Input
{
	public class RightTapped_Tests : SampleControlUITestBase
	{
		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Android, Platform.iOS)] // We cannot test right button click on WASM yet
		public void When_Basic()
		{
			Run("UITests.Shared.Windows_UI_Input.GestureRecognizerTests.RightTappedTests");

			const string targetName = "Basic_Target";
			const int tapX = 10, tapY = 10;

			// Tap and hold the target
			var target = _app.WaitForElement(targetName).Single().Rect;
			_app.TouchAndHoldCoordinates(target.X + tapX, target.Y + tapY);

			var result = _app.Marked("LastRightTapped").GetDependencyPropertyValue<string>("Text");
			result.Should().Be(FormattableString.Invariant($"{targetName}@{tapX:F2},{tapY:F2}"));
		}


		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Android, Platform.iOS)] // We cannot test right button click on WASM yet
		public void When_WithTransformed()
		{
			Run("UITests.Shared.Windows_UI_Input.GestureRecognizerTests.RightTappedTests");

			const string parentName = "Transformed_Parent", targetName = "Transformed_Target";

			var parent = _app.WaitForElement(parentName).Single().Rect;
			var target = _app.WaitForElement(targetName).Single().Rect;

			// Tap and hold the target
			_app.TouchAndHoldCoordinates(parent.Right - target.Width, parent.Bottom - 3);

			var result = _app.Marked("LastRightTapped").GetDependencyPropertyValue<string>("Text");
			result.Should().StartWith(targetName);
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Android, Platform.iOS)] // We cannot test right button click on WASM yet
		public void When_InScroll()
		{
			Run("UITests.Shared.Windows_UI_Input.GestureRecognizerTests.RightTappedTests");

			const string targetName = "InScroll_Target";
			const int tapX = 10, tapY = 10;

			// Scroll to make the target visible
			var scroll = _app.WaitForElement("InScroll_ScrollViewer").Single().Rect;
			_app.DragCoordinates(scroll.Right - 3, scroll.Bottom - 3, 0, 0);

			// Tap and hold the target
			var target = _app.WaitForElement(targetName).Single();
			_app.TouchAndHoldCoordinates(target.Rect.X + tapX, target.Rect.Y + tapY);

			var result = _app.Marked("LastRightTapped").GetDependencyPropertyValue<string>("Text");
			result.Should().Be(FormattableString.Invariant($"{targetName}@{tapX:F2},{tapY:F2}"));
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Android, Platform.iOS)] // We cannot test right button click on WASM yet
		public void When_InListViewWithItemClick()
		{
			Run("UITests.Shared.Windows_UI_Input.GestureRecognizerTests.RightTappedTests");

			const string targetName = "ListViewWithItemClick";

			// Scroll a bit in the ListView
			var target = _app.WaitForElement(targetName).Single().Rect;
			_app.DragCoordinates(target.CenterX, target.Bottom - 3, target.CenterX, target.Y + 3);

			// Tap and hold an item
			_app.TouchAndHoldCoordinates(target.CenterX, target.CenterY);

			var result = _app.Marked("LastRightTapped").GetDependencyPropertyValue<string>("Text");
			result.Should().Be("__none__"); // Gesture was muted by the item click!
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Android, Platform.iOS)] // We cannot test right button click on WASM yet
		public void When_InListViewWithoutItemClick()
		{
			Run("UITests.Shared.Windows_UI_Input.GestureRecognizerTests.RightTappedTests");

			const string targetName = "ListViewWithoutItemClick";

			// Scroll a bit in the ListView
			var target = _app.WaitForElement(targetName).Single().Rect;
			_app.DragCoordinates(target.CenterX, target.Bottom - 3, target.CenterX, target.Y + 3);

			// Tap and hold an item
			_app.TouchAndHoldCoordinates(target.CenterX, target.CenterY);

			var result = _app.Marked("LastRightTapped").GetDependencyPropertyValue<string>("Text");
			result.Should().StartWith("Item_3");
		}

	}
}
