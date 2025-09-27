﻿#if HAS_UNO
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
