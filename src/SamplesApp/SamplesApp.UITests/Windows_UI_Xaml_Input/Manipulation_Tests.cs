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
		[ActivePlatforms(Platform.Android, Platform.iOS)] // PinchToZoomInCoordinates is not supported on WASM yet
		public void Test_Scale()
		{
			Run("UITests.Shared.Windows_UI_Input.GestureRecognizerTests.Manipulation_Basics");

			var target = _app.WaitForElement("TouchTarget").Single();

			_app.PinchToZoomInCoordinates(target.Rect.CenterX, target.Rect.CenterY, TimeSpan.FromSeconds(.1));

			var resultStr = _app.Marked("Output").GetDependencyPropertyValue<string>("Text");
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

		private static (string evt, Point position, ManipulationDelta delta, ManipulationDelta cumulative) Parse(string raw)
		{
			string num(string name) => $@" ?(?<{name}>-?\d{{2,3}}\.\d{{2}})";
			var regex = new Regex(
				@"\[(?<evt>\w+)\] "
				+ $@"@=\[{num("posX")},{num("posY")}\] "
				+ $@"\| X=\(Σ:{num("ΣtrX")} / Δ:{num("ΔtrX")}\) "
				+ $@"\| Y=\(Σ:{num("ΣtrY")} / Δ:{num("ΔtrY")}\) "
				+ $@"\| θ=\(Σ:{num("Σr")} / Δ:{num("Δr")}\) "
				+ $@"\| s=\(Σ:{num("Σs")} / Δ:{num("Δs")}\) "
				+ $@"\| e=\(Σ:{num("Σe")} / Δ:{num("Δe")}\)");

			var values = regex.Match(raw);
			if (!values.Success)
			{
				var nameOnlyRegex = new Regex(@"^\[(?<evt>\w+)\] ");
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
