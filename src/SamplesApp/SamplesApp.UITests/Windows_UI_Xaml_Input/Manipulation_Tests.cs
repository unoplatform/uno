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

			_app.WaitForElement("_result");

			var result = _app.Marked("_result").GetDependencyPropertyValue<string>("Text");
			result = result.Replace("\r", "").Replace("\n", "");

			Assert.IsTrue(result.Contains("[PARENT] Manip delta[CHILD] Pointer moved[PARENT] Manip delta[CHILD] Pointer moved"));
		}

		private static (Point position, ManipulationDelta delta, ManipulationDelta cumulative) Parse(string raw)
		{
			string num(string name) => $@" ?(?<{name}>-?\d{{2,3}}\.\d{{2}})";
			var regex = new Regex(
				$@"@=\[{num("posX")},{num("posY")}\] "
				+ $@"\| X=\(Σ:{num("ΣtrX")} / Δ:{num("ΔtrX")}\) "
				+ $@"\| Y=\(Σ:{num("ΣtrY")} / Δ:{num("ΔtrY")}\) "
				+ $@"\| θ=\(Σ:{num("Σr")} / Δ:{num("Δr")}\) "
				+ $@"\| s=\(Σ:{num("Σs")} / Δ:{num("Δs")}\) "
				+ $@"\| e=\(Σ:{num("Σe")} / Δ:{num("Δe")}\)");

			var values = regex.Match(raw);
			if (!values.Success)
			{
				throw new FormatException("Cannot parse: ");
			}

			float f(string name) => float.Parse(values.Groups[name].Value, CultureInfo.InvariantCulture);
			Point p(string nameX, string nameY) => new Point((int)f(nameX), (int)f(nameY));

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

			return (position, delta, cumulative);
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
