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
	[Ignore("Temporary ignore to fix higher priority issue")]
	public partial class DragDrop_Basics_Automated : SampleControlUITestBase
	{
		private static readonly Regex _logEntry = new Regex(@"^\s*\[(?<element>\w+)\] (?<event>[A-Z]+) (\<(?<data>[\w\|]*)\>|(?<result>\w+))\s*$");

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Android | Platform.iOS)] // Starting and completed might not be raised by the WASM test engine
		public void When_BasicDragSource() => RunTest("");

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Android | Platform.iOS)] // Starting and completed might not be raised by the WASM test engine
		public void When_TextDragSource() => RunTest("Text");

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Android | Platform.iOS)] // Starting and completed might not be raised by the WASM test engine
		public void When_LinkDragSource() => RunTest("UniformResourceLocatorW");

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Android | Platform.iOS)] // Starting and completed might not be raised by the WASM test engine
		public void When_ImageDragSource() => RunTest("Bitmap");

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Android | Platform.iOS)] // Starting and completed might not be raised by the WASM test engine
		public void When_BasicDragSource_And_GoAway_Then_GetLeave() => RunTest("");

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Android | Platform.iOS)] // Starting and completed might not be raised by the WASM test engine
		public void When_TextDragSource_And_GoAway_Then_GetLeave() => RunTest("Text");

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Android | Platform.iOS)] // Starting and completed might not be raised by the WASM test engine
		public void When_LinkDragSource_And_GoAway_Then_GetLeave() => RunTest("UniformResourceLocatorW");

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Android | Platform.iOS)] // Starting and completed might not be raised by the WASM test engine
		public void When_ImageDragSource_And_GoAway_Then_GetLeave() => RunTest("Bitmap");

		private void RunTest(string? expectedData, [CallerMemberName] string? testName = null)
		{
			Run("UITests.Windows_UI_Xaml.DragAndDrop.DragDrop_Basics", skipInitialScreenshot: true);

			var dragSource = testName!.Split('_')[1];
			var goAway = testName.Contains("And_GoAway");

			var automated = _app.Marked("Automated");
			var source = _app.Marked(dragSource);
			var target = _app.Marked("DropTarget");
			var output = _app.Marked("Output");

			_app.WaitForElement(automated);

			var automatedBounds = _app.Query(automated).Single().Rect;
			var sourceBounds = _app.Query(source).Single().Rect;
			var targetBounds = _app.Query(target).Single().Rect;

			_app.TapCoordinates(automatedBounds.X + 15, automatedBounds.CenterY);
			_app.DragCoordinates(sourceBounds.CenterX, sourceBounds.CenterY, goAway ? targetBounds.Right + 10 : targetBounds.CenterX, targetBounds.CenterY);

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

			AssertEvent("STARTING");
			AssertEvent("ENTER");
			AssertEvent("OVER");
			while (log.Current.@event == "OVER" && log.MoveNext()) { }
			if (goAway)
			{
				AssertEvent("LEAVE", moveNext: false);
				AssertCompleted("None");
			}
			else
			{
				AssertEvent("DROP", moveNext: false);
				AssertCompleted("Copy");
			}
			Assert.IsFalse(log.MoveNext(), "Should have reach the end of the log.");

			void AssertEvent(string expectedEvent, bool moveNext = true)
			{
				if (moveNext)
				{
					Assert.IsTrue(log.MoveNext(), $"Expecting {expectedEvent} event, but reached the end of the log.");
				}
				Assert.AreEqual(expectedEvent, log.Current.@event);
				Assert.AreEqual(expectedData, log.Current.data);
			}

			void AssertCompleted(string expectedResult)
			{
				Assert.IsTrue(log.MoveNext(), $"Expecting COMPLETED event, but reached the end of the log.");
				Assert.AreEqual("COMPLETED", log.Current.@event);
				Assert.AreEqual(expectedResult, log.Current.dropResult);
			}
		}
	}
}
