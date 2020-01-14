using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.Microsoft_UI_Xaml_Controls.NumberBoxTests
{
	public class NumberBoxTests : SampleControlUITestBase
	{
		[Test]
		[AutoRetry]
		public void UpDownTest()
		{
			Run("UITests.Shared.Microsoft_UI_Xaml_Controls.NumberBoxTests.MUX_Test");

			var numBox = _app.Marked("TestNumberBox");
			Assert.AreEqual(0, numBox.GetDependencyPropertyValue<double>("Value"));

			numBox.SetDependencyPropertyValue("SpinButtonPlacementMode", "Inline");

			var upButton = numBox.Descendant().Marked("UpSpinButton");
			var downButton = numBox.Descendant().Marked("DownSpinButton");

			Console.WriteLine("Assert that up button increases value by 1");
			upButton.Tap();
			Assert.AreEqual(1, numBox.GetDependencyPropertyValue<double>("Value"));

			Console.WriteLine("Assert that down button decreases value by 1");
			downButton.Tap();
			Assert.AreEqual(0, numBox.GetDependencyPropertyValue<double>("Value"));

			Console.WriteLine("Change SmallChange value to 5");
			var smallChangeNumBox = _app.Marked("SmallChangeNumberBox");
			smallChangeNumBox.SetDependencyPropertyValue("Text", "5");

			Console.WriteLine("Assert that up button increases value by 5");
			upButton.Tap();
			Assert.AreEqual(5, numBox.GetDependencyPropertyValue<double>("Value"));

			_app.Tap("MinCheckBox");
			_app.Tap("MaxCheckBox");

			numBox.SetDependencyPropertyValue("Value", "100");
			_app.Tap("WrapCheckBox");

			Console.WriteLine("Assert that when wrapping is on, and value is at max, clicking the up button wraps to the min value.");
			upButton.Tap();
			Assert.AreEqual(0, numBox.GetDependencyPropertyValue<double>("Value"));

			Console.WriteLine("Assert that when wrapping is on, clicking the down button wraps to the max value.");
			downButton.Tap();
			Assert.AreEqual(100, numBox.GetDependencyPropertyValue<double>("Value"));

			Console.WriteLine("Assert that incrementing after typing in a value validates the text first.");
			numBox.ClearText();
			numBox.EnterText("50");
			_app.PressEnter();
			upButton.Tap();
			Assert.AreEqual(55, numBox.GetDependencyPropertyValue<double>("Value"));
		}

		[Test]
		[AutoRetry]
		public void MinMaxTest()
		{
			Run("UITests.Shared.Microsoft_UI_Xaml_Controls.NumberBoxTests.MUX_Test");

			_app.Tap("MinCheckBox");
			_app.Tap("MaxCheckBox");

			var numBox = _app.Marked("TestNumberBox");
			Assert.AreEqual(0, numBox.GetDependencyPropertyValue<double>("Minimum"));
			Assert.AreEqual(100, numBox.GetDependencyPropertyValue<double>("Maximum"));

			numBox.SetDependencyPropertyValue("Value", "10");

			Console.Write("Assert that setting the value to -1 changes the value to 0");
			numBox.SetDependencyPropertyValue("Value", "-1");
			Assert.AreEqual(0, numBox.GetDependencyPropertyValue<double>("Value"));

			Console.Write("Assert that typing '123' in the NumberBox changes the value to 100");
			numBox.ClearText();
			numBox.EnterText("123");
			_app.PressEnter();

			Assert.AreEqual(100, numBox.GetDependencyPropertyValue<double>("Value"));

			Console.Write("Changing Max to 90; Assert value also changes to 90");
			var maxBox = _app.Marked("MaxNumberBox");
			maxBox.SetDependencyPropertyValue("Value", "90");
			Assert.AreEqual(90, numBox.GetDependencyPropertyValue<double>("Value"));

			Console.Write("Assert that setting the minimum above the maximum changes the maximum");
			var minBox = _app.Marked("MinNumberBox");
			minBox.SetDependencyPropertyValue("Value", "200");
			Assert.AreEqual(200, numBox.GetDependencyPropertyValue<double>("Minimum"));
			Assert.AreEqual(200, numBox.GetDependencyPropertyValue<double>("Maximum"));
			Assert.AreEqual(200, numBox.GetDependencyPropertyValue<double>("Value"));

			Console.Write("Assert that setting the maximum below the minimum changes the minimum");
			maxBox.SetDependencyPropertyValue("Value", "150");
			Assert.AreEqual(150, numBox.GetDependencyPropertyValue<double>("Minimum"));
			Assert.AreEqual(150, numBox.GetDependencyPropertyValue<double>("Maximum"));
			Assert.AreEqual(150, numBox.GetDependencyPropertyValue<double>("Value"));
		}

		[Test]
		[AutoRetry]
		public void UpDownEnabledTest()
		{
			Run("UITests.Shared.Microsoft_UI_Xaml_Controls.NumberBoxTests.MUX_Test");

			var numBox = _app.Marked("TestNumberBox");

			numBox.SetDependencyPropertyValue("SpinButtonPlacementMode", "Inline");

			var upButton = numBox.Descendant().Marked("UpSpinButton");
			var downButton = numBox.Descendant().Marked("DownSpinButton");

			_app.Tap("MinCheckBox");
			_app.Tap("MaxCheckBox");

			Console.WriteLine("Assert that when Value is at Minimum, the down spin button is disabled.");
			Assert.IsTrue(upButton.GetDependencyPropertyValue<bool>("IsEnabled"));
			Assert.IsFalse(downButton.GetDependencyPropertyValue<bool>("IsEnabled"));

			Console.WriteLine("Assert that when Value is at Maximum, the up spin button is disabled.");
			numBox.SetDependencyPropertyValue("Value", "100");

			Assert.IsFalse(upButton.GetDependencyPropertyValue<bool>("IsEnabled"));
			Assert.IsTrue(downButton.GetDependencyPropertyValue<bool>("IsEnabled"));

			Console.WriteLine("Assert that when wrapping is enabled, spin buttons are enabled.");
			_app.Tap("WrapCheckBox");
			Assert.IsTrue(upButton.GetDependencyPropertyValue<bool>("IsEnabled"));
			Assert.IsTrue(downButton.GetDependencyPropertyValue<bool>("IsEnabled"));
			_app.Tap("WrapCheckBox");

			Console.WriteLine("Assert that when Maximum is updated the up button is updated also.");
			var maxBox = _app.Marked("MaxNumberBox");
			maxBox.SetDependencyPropertyValue("Value", "200");

			Assert.IsTrue(upButton.GetDependencyPropertyValue<bool>("IsEnabled"));
			Assert.IsTrue(downButton.GetDependencyPropertyValue<bool>("IsEnabled"));

			Console.WriteLine("Assert that spin buttons are disabled if value is NaN.");
			numBox.SetDependencyPropertyValue("Value", "NaN");

			Assert.IsFalse(upButton.GetDependencyPropertyValue<bool>("IsEnabled"));
			Assert.IsFalse(downButton.GetDependencyPropertyValue<bool>("IsEnabled"));

			numBox.SetDependencyPropertyValue("ValidationMode", "Disabled");

			Console.WriteLine("Assert that when validation is off, spin buttons are enabled");
			numBox.SetDependencyPropertyValue("Value", "0");

			Assert.IsTrue(upButton.GetDependencyPropertyValue<bool>("IsEnabled"));
			Assert.IsTrue(downButton.GetDependencyPropertyValue<bool>("IsEnabled"));

			Console.WriteLine("...except in the NaN case");
			numBox.SetDependencyPropertyValue("Value", "NaN");

			Assert.IsFalse(upButton.GetDependencyPropertyValue<bool>("IsEnabled"));
			Assert.IsFalse(downButton.GetDependencyPropertyValue<bool>("IsEnabled"));
		}

		[Test]
		[AutoRetry]
		public void BasicExpressionTest()
		{
			Run("UITests.Shared.Microsoft_UI_Xaml_Controls.NumberBoxTests.MUX_Test");

			var numBox = _app.Marked("TestNumberBox");

			_app.EnterText(numBox, "5 + 3");
			Assert.AreEqual("0", numBox.GetText());

			_app.Tap("ExpressionCheckBox");

			int numErrors = 0;
			const double resetValue = double.NaN;

			Dictionary<string, double> expressions = new Dictionary<string, double>
			{ 
               // Valid expressions. None of these should evaluate to the reset value.
				{ "5", 5 },
				{ "-358", -358 },
				{ "12.34", 12.34 },
				{ "5 + 3", 8 },
				{ "12345 + 67 + 890", 13302 },
				{ "000 + 0011", 11 },
				{ "5 - 3 + 2", 4 },
				{ "3 + 2 - 5", 0 },
				{ "9 - 2 * 6 / 4", 6 },
				{ "9 - -7",  16 },
				{ "9-3*2", 3 },         // no spaces
				{ " 10  *   6  ", 60 }, // extra spaces
				{ "10 /( 2 + 3 )", 2 },
				{ "5 * -40", -200 },
				{ "(1 - 4) / (2 + 1)", -1 },
				{ "3 * ((4 + 8) / 2)", 18 },
				{ "23 * ((0 - 48) / 8)", -138 },
				{ "((74-71)*2)^3", 216 },
				{ "2 - 2 ^ 3", -6 },
				{ "2 ^ 2 ^ 2 / 2 + 9", 17 },
				{ "5 ^ -2", 0.04 },
				{ "5.09 + 14.333", 19.423 },
				{ "2.5 * 0.35", 0.875 },
				{ "-2 - 5", -7 },       // begins with negative number
				{ "(10)", 10 },         // number in parens
				{ "(-9)", -9 },         // negative number in parens
				{ "0^0", 1 },           // who knew?

				// These should not parse, which means they will reset back to the previous value.
				{ "5x + 3y", resetValue },        // invalid chars
				{ "5 + (3", resetValue },         // mismatched parens
				{ "9 + (2 + 3))", resetValue },
				{ "(2 + 3)(1 + 5)", resetValue }, // missing operator
				{ "9 + + 7", resetValue },        // extra operators
				{ "9 - * 7",  resetValue },
				{ "9 - - 7",  resetValue },
				{ "+9", resetValue },
				{ "1 / 0", resetValue },          // divide by zero

				// These don't currently work, but maybe should.
				{ "-(3 + 5)", resetValue }, // negative sign in front of parens -- should be -8
			};

			foreach (KeyValuePair<string, double> pair in expressions)
			{
				numBox.ClearText();
				numBox.EnterText(pair.Key);
				_app.PressEnter();

				var value = numBox.GetDependencyPropertyValue<double>("Value");
				string output = "Expression '" + pair.Key + "' - expected: " + pair.Value + ", actual: " + value;
				if (Math.Abs(pair.Value - value) > 0.00001)
				{
					numErrors++;
					Console.WriteLine(output);
				}
				else
				{
					Console.WriteLine(output);
				}
			}

			Assert.AreEqual(0, numErrors);
		}
	}
}
