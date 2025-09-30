using System;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.Microsoft_UI_Xaml_Controls.NumberBoxTests
{
	public partial class NumberBoxTests : SampleControlUITestBase
	{
		private void EnterTextInNumberBox(QueryEx numberBox, string text)
		{
			numberBox.SetDependencyPropertyValue("Text", text);
		}

		[Test]
		[AutoRetry]
		public void UpDownTest()
		{
			Run("UITests.Shared.Microsoft_UI_Xaml_Controls.NumberBoxTests.NumberBoxPage");

			var numBox = _app.Marked("TestNumberBox");
			numBox.SetDependencyPropertyValue("Value", "0");

			numBox.SetDependencyPropertyValue("SpinButtonPlacementMode", "Inline");

			var upButton = numBox.Descendant().Marked("UpSpinButton");
			var downButton = numBox.Descendant().Marked("DownSpinButton");

			Console.WriteLine("Assert that up button increases value by 1");
			upButton.FastTap();
			Assert.AreEqual(1, numBox.GetDependencyPropertyValue<double>("Value"));

			Console.WriteLine("Assert that down button decreases value by 1");
			downButton.FastTap();
			Assert.AreEqual(0, numBox.GetDependencyPropertyValue<double>("Value"));

			Console.WriteLine("Change SmallChange value to 5");
			var smallChangeNumBox = _app.Marked("SmallChangeNumberBox");
			smallChangeNumBox.SetDependencyPropertyValue("Text", "5");

			Console.WriteLine("Assert that up button increases value by 5");
			upButton.FastTap();
			Assert.AreEqual(5, numBox.GetDependencyPropertyValue<double>("Value"));

			_app.FastTap("MinCheckBox");
			_app.FastTap("MaxCheckBox");

			numBox.SetDependencyPropertyValue("Value", "100");
			_app.FastTap("WrapCheckBox");

			Console.WriteLine("Assert that when wrapping is on, and value is at max, clicking the up button wraps to the min value.");
			upButton.FastTap();
			Assert.AreEqual(0, numBox.GetDependencyPropertyValue<double>("Value"));

			Console.WriteLine("Assert that when wrapping is on, clicking the down button wraps to the max value.");
			downButton.FastTap();
			Assert.AreEqual(100, numBox.GetDependencyPropertyValue<double>("Value"));

			Console.WriteLine("Assert that incrementing after typing in a value validates the text first.");
			EnterTextInNumberBox(numBox, "50");
			upButton.FastTap();
			Assert.AreEqual(55, numBox.GetDependencyPropertyValue<double>("Value"));
		}

		[Test]
		[AutoRetry]
		public void MinMaxTest()
		{
			Run("UITests.Shared.Microsoft_UI_Xaml_Controls.NumberBoxTests.NumberBoxPage");

			_app.FastTap("MinCheckBox");
			_app.FastTap("MaxCheckBox");

			var numBox = _app.Marked("TestNumberBox");
			Assert.AreEqual(0, numBox.GetDependencyPropertyValue<double>("Minimum"));
			Assert.AreEqual(100, numBox.GetDependencyPropertyValue<double>("Maximum"));

			numBox.SetDependencyPropertyValue("Value", "10");

			Console.Write("Assert that setting the value to -1 changes the value to 0");
			numBox.SetDependencyPropertyValue("Value", "-1");
			Assert.AreEqual(0, numBox.GetDependencyPropertyValue<double>("Value"));

			Console.Write("Assert that typing '123' in the NumberBox changes the value to 100");
			EnterTextInNumberBox(numBox, "123");

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
			Run("UITests.Shared.Microsoft_UI_Xaml_Controls.NumberBoxTests.NumberBoxPage");

			var numBox = _app.Marked("TestNumberBox");

			numBox.SetDependencyPropertyValue("SpinButtonPlacementMode", "Inline");

			var upButton = numBox.Descendant().Marked("UpSpinButton");
			var downButton = numBox.Descendant().Marked("DownSpinButton");

			_app.FastTap("MinCheckBox");
			_app.FastTap("MaxCheckBox");

			Console.WriteLine("Verify that spin buttons are disabled if value is NaN.");
			Assert.False(upButton.GetDependencyPropertyValue<bool>("IsEnabled"));
			Assert.IsFalse(downButton.GetDependencyPropertyValue<bool>("IsEnabled"));


			Console.WriteLine("Assert that when Value is at Minimum, the down spin button is disabled.");
			numBox.SetDependencyPropertyValue("Value", "0");
			Assert.IsTrue(upButton.GetDependencyPropertyValue<bool>("IsEnabled"));
			Assert.IsFalse(downButton.GetDependencyPropertyValue<bool>("IsEnabled"));

			Console.WriteLine("Assert that when Value is at Maximum, the up spin button is disabled.");
			numBox.SetDependencyPropertyValue("Value", "100");
			Assert.IsFalse(upButton.GetDependencyPropertyValue<bool>("IsEnabled"));
			Assert.IsTrue(downButton.GetDependencyPropertyValue<bool>("IsEnabled"));

			Console.WriteLine("Assert that when wrapping is enabled, spin buttons are enabled.");
			_app.FastTap("WrapCheckBox");
			Assert.IsTrue(upButton.GetDependencyPropertyValue<bool>("IsEnabled"));
			Assert.IsTrue(downButton.GetDependencyPropertyValue<bool>("IsEnabled"));
			_app.FastTap("WrapCheckBox");

			Console.WriteLine("Assert that when Maximum is updated the up button is updated also.");
			var maxBox = _app.Marked("MaxNumberBox");
			maxBox.SetDependencyPropertyValue("Value", "200");
			Assert.IsTrue(upButton.GetDependencyPropertyValue<bool>("IsEnabled"));
			Assert.IsTrue(downButton.GetDependencyPropertyValue<bool>("IsEnabled"));


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
			Run("UITests.Microsoft_UI_Xaml_Controls.NumberBoxTests.NumberBox_ExpressionTest");

			_app.Marked("TestNumberBox").WaitUntilExists();

			_app.Marked("RunButton").FastTap();

			_app.WaitFor(() => _app.Marked("Status").GetText() is string s && (s.Equals("Success") || s.StartsWith("Failure")));

			_app.Marked("Status").GetText().Should().Be("Success");
		}
	}
}
