#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.Windows_UI_Xaml.DragAndDropTests
{
	public partial class DragDrop_Nested_Automated : SampleControlUITestBase
	{
		private static readonly Regex _logEntry = new Regex(@"^\s*\[(?<element>\w+)\] (?<event>[A-Z]+) (\<(?<data>[\w\|]*)\>|(?<result>\w+))\s*$");

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Android | Platform.iOS)] // Starting and completed might not be raised by the WASM test engine
		public void When_DragElementNestedInDraggableElement() => RunTest();

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Android | Platform.iOS)] // Starting and completed might not be raised by the WASM test engine
		public void When_DragElementNestedInDraggableElement_And_GoAway_Then_GetLeave() => RunTest();

		private void RunTest([CallerMemberName] string? testName = null)
		{
			Run("UITests.Windows_UI_Xaml.DragAndDrop.DragDrop_Nested", skipInitialScreenshot: true);

			var goAway = testName!.Contains("And_GoAway");

			var automated = _app.Marked("Automated");
			var source = _app.Marked("NestedDragSource");
			var target = _app.Marked("NestedDropTarget");
			var parentTarget = _app.Marked("ParentDropTarget");
			var output = _app.Marked("Output");

			_app.WaitForElement(automated);

			var automatedBounds = _app.Query(automated).Single().Rect;
			var sourceBounds = _app.Query(source).Single().Rect;
			var targetBounds = _app.Query(target).Single().Rect;
			var parentTargetBounds = _app.Query(parentTarget).Single().Rect;

			_app.TapCoordinates(automatedBounds.X + 15, automatedBounds.CenterY);
			_app.DragCoordinates(sourceBounds.CenterX, sourceBounds.CenterY, targetBounds.CenterX, goAway ? parentTargetBounds.Bottom + 10 : targetBounds.CenterY);

			TakeScreenshot("Result", ignoreInSnapshotCompare: true);

			var raw = output.GetDependencyPropertyValue<string>("Text");
			var logEntries = raw
				.Split(new[] {'\r', '\n'}, StringSplitOptions.RemoveEmptyEntries)
				.Select(op =>
				{
					var match = _logEntry.Match(op);

					return (
						raw: op,
						succes: match.Success,
						element: match.Groups["element"].Value,
						@event: match.Groups["event"].Value,
						data: match.Groups["data"].Value,
						dropResult: match.Groups["result"].Value);
				})
				.ToList();

			if (logEntries.Any(e => !e.succes))
			{
				Assert.Fail("Unexpected log entries: \r\n" + string.Join("\r\n", logEntries.Where(e => !e.succes).Select(e => e.raw)));
			}

			var log = logEntries.GetEnumerator();
			var moveNext = true;

			AssertEvent("NestedDragSource", "STARTING");
			AssertEvent("ParentDropTarget", "ENTER");
			AssertEvent("ParentDropTarget", "OVER");
			SkipEvents("ParentDropTarget", "OVER");
			AssertEvent("NestedDropTarget", "ENTER");
			AssertEvent("NestedDropTarget", "OVER");
			SkipEvents(null, "OVER");

			if (goAway)
			{
				AssertEvent("NestedDropTarget", "LEAVE");

				// This is not the UWP behavior but is a known limitation for an unusual setup
				SkipEvents("ParentDropTarget", "LEAVE");
				SkipEvents("ParentDropTarget", "ENTER");

				SkipEvents("ParentDropTarget", "OVER");
				AssertEvent("ParentDropTarget", "LEAVE");
				AssertCompleted("None");
			}
			else
			{
				AssertEvent("NestedDropTarget", "DROP");
				AssertEvent("ParentDropTarget", "DROP");
				AssertCompleted("Copy");
			}
			Assert.IsFalse(log.MoveNext(), "Should have reach the end of the log.");

			void SkipEvents(string? expectedSender, string expectedEvent)
			{
				while (
					log.MoveNext()
					&& (expectedSender is null || log.Current.element == expectedSender)
					&& log.Current.@event == expectedEvent)
				{ }
				moveNext = false;
			}

			void AssertEvent(string expectedSender, string expectedEvent)
			{
				if (moveNext)
				{
					Assert.IsTrue(log.MoveNext(), $"Expecting {expectedEvent} event, but reached the end of the log.");
				}
				Assert.AreEqual(expectedSender, log.Current.element);
				Assert.AreEqual(expectedEvent, log.Current.@event);
				moveNext = true;
			}

			void AssertCompleted(string expectedResult)
			{
				if (moveNext)
				{
					Assert.IsTrue(log.MoveNext(), $"Expecting COMPLETED event, but reached the end of the log.");
				}
				Assert.AreEqual("COMPLETED", log.Current.@event);
				Assert.AreEqual(expectedResult, log.Current.dropResult);
				moveNext = true;
			}
		}
	}
}
