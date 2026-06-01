using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using Uno.UI.RuntimeTests.Helpers;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml;

[TestClass]
public class Given_FrameworkElement_FocusVisuals
{
	[TestMethod]
	[RunsOnUIThread]
	public void When_PrimaryThickness_Default()
	{
		var element = new PlainFrameworkElement();
		var actualThickness = element.FocusVisualPrimaryThickness;
		var expectedThickness = ThicknessFromUniformLength(2);
		Assert.AreEqual(expectedThickness, actualThickness);
	}

	[TestMethod]
	[RunsOnUIThread]
	public void When_SecondaryThickness_Default()
	{
		var element = new PlainFrameworkElement();
		var actualThickness = element.FocusVisualSecondaryThickness;
		var expectedThickness = ThicknessFromUniformLength(1);
		Assert.AreEqual(expectedThickness, actualThickness);
	}

	[TestMethod]
	[RunsOnUIThread]
	public void When_Margin_Default()
	{
		var element = new PlainFrameworkElement();
		var actualMargin = element.FocusVisualMargin;
		var expectedMargin = ThicknessFromUniformLength(0);
		Assert.AreEqual(expectedMargin, actualMargin);
	}

	[TestMethod]
	[RunsOnUIThread]
	public void When_PrimaryBrush_Default()
	{
		var element = new PlainFrameworkElement();
		var expectedBrush = Application.Current.Resources["SystemControlFocusVisualPrimaryBrush"];
		var actualBrush = element.FocusVisualPrimaryBrush;
		Assert.AreEqual(expectedBrush, actualBrush);
	}

	[TestMethod]
	[RunsOnUIThread]
	public void When_SecondaryBrush_Default()
	{
		var element = new PlainFrameworkElement();
		var expectedBrush = Application.Current.Resources["SystemControlFocusVisualSecondaryBrush"];
		var actualBrush = element.FocusVisualSecondaryBrush;
		Assert.AreEqual(expectedBrush, actualBrush);
	}

#if HAS_UNO
	// Regression coverage: focus visual brush must follow the focused element's
	// effective theme (its RequestedTheme or its parent's), not the app/system theme.
	[TestMethod]
	[RunsOnUIThread]
	[RequiresFullWindow]
	public async Task When_Element_RequestedTheme_Light_App_Dark_Primary_Brush_Is_Light()
	{
		using var _ = ThemeHelper.UseApplicationDarkTheme();

		var element = new Border { Width = 100, Height = 100, RequestedTheme = ElementTheme.Light };
		WindowHelper.WindowContent = element;
		await WindowHelper.WaitForLoaded(element);

		Assert.AreEqual(ElementTheme.Light, element.ActualTheme);

		var brush = element.FocusVisualPrimaryBrush as SolidColorBrush;
		Assert.IsNotNull(brush);
		Assert.AreEqual(Color.FromArgb(0xE4, 0x00, 0x00, 0x00), brush.Color);
	}

	[TestMethod]
	[RunsOnUIThread]
	[RequiresFullWindow]
	public async Task When_Element_RequestedTheme_Dark_App_Light_Primary_Brush_Is_Dark()
	{
		using var _ = ThemeHelper.UseApplicationLightTheme();

		var element = new Border { Width = 100, Height = 100, RequestedTheme = ElementTheme.Dark };
		WindowHelper.WindowContent = element;
		await WindowHelper.WaitForLoaded(element);

		Assert.AreEqual(ElementTheme.Dark, element.ActualTheme);

		var brush = element.FocusVisualPrimaryBrush as SolidColorBrush;
		Assert.IsNotNull(brush);
		Assert.AreEqual(Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF), brush.Color);
	}

	[TestMethod]
	[RunsOnUIThread]
	[RequiresFullWindow]
	public async Task When_Element_RequestedTheme_Light_App_Dark_Secondary_Brush_Is_Light()
	{
		using var _ = ThemeHelper.UseApplicationDarkTheme();

		var element = new Border { Width = 100, Height = 100, RequestedTheme = ElementTheme.Light };
		WindowHelper.WindowContent = element;
		await WindowHelper.WaitForLoaded(element);

		Assert.AreEqual(ElementTheme.Light, element.ActualTheme);

		var brush = element.FocusVisualSecondaryBrush as SolidColorBrush;
		Assert.IsNotNull(brush);
		Assert.AreEqual(Color.FromArgb(0xB3, 0xFF, 0xFF, 0xFF), brush.Color);
	}

	[TestMethod]
	[RunsOnUIThread]
	[RequiresFullWindow]
	public async Task When_Element_RequestedTheme_Dark_App_Light_Secondary_Brush_Is_Dark()
	{
		using var _ = ThemeHelper.UseApplicationLightTheme();

		var element = new Border { Width = 100, Height = 100, RequestedTheme = ElementTheme.Dark };
		WindowHelper.WindowContent = element;
		await WindowHelper.WaitForLoaded(element);

		Assert.AreEqual(ElementTheme.Dark, element.ActualTheme);

		var brush = element.FocusVisualSecondaryBrush as SolidColorBrush;
		Assert.IsNotNull(brush);
		Assert.AreEqual(Color.FromArgb(0xB3, 0x00, 0x00, 0x00), brush.Color);
	}

	[TestMethod]
	[RunsOnUIThread]
	[RequiresFullWindow]
	public async Task When_Child_Inherits_Parent_RequestedTheme_Light_Primary_Brush_Is_Light()
	{
		using var _ = ThemeHelper.UseApplicationDarkTheme();

		var parent = new Border { Width = 100, Height = 100, RequestedTheme = ElementTheme.Light };
		var child = new Border { Width = 50, Height = 50 };
		parent.Child = child;

		WindowHelper.WindowContent = parent;
		await WindowHelper.WaitForLoaded(parent);

		Assert.AreEqual(ElementTheme.Light, child.ActualTheme);

		var brush = child.FocusVisualPrimaryBrush as SolidColorBrush;
		Assert.IsNotNull(brush);
		Assert.AreEqual(Color.FromArgb(0xE4, 0x00, 0x00, 0x00), brush.Color);
	}
#endif

	private static Thickness ThicknessFromUniformLength(double uniformLength) =>
		new Thickness(uniformLength);
}

public partial class PlainFrameworkElement : FrameworkElement
{
}
