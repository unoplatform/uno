#if HAS_UNO
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
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
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/9942")]
	public async Task When_NumberBox_TwoWay_Binding_With_FallbackValue_And_Null_DataContext()
	{
		// Issue #9942: An exception is thrown when a NumberBox is bound in TwoWay
		// mode with a FallbackValue, and the DataContext (model) becomes null.
		var viewModel = new NumberBoxTestViewModel { Value = 42.0 };

		var numberBox = new NumberBox();
		numberBox.SetBinding(NumberBox.ValueProperty, new Binding
		{
			Path = new PropertyPath("Value"),
			Mode = BindingMode.TwoWay,
			FallbackValue = 0.0,
		});
		numberBox.DataContext = viewModel;

		WindowHelper.WindowContent = numberBox;
		await WindowHelper.WaitForLoaded(numberBox);
		await WindowHelper.WaitForIdle();

		// Verify initial binding works
		Assert.AreEqual(42.0, numberBox.Value, "Initial value should be 42");

		// Setting DataContext to null should not throw an exception.
		// The FallbackValue should be applied instead.
		Exception caughtException = null;
		try
		{
			numberBox.DataContext = null;
			await WindowHelper.WaitForIdle();
		}
		catch (Exception ex)
		{
			caughtException = ex;
		}

		Assert.IsNull(caughtException,
			$"Setting DataContext to null should not throw an exception, but got: {caughtException?.Message}");

		// The FallbackValue should be applied
		Assert.AreEqual(0.0, numberBox.Value,
			"NumberBox value should be the FallbackValue (0.0) when DataContext is null");
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/9942")]
	public async Task When_NumberBox_TwoWay_Binding_And_DataContext_Changes_Between_Models()
	{
		// Issue #9942: Extended test - verifying navigation between models
		// and clearing (setting to null) works without exceptions.
		var model1 = new NumberBoxTestViewModel { Value = 10.0 };
		var model2 = new NumberBoxTestViewModel { Value = 20.0 };

		var numberBox = new NumberBox();
		numberBox.SetBinding(NumberBox.ValueProperty, new Binding
		{
			Path = new PropertyPath("Value"),
			Mode = BindingMode.TwoWay,
			FallbackValue = 0.0,
		});
		numberBox.DataContext = model1;

		WindowHelper.WindowContent = numberBox;
		await WindowHelper.WaitForLoaded(numberBox);
		await WindowHelper.WaitForIdle();

		Assert.AreEqual(10.0, numberBox.Value, "Should show model1 value");

		// Navigate to model2
		numberBox.DataContext = model2;
		await WindowHelper.WaitForIdle();
		Assert.AreEqual(20.0, numberBox.Value, "Should show model2 value");

		// Clear (set to null) - this is the problematic scenario
		Exception caughtException = null;
		try
		{
			numberBox.DataContext = null;
			await WindowHelper.WaitForIdle();
		}
		catch (Exception ex)
		{
			caughtException = ex;
		}

		Assert.IsNull(caughtException,
			$"Clearing DataContext should not throw, but got: {caughtException?.Message}");

		Assert.AreEqual(0.0, numberBox.Value,
			"NumberBox value should be the FallbackValue (0.0) after clearing DataContext");

		// Navigate back to model1 - should work fine
		numberBox.DataContext = model1;
		await WindowHelper.WaitForIdle();
		Assert.AreEqual(10.0, numberBox.Value, "Should show model1 value again after re-assigning");
	}
}

internal class NumberBoxTestViewModel : INotifyPropertyChanged
{
	private double _value;
	public double Value
	{
		get => _value;
		set
		{
			if (_value != value)
			{
				_value = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
			}
		}
	}

	public event PropertyChangedEventHandler PropertyChanged;
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
