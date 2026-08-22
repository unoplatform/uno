using System;
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
	[RunsOnUIThread]
	public async Task When_NumberBox_Compact_In_ScrollViewer_And_Scrolled()
	{
		var scrollViewer = new ScrollViewer
		{
			Height = 200,
			Width = 300
		};

		var stackPanel = new StackPanel();

		// Add spacer at top
		stackPanel.Children.Add(new Border { Height = 300 });

		// Add the NumberBox with Compact spin button placement
		var numberBox = new NumberBox
		{
			Width = 150,
			SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Compact,
			Value = 10
		};
		stackPanel.Children.Add(numberBox);

		// Add spacer at bottom
		stackPanel.Children.Add(new Border { Height = 3000 });

		scrollViewer.Content = stackPanel;

		WindowHelper.WindowContent = scrollViewer;
		await WindowHelper.WaitForLoaded(scrollViewer);

		// Focus the NumberBox to show the spin buttons popup
		numberBox.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);
		await WindowHelper.WaitForIdle();

		// Get the popup
		var popup = numberBox.FindFirstChild<Microsoft.UI.Xaml.Controls.Primitives.Popup>(p => p.Name == "UpDownPopup");
		Assert.IsNotNull(popup, "NumberBox should have an UpDownPopup");

		// Wait for popup to open
		await WindowHelper.WaitFor(() => popup.IsOpen);
		Assert.IsTrue(popup.IsOpen, "Popup should be open when NumberBox has focus");

		// Get the popup content root to track its position
		var popupChild = popup.Child;
		Assert.IsNotNull(popupChild, "Popup should have a child");

		// Get the initial position of the popup child relative to the window
		var initialPosition = popupChild.TransformToVisual(null).TransformPoint(new Windows.Foundation.Point(0, 0));

		// Scroll down
		scrollViewer.ChangeView(null, 200, null, disableAnimation: true);
		await WindowHelper.WaitForIdle();

		// Get the new position of the popup child
		var newPosition = popupChild.TransformToVisual(null).TransformPoint(new Windows.Foundation.Point(0, 0));

		// The popup should have moved with the scroll (Y position should have decreased)
		Assert.AreNotEqual(initialPosition.Y, newPosition.Y, "Popup should have moved when scrolling");
		Assert.IsTrue(newPosition.Y < initialPosition.Y, "Popup should have moved up when scrolling down");
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
