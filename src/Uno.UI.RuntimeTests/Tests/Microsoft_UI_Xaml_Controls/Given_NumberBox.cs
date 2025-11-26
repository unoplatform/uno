#if HAS_UNO
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Uno.UI.RuntimeTests.Helpers;
using Windows.Globalization.NumberFormatting;
using static Private.Infrastructure.TestServices;

#if WINAPPSDK
using Uno.UI.Extensions;
#elif __APPLE_UIKIT__
using UIKit;
#endif

#if !HAS_UNO_WINUI
using Microsoft.UI.Xaml.Controls;
#endif

namespace Uno.UI.RuntimeTests.Tests.Microsoft_UI_Xaml_Controls;

[TestClass]
[RunsOnUIThread]
public class Given_NumberBox
{
	[TestMethod]
	public async Task When_NB_Fluent_And_Theme_Changed()
	{
		var textBox = new NumberBox
		{
			PlaceholderText = "Enter..."
		};

		WindowHelper.WindowContent = textBox;
		await WindowHelper.WaitForLoaded(textBox);

		var placeholderTextContentPresenter = textBox.FindFirstChild<TextBlock>(tb => tb.Name == "PlaceholderTextContentPresenter");
		Assert.IsNotNull(placeholderTextContentPresenter);

		var lightThemeForeground = TestsColorHelper.ToColor("#9E000000");
		var darkThemeForeground = TestsColorHelper.ToColor("#C5FFFFFF");

		Assert.AreEqual(lightThemeForeground, (placeholderTextContentPresenter.Foreground as SolidColorBrush)?.Color);

		using (ThemeHelper.UseDarkTheme())
		{
			Assert.AreEqual(darkThemeForeground, (placeholderTextContentPresenter.Foreground as SolidColorBrush)?.Color);
		}

		Assert.AreEqual(lightThemeForeground, (placeholderTextContentPresenter.Foreground as SolidColorBrush)?.Color);
	}

	[TestMethod]
	public async Task NumberBox_Should_Apply_CustomFormatter()
	{
		var numberBox = new NumberBox();

		WindowHelper.WindowContent = numberBox;
		await WindowHelper.WaitForLoaded(numberBox);

		var customFormatter = new CustomNumberFormatter();
		numberBox.NumberFormatter = customFormatter;

		numberBox.Value = 123.456;
		var formattedText = numberBox.Text;

		Assert.AreEqual("123.46 units", formattedText);
	}

	[TestMethod]
	public async Task When_Value_Set_To_NaN_Multiple_Times_Should_Not_StackOverflow()
	{
		// This test validates the fix for potential StackOverflow when using two-way bindings with x:Bind
		// When both the current Value and incoming value are NaN, we should not call SetValue
		// to avoid infinite loops since NaN != NaN

		var numberBox = new NumberBox();

		WindowHelper.WindowContent = numberBox;
		await WindowHelper.WaitForLoaded(numberBox);

		// Initially, Value should be NaN (default)
		Assert.IsTrue(double.IsNaN(numberBox.Value), "Initial Value should be NaN");

		// Setting NaN when current value is already NaN should not trigger property change
		// This simulates what happens with two-way binding
		int valueChangedCount = 0;
		numberBox.ValueChanged += (s, e) => valueChangedCount++;

		// Set NaN multiple times - should not cause StackOverflow and should not trigger ValueChanged
		numberBox.Value = double.NaN;
		numberBox.Value = double.NaN;
		numberBox.Value = double.NaN;

		Assert.AreEqual(0, valueChangedCount, "ValueChanged should not be raised when setting NaN to NaN");
		Assert.IsTrue(double.IsNaN(numberBox.Value), "Value should still be NaN");
	}

	[TestMethod]
	public async Task When_Value_Changed_From_NaN_To_Number_Should_Update()
	{
		// Validate that changing from NaN to a number works correctly
		var numberBox = new NumberBox();

		WindowHelper.WindowContent = numberBox;
		await WindowHelper.WaitForLoaded(numberBox);

		Assert.IsTrue(double.IsNaN(numberBox.Value), "Initial Value should be NaN");

		int valueChangedCount = 0;
		numberBox.ValueChanged += (s, e) =>
		{
			valueChangedCount++;
			Assert.IsTrue(double.IsNaN(e.OldValue), "OldValue should be NaN");
			Assert.AreEqual(42.0, e.NewValue, "NewValue should be 42.0");
		};

		// Change from NaN to a number - should work
		numberBox.Value = 42.0;

		Assert.AreEqual(1, valueChangedCount, "ValueChanged should be raised once");
		Assert.AreEqual(42.0, numberBox.Value, "Value should be 42.0");
	}

	[TestMethod]
	public async Task When_Value_Changed_From_Number_To_NaN_Should_Update()
	{
		// Validate that changing from a number to NaN works correctly
		var numberBox = new NumberBox();

		WindowHelper.WindowContent = numberBox;
		await WindowHelper.WaitForLoaded(numberBox);

		// Set initial value
		numberBox.Value = 100.0;
		Assert.AreEqual(100.0, numberBox.Value, "Initial Value should be 100.0");

		int valueChangedCount = 0;
		numberBox.ValueChanged += (s, e) =>
		{
			valueChangedCount++;
			Assert.AreEqual(100.0, e.OldValue, "OldValue should be 100.0");
			Assert.IsTrue(double.IsNaN(e.NewValue), "NewValue should be NaN");
		};

		// Change from number to NaN - should work
		numberBox.Value = double.NaN;

		Assert.AreEqual(1, valueChangedCount, "ValueChanged should be raised once");
		Assert.IsTrue(double.IsNaN(numberBox.Value), "Value should be NaN");
	}

	[TestMethod]
	public async Task When_Value_Changed_Between_Numbers_Should_Update()
	{
		// Validate that changing between numbers works correctly
		var numberBox = new NumberBox();

		WindowHelper.WindowContent = numberBox;
		await WindowHelper.WaitForLoaded(numberBox);

		// Set initial value
		numberBox.Value = 10.0;
		Assert.AreEqual(10.0, numberBox.Value, "Initial Value should be 10.0");

		int valueChangedCount = 0;
		numberBox.ValueChanged += (s, e) =>
		{
			valueChangedCount++;
		};

		// Change from one number to another - should work
		numberBox.Value = 20.0;
		numberBox.Value = 30.0;

		Assert.AreEqual(2, valueChangedCount, "ValueChanged should be raised twice");
		Assert.AreEqual(30.0, numberBox.Value, "Value should be 30.0");
	}
}

internal class CustomNumberFormatter : INumberFormatter2, INumberParser
{
	public string FormatDouble(double value) => value.ToString("0.00") + " units";
	public double? ParseDouble(string text) => throw new NotImplementedException();

	public string FormatInt(long value) => throw new NotImplementedException();
	public string FormatUInt(ulong value) => throw new NotImplementedException();

	public long? ParseInt(string text) => throw new NotImplementedException();
	public ulong? ParseUInt(string text) => throw new NotImplementedException();
}
#endif
