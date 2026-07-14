using System.Threading.Tasks;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Uno.UI.RuntimeTests.Helpers;
using static Private.Infrastructure.TestServices;

#if WINAPPSDK
using Uno.UI.Extensions;
#elif __APPLE_UIKIT__
using UIKit;
#endif

namespace Uno.UI.RuntimeTests.Tests.Microsoft_UI_Xaml_Controls;

[TestClass]
[RunsOnUIThread]
public class Given_NumberBox_UITest
{
	[TestMethod]
	public async Task When_SpinButtons_Clicked_Changes_Value_By_SmallChange()
	{
		var numberBox = new NumberBox
		{
			SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline,
			Minimum = double.MinValue,
			Maximum = double.MaxValue,
			Value = 0,
		};

		try
		{
			await UITestHelper.Load(numberBox);

			var upButton = numberBox.FindFirstChild<RepeatButton>(x => x.Name == "UpSpinButton");
			var downButton = numberBox.FindFirstChild<RepeatButton>(x => x.Name == "DownSpinButton");
			Assert.IsNotNull(upButton, "UpSpinButton part should exist when SpinButtonPlacementMode is Inline");
			Assert.IsNotNull(downButton, "DownSpinButton part should exist when SpinButtonPlacementMode is Inline");

			new RepeatButtonAutomationPeer(upButton).Invoke();
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(1, numberBox.Value, "Up button should increase the value by SmallChange (default 1)");

			new RepeatButtonAutomationPeer(downButton).Invoke();
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(0, numberBox.Value, "Down button should decrease the value by SmallChange (default 1)");

			numberBox.SmallChange = 5;
			new RepeatButtonAutomationPeer(upButton).Invoke();
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(5, numberBox.Value, "Up button should increase the value by the updated SmallChange");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public async Task When_Wrap_Enabled_SpinButtons_Wrap_At_Bounds()
	{
		var numberBox = new NumberBox
		{
			SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline,
			Minimum = 0,
			Maximum = 100,
			IsWrapEnabled = true,
			Value = 100,
		};

		try
		{
			await UITestHelper.Load(numberBox);

			var upButton = numberBox.FindFirstChild<RepeatButton>(x => x.Name == "UpSpinButton");
			var downButton = numberBox.FindFirstChild<RepeatButton>(x => x.Name == "DownSpinButton");
			Assert.IsNotNull(upButton);
			Assert.IsNotNull(downButton);

			new RepeatButtonAutomationPeer(upButton).Invoke();
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(0, numberBox.Value, "Up button should wrap from Maximum to Minimum");

			new RepeatButtonAutomationPeer(downButton).Invoke();
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(100, numberBox.Value, "Down button should wrap from Minimum to Maximum");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public async Task When_SpinButton_Clicked_Validates_Pending_Text_First()
	{
		var numberBox = new NumberBox
		{
			SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline,
			Minimum = 0,
			Maximum = 100,
			SmallChange = 5,
			Value = 0,
		};

		try
		{
			await UITestHelper.Load(numberBox);

			var upButton = numberBox.FindFirstChild<RepeatButton>(x => x.Name == "UpSpinButton");
			Assert.IsNotNull(upButton);

			// Simulates the user having typed "50" without committing it (no Enter/LostFocus yet).
			numberBox.Text = "50";
			await WindowHelper.WaitForIdle();

			new RepeatButtonAutomationPeer(upButton).Invoke();
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(55, numberBox.Value, "Incrementing should validate the pending text (50) before applying SmallChange (5)");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public async Task When_SpinButtons_IsEnabled_Reflects_Value_State()
	{
		var numberBox = new NumberBox
		{
			SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline,
			Minimum = 0,
			Maximum = 100,
		};

		try
		{
			await UITestHelper.Load(numberBox);

			var upButton = numberBox.FindFirstChild<RepeatButton>(x => x.Name == "UpSpinButton");
			var downButton = numberBox.FindFirstChild<RepeatButton>(x => x.Name == "DownSpinButton");
			Assert.IsNotNull(upButton);
			Assert.IsNotNull(downButton);

			// Value is NaN by default, so both spin buttons should be disabled.
			Assert.IsFalse(upButton.IsEnabled, "Up button should be disabled when Value is NaN");
			Assert.IsFalse(downButton.IsEnabled, "Down button should be disabled when Value is NaN");

			numberBox.Value = 0;
			await WindowHelper.WaitForIdle();
			Assert.IsTrue(upButton.IsEnabled, "Up button should be enabled when Value is above Minimum");
			Assert.IsFalse(downButton.IsEnabled, "Down button should be disabled when Value is at Minimum");

			numberBox.Value = 100;
			await WindowHelper.WaitForIdle();
			Assert.IsFalse(upButton.IsEnabled, "Up button should be disabled when Value is at Maximum");
			Assert.IsTrue(downButton.IsEnabled, "Down button should be enabled when Value is below Maximum");

			numberBox.IsWrapEnabled = true;
			await WindowHelper.WaitForIdle();
			Assert.IsTrue(upButton.IsEnabled, "Both buttons should be enabled when wrapping is enabled");
			Assert.IsTrue(downButton.IsEnabled, "Both buttons should be enabled when wrapping is enabled");
			numberBox.IsWrapEnabled = false;
			await WindowHelper.WaitForIdle();

			numberBox.Maximum = 200;
			await WindowHelper.WaitForIdle();
			Assert.IsTrue(upButton.IsEnabled, "Up button should re-evaluate when Maximum is updated");
			Assert.IsTrue(downButton.IsEnabled, "Down button should re-evaluate when Maximum is updated");

			numberBox.ValidationMode = NumberBoxValidationMode.Disabled;
			numberBox.Value = 0;
			await WindowHelper.WaitForIdle();
			Assert.IsTrue(upButton.IsEnabled, "Both buttons should be enabled when validation is disabled, even at Minimum");
			Assert.IsTrue(downButton.IsEnabled, "Both buttons should be enabled when validation is disabled, even at Minimum");

			numberBox.Value = double.NaN;
			await WindowHelper.WaitForIdle();
			Assert.IsFalse(upButton.IsEnabled, "Up button should still be disabled for NaN, even when validation is disabled");
			Assert.IsFalse(downButton.IsEnabled, "Down button should still be disabled for NaN, even when validation is disabled");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public async Task When_Minimum_Maximum_Value_Are_Coerced()
	{
		var numberBox = new NumberBox();

		try
		{
			await UITestHelper.Load(numberBox);

			numberBox.Minimum = 0;
			numberBox.Maximum = 100;
			Assert.AreEqual(0, numberBox.Minimum);
			Assert.AreEqual(100, numberBox.Maximum);

			numberBox.Value = 10;
			await WindowHelper.WaitForIdle();

			numberBox.Value = -1;
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(0, numberBox.Value, "Setting Value below Minimum should clamp it to Minimum");

			numberBox.Text = "123";
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(100, numberBox.Value, "Typing a value above Maximum should clamp Value to Maximum");

			numberBox.Maximum = 90;
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(90, numberBox.Value, "Lowering Maximum below the current Value should clamp Value to the new Maximum");

			numberBox.Minimum = 200;
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(200, numberBox.Minimum);
			Assert.AreEqual(200, numberBox.Maximum, "Raising Minimum above Maximum should also raise Maximum");
			Assert.AreEqual(200, numberBox.Value, "Value should be clamped to the new Minimum/Maximum");

			numberBox.Maximum = 150;
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(150, numberBox.Minimum, "Lowering Maximum below Minimum should also lower Minimum");
			Assert.AreEqual(150, numberBox.Maximum);
			Assert.AreEqual(150, numberBox.Value, "Value should be clamped to the new Minimum/Maximum");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[DataRow("5", 5d)]
	[DataRow("-358", -358d)]
	[DataRow("12.34", 12.34d)]
	[DataRow("5 + 3", 8d)]
	[DataRow("12345 + 67 + 890", 13302d)]
	[DataRow("000 + 0011", 11d)]
	[DataRow("5 - 3 + 2", 4d)]
	[DataRow("3 + 2 - 5", 0d)]
	[DataRow("9 - 2 * 6 / 4", 6d)]
	[DataRow("9 - -7", 16d)]
	[DataRow("9-3*2", 3d, DisplayName = "9-3*2 (no spaces)")]
	[DataRow(" 10  *   6  ", 60d, DisplayName = "10 * 6 (extra spaces)")]
	[DataRow("10 /( 2 + 3 )", 2d)]
	[DataRow("5 * -40", -200d)]
	[DataRow("(1 - 4) / (2 + 1)", -1d)]
	[DataRow("3 * ((4 + 8) / 2)", 18d)]
	[DataRow("23 * ((0 - 48) / 8)", -138d)]
	[DataRow("((74-71)*2)^3", 216d)]
	[DataRow("2 - 2 ^ 3", -6d)]
	[DataRow("2 ^ 2 ^ 2 / 2 + 9", 17d)]
	[DataRow("5 ^ -2", 0.04d)]
	[DataRow("5.09 + 14.333", 19.423d)]
	[DataRow("2.5 * 0.35", 0.875d)]
	[DataRow("-2 - 5", -7d, DisplayName = "-2 - 5 (begins with negative number)")]
	[DataRow("(10)", 10d, DisplayName = "(10) (number in parens)")]
	[DataRow("(-9)", -9d, DisplayName = "(-9) (negative number in parens)")]
	[DataRow("0^0", 1d)]
	[DataRow("5x + 3y", double.NaN, DisplayName = "5x + 3y (invalid chars)")]
	[DataRow("5 + (3", double.NaN, DisplayName = "5 + (3 (mismatched parens)")]
	[DataRow("9 + (2 + 3))", double.NaN, DisplayName = "9 + (2 + 3)) (mismatched parens)")]
	[DataRow("(2 + 3)(1 + 5)", double.NaN, DisplayName = "(2 + 3)(1 + 5) (missing operator)")]
	[DataRow("9 + + 7", double.NaN, DisplayName = "9 + + 7 (extra operators)")]
	[DataRow("9 - * 7", double.NaN)]
	[DataRow("9 - - 7", double.NaN)]
	[DataRow("+9", double.NaN)]
	[DataRow("-(3 + 5)", double.NaN, DisplayName = "-(3 + 5) (negative sign in front of parens, currently unsupported)")]
	public async Task When_AcceptsExpression_Evaluates(string expression, double expected)
	{
		var numberBox = new NumberBox
		{
			AcceptsExpression = true,
		};

		try
		{
			await UITestHelper.Load(numberBox);

			// Reset to an empty/NaN state first, matching how the value would look before the expression is entered.
			numberBox.Text = "";
			await WindowHelper.WaitForIdle();

			numberBox.Text = expression;
			await WindowHelper.WaitForIdle();

			if (double.IsNaN(expected))
			{
				Assert.AreEqual(string.Empty, numberBox.Text, $"Expression '{expression}' should fail to parse and reset the text to empty");
			}
			else
			{
				Assert.AreEqual(expected, numberBox.Value, 0.0001, $"Expression '{expression}' should evaluate to {expected}");
			}
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}
}
