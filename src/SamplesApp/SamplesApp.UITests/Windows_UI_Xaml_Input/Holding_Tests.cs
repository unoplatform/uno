using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;
using Uno.UITests.Helpers;

namespace SamplesApp.UITests.Windows_UI_Xaml_Input
{
	public partial class Holding_Tests : SampleControlUITestBase
	{
		private const string _xamlTestPage = "UITests.Shared.Windows_UI_Input.GestureRecognizerTests.HoldingTests";

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.iOS)] // No WASM as Holding is not raise for mouse pointers, Android failing https://github.com/unoplatform/uno/issues/9080
		public void When_Basic()
		{
			Run(_xamlTestPage);

			const string targetName = "Basic_Target";
			const int tapX = 10, tapY = 10;

			// Tap and hold the target
			var target = _app.WaitForElement(targetName).Single().Rect;
			Hold(target.X + tapX, target.Y + tapY);

			var result = GestureResult.Get(_app.Marked("LastHeld"));
			result.Element.Should().Be(targetName);
			result.State.Should().Be("Completed");
			((int)result.X).Should().Be(tapX);
			((int)result.Y).Should().Be(tapY);
		}

		[Test]
		[AutoRetry]
		[Ignore("Currently there is no available gesture to test cancel")]
		[ActivePlatforms(Platform.Android, Platform.iOS)] // No WASM as Holding is not raise for mouse pointers
		public void When_Basic_Canceled()
		{
			Run(_xamlTestPage);

			const string targetName = "Basic_Target";
			const int tapX = 10, tapY = 10;

			// Tap and hold the target
			var target = _app.WaitForElement(targetName).Single().Rect;
			HoldAndCancel(target.X + tapX, target.Y + tapY);

			var result = GestureResult.Get(_app.Marked("LastHeld"));
			result.Element.Should().Be(targetName);
			result.State.Should().Be("Canceled");
			((int)result.X).Should().Be(tapX);
			((int)result.Y).Should().Be(tapY);
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.iOS)] // No WASM as Holding is not raise for mouse pointers + Disabled on Android: The test engine is not able to find "Transformed_Target"
		public void When_Transformed()
		{
			Run(_xamlTestPage);

			const string parentName = "Transformed_Parent";
			const string targetName = "Transformed_Target";

			var parent = _app.WaitForElement(parentName).Single().Rect;
			var target = _app.WaitForElement(targetName).Single().Rect;

			// Tap and hold the target
			Hold(parent.Right - target.Width, parent.Bottom - 3);

			var result = GestureResult.Get(_app.Marked("LastHeld"));
			result.Element.Should().Be(targetName);
			result.State.Should().Be("Completed");
		}

		[Test]
		[AutoRetry]
		[Ignore("Currently there is no available gesture to test cancel")]
		[ActivePlatforms(Platform.iOS)] // No WASM as Holding is not raise for mouse pointers + Disabled on Android: The test engine is not able to find "Transformed_Target"
		public void When_Transformed_Canceled()
		{
			Run(_xamlTestPage);

			const string parentName = "Transformed_Parent";
			const string targetName = "Transformed_Target";

			var parent = _app.WaitForElement(parentName).Single().Rect;
			var target = _app.WaitForElement(targetName).Single().Rect;

			// Tap and hold the target
			HoldAndCancel(parent.Right - target.Width, parent.Bottom - 3);

			var result = GestureResult.Get(_app.Marked("LastHeld"));
			result.Element.Should().Be(targetName);
			result.State.Should().Be("Canceled");
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.iOS)] // No WASM as Holding is not raise for mouse pointers, Android failing https://github.com/unoplatform/uno/issues/9080
		public void When_InScroll()
		{
			Run(_xamlTestPage);

			const string targetName = "InScroll_Target";
			const int tapX = 10, tapY = 10;

			// Scroll to make the target visible
			var scroll = _app.WaitForElement("InScroll_ScrollViewer").Single().Rect;
			_app.DragCoordinates(scroll.Right - 3, scroll.Bottom - 3, 0, 0);

			// Tap and hold the target
			var target = _app.WaitForElement(targetName).Single();
			Hold(target.Rect.X + tapX, target.Rect.Y + tapY);

			var result = GestureResult.Get(_app.Marked("LastHeld"));
			result.Element.Should().Be(targetName);
			result.State.Should().Be("Completed");
			((int)result.X).Should().Be(tapX);
			((int)result.Y).Should().Be(tapY);
		}

		[Test]
		[AutoRetry]
		[Ignore("Currently there is no available gesture to test cancel")]
		[ActivePlatforms(Platform.Android, Platform.iOS)] // No WASM as Holding is not raise for mouse pointers
		public void When_InScroll_Canceled()
		{
			Run(_xamlTestPage);

			const string targetName = "InScroll_Target";
			const int tapX = 10, tapY = 10;

			// Scroll to make the target visible
			var scroll = _app.WaitForElement("InScroll_ScrollViewer").Single().Rect;
			_app.DragCoordinates(scroll.Right - 3, scroll.Bottom - 3, 0, 0);

			// Tap and hold the target
			var target = _app.WaitForElement(targetName).Single();
			HoldAndCancel(target.Rect.X + tapX, target.Rect.Y + tapY);

			var result = GestureResult.Get(_app.Marked("LastHeld"));
			result.Element.Should().Be(targetName);
			result.State.Should().Be("Canceled");
			((int)result.X).Should().Be(tapX);
			((int)result.Y).Should().Be(tapY);
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.iOS)] // No WASM as Holding is not raise for mouse pointers, Android failing https://github.com/unoplatform/uno/issues/9080
		public void When_InListViewWithItemClick()
		{
			Run(_xamlTestPage);

			const string targetName = "ListViewWithItemClick";

			// Scroll a bit in the ListView
			var target = _app.WaitForElement(targetName).Single().Rect;
			_app.DragCoordinates(target.CenterX, target.Bottom - 3, target.CenterX, target.Y + 3);

			// Tap and hold an item
			Hold(target.CenterX, target.CenterY - 5);

			var result = GestureResult.Get(_app.Marked("LastHeld"));
			var expectedItem = AppInitializer.GetLocalPlatform() == Platform.Browser
				? "none" // Long press not supported with mouse
				: "Item_3";
			result.Element.Should().Be(expectedItem);
			result.State.Should().Be("Completed");
		}

		[Test]
		[AutoRetry]
		[Ignore("Currently there is no available gesture to test cancel")]
		[ActivePlatforms(Platform.Android, Platform.iOS)] // No WASM as Holding is not raise for mouse pointers
		public void When_InListViewWithItemClick_Canceled()
		{
			Run(_xamlTestPage);

			const string targetName = "ListViewWithItemClick";

			// Scroll a bit in the ListView
			var target = _app.WaitForElement(targetName).Single().Rect;
			_app.DragCoordinates(target.CenterX, target.Bottom - 3, target.CenterX, target.Y + 3);

			// Tap and hold an item
			HoldAndCancel(target.CenterX, target.CenterY - 5);

			var result = GestureResult.Get(_app.Marked("LastHeld"));
			var expectedItem = AppInitializer.GetLocalPlatform() == Platform.Browser
				? "none" // Long press not supported with mouse
				: "Item_3";
			result.Element.Should().Be(expectedItem);
			result.State.Should().Be("Canceled");
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.iOS)] // No WASM as Holding is not raise for mouse pointers, Android failing https://github.com/unoplatform/uno/issues/9080
		public void When_InListViewWithoutItemClick()
		{
			Run(_xamlTestPage);

			const string targetName = "ListViewWithoutItemClick";

			// Scroll a bit in the ListView
			var target = _app.WaitForElement(targetName).Single().Rect;
			_app.DragCoordinates(target.CenterX, target.Bottom - 3, target.CenterX, target.Y + 3);

			// Tap and hold an item
			Hold(target.CenterX, target.CenterY - 5);

			var result = GestureResult.Get(_app.Marked("LastHeld"));
			var expectedItem = AppInitializer.GetLocalPlatform() == Platform.Browser
				? "none" // Long press not supported with mouse
				: "Item_3";
			result.Element.Should().Be(expectedItem);
			result.State.Should().Be("Completed");
		}

		[Test]
		[AutoRetry]
		[Ignore("Currently there is no available gesture to test cancel")]
		[ActivePlatforms(Platform.Android, Platform.iOS)] // No WASM as Holding is not raise for mouse pointers
		public void When_InListViewWithoutItemClick_Canceled()
		{
			Run(_xamlTestPage);

			const string targetName = "ListViewWithoutItemClick";

			// Scroll a bit in the ListView
			var target = _app.WaitForElement(targetName).Single().Rect;
			_app.DragCoordinates(target.CenterX, target.Bottom - 3, target.CenterX, target.Y + 3);

			// Tap and hold an item
			HoldAndCancel(target.CenterX, target.CenterY - 5);

			var result = GestureResult.Get(_app.Marked("LastHeld"));
			var expectedItem = AppInitializer.GetLocalPlatform() == Platform.Browser
				? "none" // Long press not supported with mouse
				: "Item_3";
			result.Element.Should().Be(expectedItem);
			result.State.Should().Be("Canceled");
		}

		private void Hold(float x, float y)
		{
			// Included delay to accound for faster emulator on Android
			System.Threading.Thread.Sleep(1000);

			// Note:  We use DragCoordinates to simulate the real behavior of user which
			// moves a bit its finger over the screen
			_app.DragCoordinates(x, y, x + 2, y + 2);
		}

		private void HoldAndCancel(float x, float y)
		{
			// Included delay to accound for faster emulator on Android
			System.Threading.Thread.Sleep(1000);

			// Note:  We use DragCoordinates to simulate the real behavior of user which
			// moves a bit its finger over the screen
			_app.DragCoordinates(x, y, x + 40, y + 40);
		}
	}
}
