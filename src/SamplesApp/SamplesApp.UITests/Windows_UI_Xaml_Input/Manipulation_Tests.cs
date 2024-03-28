using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.Windows_UI_Xaml_Input
{
	public partial class Manipulation_Tests : SampleControlUITestBase
	{
		[Test]
		[AutoRetry]
		public void TestManipulation()
		{
			Run("UITests.Shared.Windows_UI_Input.GestureRecognizerTests.ManipulationEvents");

			var rect = _app.WaitForElement("_thumb").Single().Rect;
			_app.DragCoordinates(rect.X + 10, rect.Y + 10, rect.X - 100, rect.Y - 100);

			TakeScreenshot("Result");
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.iOS, Platform.Android)]
		public void ManipulateDelta_DragLeft() => ManipulationDelta_Dragging(dx: 100);

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.iOS, Platform.Android)]
		public void ManipulateDelta_DragUp() => ManipulationDelta_Dragging(dy: 100);

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.iOS, Platform.Android)]
		public void ManipulateDelta_DragRight() => ManipulationDelta_Dragging(dx: -100);

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.iOS, Platform.Android)]
		public void ManipulateDelta_DragDown() => ManipulationDelta_Dragging(dy: -100);

		public void ManipulationDelta_Dragging(int dx = 0, int dy = 0)
		{
			if (!(dx == 0 ^ dy == 0))
			{
				throw new ArgumentException($"dx and dy cannot be both 0 or both have value: {dx}, {dy}");
			}

			Run("UITests.Shared.Windows_UI_Input.GestureRecognizerTests.ManipulationEvents");

			// dragging
			var rect = _app.WaitForElement("_thumb").Single().Rect;
			_app.DragCoordinates(rect.CenterX, rect.CenterY, rect.CenterX + dx, rect.CenterY + dy);

			// assert if the square translated in the opposite direction
			var translateX = _app.Marked("DebugThumbTranslateX").GetDependencyPropertyValue<string>("Text");
			var translateY = _app.Marked("DebugThumbTranslateY").GetDependencyPropertyValue<string>("Text");
			if (float.TryParse(translateX, out var tx) && float.TryParse(translateY, out var ty))
			{
				var context = dx != 0
					? new { Axis = 'X', Subscript = 'x', Delta = dx, Translation = tx }
					: new { Axis = 'Y', Subscript = 'y', Delta = dy, Translation = ty };
				Assert.IsTrue(
					Math.Sign(context.Delta) * -1 == Math.Sign(context.Translation),
					$"Expect TranslateTransform.{context.Axis} (t{context.Subscript}:{context.Translation}) to be in the opposite direction of dragging direction (d{context.Subscript}:{context.Delta})."
				);
			}
			else
			{
				throw new InvalidOperationException($"Failed to parse DebugThumbTranslate(X|Y) values: '{translateX}', '{translateY}'");
			}
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.iOS)] // PinchToZoomInCoordinates is not supported on WASM yet, failing on Android (Expected: 2.0d, but was: 2.04999995)
		public void Test_Scale()
		{
			Run("UITests.Shared.Windows_UI_Input.GestureRecognizerTests.Manipulation_Basics");

			var target = _app.WaitForElement("TouchTarget").Single();

			_app.PinchToZoomInCoordinates(target.Rect.CenterX, target.Rect.CenterY, TimeSpan.FromSeconds(.1));

			var resultStr = new QueryEx(q => q.All().Marked("Output")).GetDependencyPropertyValue<string>("Text");
			var result = Parse(resultStr);

			Assert.AreEqual(2.0, result.cumulative.Scale);
		}

		[Test]
		[AutoRetry]
		public void Manipulation_WithNestedElement()
		{
			Run("UITests.Windows_UI_Input.GestureRecognizerTests.Manipulation_WithNestedElement");

			var rect = _app.WaitForElement("_target").Single().Rect;
			_app.DragCoordinates(rect.X + 10, rect.Y + 10, rect.Right - 10, rect.Bottom - 10);

			var result = _app.Marked("_result").GetDependencyPropertyValue<string>("Text");
			result = result.Replace("\r", "").Replace("\n", "");

			Assert.IsTrue(result.Contains("[PARENT] Manip delta[CHILD] Pointer moved[PARENT] Manip delta[CHILD] Pointer moved"));
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Android, Platform.iOS)] // Touch only test
		public void Manipulation_WhenInListViewAndManipulationTranslateX_ThenAbort()
		{
			Run("UITests.Windows_UI_Input.GestureRecognizerTests.Manipulation_WhenInListView");

			var scroller = _app.WaitForElement("ItemsSupportsTranslateX").Single().Rect;

			_app.DragCoordinates(scroller.CenterX, scroller.Bottom - 5, scroller.CenterX, scroller.Y + 10);

			var result = TakeScreenshot("after_scroll", ignoreInSnapshotCompare: true);

			ImageAssert.DoesNotHaveColorAt(result, scroller.CenterX, scroller.Y + 10, "#FF0000");
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Android, Platform.iOS)]
		[Ignore("We need to do a L manipulation to get manip started then pt cancel due to SV kick-in")]
		public void Manipulation_WhenInScrollViewer()
		{
			Run("UITests.Windows_UI_Input.GestureRecognizerTests.Manipulation_WhenInScrollViewer");

			var scroller = _app.WaitForElement("TheScroller").Single().Rect;
			var target = _app.WaitForElement("TouchTarget").Single().Rect;
			_app.DragCoordinates(target.X + 5, target.Bottom - 5, scroller.Right - 20, scroller.Y + 10);

			var result = _app.Marked("Output").GetDependencyPropertyValue<string>("Text");
			var events = result.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(Parse).ToArray();

			Assert.AreEqual(events.Last().evt.ToLowerInvariant(), "completed");
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Android, Platform.iOS)]
		public void Manipulation_WhenInScrollViewerAndManipulationNoneAndTranslateX_ThenNoManipulation()
			=> TestManipulationInScroller("SetNone", TranslateX, AssertNoManipulationSequence);

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Android, Platform.iOS)]
		public void Manipulation_WhenInScrollViewerAndManipulationNoneAndTranslateY_ThenNoManipulation()
			=> TestManipulationInScroller("SetNone", TranslateY, AssertNoManipulationSequence);

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Android, Platform.iOS)]
		public void Manipulation_WhenInScrollViewerAndManipulationNoneAndDiagonal_ThenNoManipulation()
			=> TestManipulationInScroller("SetNone", Diagonal, AssertNoManipulationSequence);

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Android, Platform.iOS)]
		public void Manipulation_WhenInScrollViewerAndManipulationTranslateXYAndTranslateX_ThenManipulate()
			=> TestManipulationInScroller("SetTranslateXY", TranslateX, AssertFullManipulationSequence);

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Android, Platform.iOS)]
		public void Manipulation_WhenInScrollViewerAndManipulationTranslateXYAndTranslateY_ThenManipulate()
			=> TestManipulationInScroller("SetTranslateXY", TranslateY, AssertFullManipulationSequence);

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Android, Platform.iOS)]
		public void Manipulation_WhenInScrollViewerAndManipulationTranslateXYAndDiagonal_ThenManipulate()
			=> TestManipulationInScroller("SetTranslateXY", Diagonal, AssertFullManipulationSequence);

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Android, Platform.iOS)]
		public void Manipulation_WhenInScrollViewerAndManipulationTranslateXAndTranslateX_ThenManipulate()
			=> TestManipulationInScroller("SetTranslateX", TranslateX, AssertFullManipulationSequence);

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Android, Platform.iOS)]
		public void Manipulation_WhenInScrollViewerAndManipulationTranslateXAndTranslateY_ThenAbort()
			=> TestManipulationInScroller("SetTranslateX", TranslateY, AssertAbortedManipulationSequence);

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Android, Platform.iOS)]
		public void Manipulation_WhenInScrollViewerAndManipulationTranslateYAndTranslateX_ThenAbort()
			=> TestManipulationInScroller("SetTranslateY", TranslateX, AssertAbortedManipulationSequence);

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Android, Platform.iOS)]
		public void Manipulation_WhenInScrollViewerAndManipulationTranslateYAndTranslateY_ThenManipulate()
			=> TestManipulationInScroller("SetTranslateY", TranslateY, AssertFullManipulationSequence);

		private void Diagonal(IAppRect target, IAppRect scroller)
			=> _app.DragCoordinates(target.X + 5, target.Bottom - 5, scroller.Right - 20, scroller.Y + 10);

		private void TranslateX(IAppRect target, IAppRect scroller)
			=> _app.DragCoordinates(target.X + 5, target.Bottom - 5, scroller.Right - 20, target.Bottom - 5);

		private void TranslateY(IAppRect target, IAppRect scroller)
			=> _app.DragCoordinates(target.X + 5, target.Bottom - 5, target.X + 5, scroller.Y + 10);

		private void TestManipulationInScroller(QueryEx mode, Action<IAppRect, IAppRect> drag, Action<string> assert)
		{
			Run("UITests.Shared.Windows_UI_Input.GestureRecognizerTests.Manipulation_WhenInScrollViewer");

			var scroller = _app.WaitForElement("TheScroller").Single().Rect;
			var target = _app.WaitForElement("TouchTarget").Single().Rect;

			_app.FastTap(mode);
			drag(target, scroller);

			var result = _app.Marked("Output").GetDependencyPropertyValue<string>("Text");

			assert(result);
		}

		private static (string evt, Point position, ManipulationDelta delta, ManipulationDelta cumulative) Parse(string raw)
		{
			string num(string name) => $@" ?(?<{name}>-?\d{{2,3}}\.\d{{2}})";
			var regex = new Regex(
				@"\[(?<evt>\w+)\]\s*"
				+ $@"@=\[{num("posX")},{num("posY")}\] "
				+ $@"\| X=\(Σ:{num("ΣtrX")} / Δ:{num("ΔtrX")}\) "
				+ $@"\| Y=\(Σ:{num("ΣtrY")} / Δ:{num("ΔtrY")}\) "
				+ $@"\| θ=\(Σ:{num("Σr")} / Δ:{num("Δr")}\) "
				+ $@"\| s=\(Σ:{num("Σs")} / Δ:{num("Δs")}\) "
				+ $@"\| e=\(Σ:{num("Σe")} / Δ:{num("Δe")}\)");

			var values = regex.Match(raw);
			if (!values.Success)
			{
				var nameOnlyRegex = new Regex(@"^\[(?<evt>\w+)\]\s*");
				values = nameOnlyRegex.Match(raw);
				if (values.Success)
				{
					return (values.Groups["evt"].Value, default, default, default);
				}

				throw new FormatException("Cannot parse: ");
			}

			float f(string name) => float.Parse(values.Groups[name].Value, CultureInfo.InvariantCulture);
			Point p(string nameX, string nameY) => new Point((int)f(nameX), (int)f(nameY));

			var evt = values.Groups["evt"].Value;
			var position = p("posX", "posY");
			var delta = new ManipulationDelta
			{
				Translation = p("ΔtrX", "ΔtrX"),
				Rotation = f("Δr"),
				Scale = f("Δs"),
				Expansion = f("Δe")
			};
			var cumulative = new ManipulationDelta
			{
				Translation = p("ΣtrX", "ΣtrX"),
				Rotation = f("Σr"),
				Scale = f("Σs"),
				Expansion = f("Σe")
			};

			return (evt, position, delta, cumulative);
		}

		private void AssertNoManipulationSequence(string raw)
			=> Assert.AreEqual(0, raw.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Length);

		private void AssertFullManipulationSequence(string raw)
			=> AssertSequence(new[] { "starting", "started", "delta", "completed" }, raw.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(Parse).ToArray());

		private void AssertAbortedManipulationSequence(string raw)
			=> AssertSequence(new[] { "starting" }, raw.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(Parse).ToArray());

		private void AssertSequence(string[] expected, (string evt, Point position, ManipulationDelta delta, ManipulationDelta cumulative)[] actual)
		{
			var index = 0;
			foreach (var @event in actual)
			{
				if (!@event.evt.Equals(expected[index], StringComparison.OrdinalIgnoreCase))
				{
					index++;
					if (index >= expected.Length)
					{
						Assert.Fail("Invalid event sequence: Unexpected event.");
					}
				}
			}

			if (index < expected.Length - 1)
			{
				Assert.Fail("Invalid event sequence: Didn't  get all expected events.");
			}
		}

		private struct ManipulationDelta
		{
			public Point Translation;
			public float Scale;
			public float Rotation;
			public float Expansion;

			public override string ToString()
				=> $"x:{Translation.X:N0};y:{Translation.Y:N0};θ:{Rotation:F2};s:{Scale:F2};e:{Expansion:F2}";
		}
	}
}
