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
	[Ignore("DoubleTapCoordinates is not implemented yet")] 
	public class DoubleTapped_Tests : SampleControlUITestBase
	{
		private const string _xamlTestPage = "UITests.Shared.Windows_UI_Input.GestureRecognizerTests.DoubleTappedTests";

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Android, Platform.iOS)] // We cannot test right button click on WASM yet
		public void When_Basic()
		{
			Run(_xamlTestPage);

			const string targetName = "Basic_Target";
			const int tapX = 10, tapY = 10;

			// Double tap the target
			var target = _app.WaitForElement(targetName).Single().Rect;
			_app.DoubleTapCoordinates(target.X + tapX, target.Y + tapY);

			var result = _app.Marked("LastDoubleTapped").GetDependencyPropertyValue<string>("Text");
			result.Should().Be(FormattableString.Invariant($"{targetName}@{tapX:F2},{tapY:F2}"));
		}


		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Android, Platform.iOS)] // We cannot test right button click on WASM yet
		public void When_Transformed()
		{
			Run(_xamlTestPage);

			const string parentName = "Transformed_Parent";
			const string targetName = "Transformed_Target";

			var parent = _app.WaitForElement(parentName).Single().Rect;
			var target = _app.WaitForElement(targetName).Single().Rect;

			// Double tap the target
			_app.DoubleTapCoordinates(parent.Right - target.Width, parent.Bottom - 3);

			var result = _app.Marked("LastDoubleTapped").GetDependencyPropertyValue<string>("Text");
			result.Should().StartWith(targetName);
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Android, Platform.iOS)] // We cannot test right button click on WASM yet
		public void When_InScroll()
		{
			Run(_xamlTestPage);

			const string targetName = "InScroll_Target";
			const int tapX = 10, tapY = 10;

			// Scroll to make the target visible
			var scroll = _app.WaitForElement("InScroll_ScrollViewer").Single().Rect;
			_app.DragCoordinates(scroll.Right - 3, scroll.Bottom - 3, 0, 0);

			// Double tap the target
			var target = _app.WaitForElement(targetName).Single();
			_app.DoubleTapCoordinates(target.Rect.X + tapX, target.Rect.Y + tapY);

			var result = _app.Marked("LastDoubleTapped").GetDependencyPropertyValue<string>("Text");
			result.Should().Be(FormattableString.Invariant($"{targetName}@{tapX:F2},{tapY:F2}"));
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Android, Platform.iOS)] // We cannot test right button click on WASM yet
		public void When_InListViewWithItemClick()
		{
			Run(_xamlTestPage);

			const string targetName = "ListViewWithItemClick";

			// Scroll a bit in the ListView
			var target = _app.WaitForElement(targetName).Single().Rect;
			_app.DragCoordinates(target.CenterX, target.Bottom - 3, target.CenterX, target.Y + 3);

			// Tap and hold an item
			_app.DoubleTapCoordinates(target.CenterX, target.CenterY);

			var result = _app.Marked("LastDoubleTapped").GetDependencyPropertyValue<string>("Text");
			result.Should().StartWith("Item_3");
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Android, Platform.iOS)] // We cannot test right button click on WASM yet
		public void When_InListViewWithoutItemClick()
		{
			Run(_xamlTestPage);

			const string targetName = "ListViewWithoutItemClick";

			// Scroll a bit in the ListView
			var target = _app.WaitForElement(targetName).Single().Rect;
			_app.DragCoordinates(target.CenterX, target.Bottom - 3, target.CenterX, target.Y + 3);

			// Tap and hold an item
			_app.DoubleTapCoordinates(target.CenterX, target.CenterY);

			var result = _app.Marked("LastDoubleTapped").GetDependencyPropertyValue<string>("Text");
			result.Should().StartWith("Item_3");
		}
	}
}
