using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Uno.UI.RuntimeTests.Helpers;
using Private.Infrastructure;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml;

[TestClass]
[RunsOnUIThread]
public class Given_ColorPaletteResources
{
	[TestMethod]
	public void When_Setting_Accent_Color()
	{
		var cpr = new ColorPaletteResources();
		var testColor = Colors.Red;

		cpr.Accent = testColor;

		Assert.AreEqual(testColor, cpr.Accent);
		Assert.IsTrue(cpr.ContainsKey("SystemAccentColor"));
		Assert.AreEqual(testColor, cpr["SystemAccentColor"]);
	}

	[TestMethod]
	public void When_Setting_Null_Removes_Color()
	{
		var cpr = new ColorPaletteResources();
		cpr.Accent = Colors.Red;

		cpr.Accent = null;

		Assert.IsNull(cpr.Accent);
		Assert.IsFalse(cpr.ContainsKey("SystemAccentColor"));
	}

	[TestMethod]
	public void When_Property_Not_Set_Returns_Null()
	{
		var cpr = new ColorPaletteResources();

		Assert.IsNull(cpr.Accent);
		Assert.IsNull(cpr.BaseHigh);
		Assert.IsNull(cpr.ChromeLow);
	}

	[TestMethod]
	public void When_Setting_Multiple_Colors()
	{
		var cpr = new ColorPaletteResources();
		var accentColor = Colors.Blue;
		var baseHighColor = Colors.White;
		var chromeLowColor = Colors.Gray;

		cpr.Accent = accentColor;
		cpr.BaseHigh = baseHighColor;
		cpr.ChromeLow = chromeLowColor;

		Assert.AreEqual(accentColor, cpr.Accent);
		Assert.AreEqual(baseHighColor, cpr.BaseHigh);
		Assert.AreEqual(chromeLowColor, cpr.ChromeLow);

		Assert.AreEqual(accentColor, cpr["SystemAccentColor"]);
		Assert.AreEqual(baseHighColor, cpr["SystemBaseHighColor"]);
		Assert.AreEqual(chromeLowColor, cpr["SystemChromeLowColor"]);
	}

	[TestMethod]
	public void When_All_Properties_Map_To_Correct_Keys()
	{
		var cpr = new ColorPaletteResources();
		var testColor = Colors.Blue;

		// Test a selection of property-to-key mappings
		cpr.Accent = testColor;
		Assert.AreEqual(testColor, cpr["SystemAccentColor"]);

		cpr.AltHigh = testColor;
		Assert.AreEqual(testColor, cpr["SystemAltHighColor"]);

		cpr.AltLow = testColor;
		Assert.AreEqual(testColor, cpr["SystemAltLowColor"]);

		cpr.BaseHigh = testColor;
		Assert.AreEqual(testColor, cpr["SystemBaseHighColor"]);

		cpr.BaseLow = testColor;
		Assert.AreEqual(testColor, cpr["SystemBaseLowColor"]);

		cpr.ChromeAltLow = testColor;
		Assert.AreEqual(testColor, cpr["SystemChromeAltLowColor"]);

		cpr.ChromeBlackHigh = testColor;
		Assert.AreEqual(testColor, cpr["SystemChromeBlackHighColor"]);

		cpr.ChromeLow = testColor;
		Assert.AreEqual(testColor, cpr["SystemChromeLowColor"]);

		cpr.ErrorText = testColor;
		Assert.AreEqual(testColor, cpr["SystemErrorTextColor"]);

		cpr.ListLow = testColor;
		Assert.AreEqual(testColor, cpr["SystemListLowColor"]);

		cpr.ListMedium = testColor;
		Assert.AreEqual(testColor, cpr["SystemListMediumColor"]);
	}

	[TestMethod]
	public void When_Replacing_Color_Value()
	{
		var cpr = new ColorPaletteResources();

		cpr.Accent = Colors.Red;
		Assert.AreEqual(Colors.Red, cpr.Accent);

		cpr.Accent = Colors.Blue;
		Assert.AreEqual(Colors.Blue, cpr.Accent);
		Assert.AreEqual(Colors.Blue, cpr["SystemAccentColor"]);
	}

	[TestMethod]
	public void When_Dictionary_Contains_Color_Before_Property_Access()
	{
		var cpr = new ColorPaletteResources();

		// Add color directly via dictionary interface
		cpr["SystemAccentColor"] = Colors.Green;

		// Property should return the value from dictionary
		Assert.AreEqual(Colors.Green, cpr.Accent);
	}

	[TestMethod]
	public void When_Setting_All_Chrome_Properties()
	{
		var cpr = new ColorPaletteResources();
		var testColor = Colors.Purple;

		cpr.ChromeAltLow = testColor;
		cpr.ChromeBlackHigh = testColor;
		cpr.ChromeBlackLow = testColor;
		cpr.ChromeBlackMedium = testColor;
		cpr.ChromeBlackMediumLow = testColor;
		cpr.ChromeDisabledHigh = testColor;
		cpr.ChromeDisabledLow = testColor;
		cpr.ChromeGray = testColor;
		cpr.ChromeHigh = testColor;
		cpr.ChromeLow = testColor;
		cpr.ChromeMedium = testColor;
		cpr.ChromeMediumLow = testColor;
		cpr.ChromeWhite = testColor;

		Assert.AreEqual(testColor, cpr["SystemChromeAltLowColor"]);
		Assert.AreEqual(testColor, cpr["SystemChromeBlackHighColor"]);
		Assert.AreEqual(testColor, cpr["SystemChromeBlackLowColor"]);
		Assert.AreEqual(testColor, cpr["SystemChromeBlackMediumColor"]);
		Assert.AreEqual(testColor, cpr["SystemChromeBlackMediumLowColor"]);
		Assert.AreEqual(testColor, cpr["SystemChromeDisabledHighColor"]);
		Assert.AreEqual(testColor, cpr["SystemChromeDisabledLowColor"]);
		Assert.AreEqual(testColor, cpr["SystemChromeGrayColor"]);
		Assert.AreEqual(testColor, cpr["SystemChromeHighColor"]);
		Assert.AreEqual(testColor, cpr["SystemChromeLowColor"]);
		Assert.AreEqual(testColor, cpr["SystemChromeMediumColor"]);
		Assert.AreEqual(testColor, cpr["SystemChromeMediumLowColor"]);
		Assert.AreEqual(testColor, cpr["SystemChromeWhiteColor"]);
	}

	[TestMethod]
	public void When_Setting_All_Alt_Properties()
	{
		var cpr = new ColorPaletteResources();
		var testColor = Colors.Orange;

		cpr.AltHigh = testColor;
		cpr.AltLow = testColor;
		cpr.AltMedium = testColor;
		cpr.AltMediumHigh = testColor;
		cpr.AltMediumLow = testColor;

		Assert.AreEqual(testColor, cpr["SystemAltHighColor"]);
		Assert.AreEqual(testColor, cpr["SystemAltLowColor"]);
		Assert.AreEqual(testColor, cpr["SystemAltMediumColor"]);
		Assert.AreEqual(testColor, cpr["SystemAltMediumHighColor"]);
		Assert.AreEqual(testColor, cpr["SystemAltMediumLowColor"]);
	}

	[TestMethod]
	public void When_Setting_All_Base_Properties()
	{
		var cpr = new ColorPaletteResources();
		var testColor = Colors.Cyan;

		cpr.BaseHigh = testColor;
		cpr.BaseLow = testColor;
		cpr.BaseMedium = testColor;
		cpr.BaseMediumHigh = testColor;
		cpr.BaseMediumLow = testColor;

		Assert.AreEqual(testColor, cpr["SystemBaseHighColor"]);
		Assert.AreEqual(testColor, cpr["SystemBaseLowColor"]);
		Assert.AreEqual(testColor, cpr["SystemBaseMediumColor"]);
		Assert.AreEqual(testColor, cpr["SystemBaseMediumHighColor"]);
		Assert.AreEqual(testColor, cpr["SystemBaseMediumLowColor"]);
	}

	[TestMethod]
	public async Task When_Used_In_XAML_ResourceDictionary()
	{
		var xaml = """
			<Grid xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
			      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
				<Grid.Resources>
					<ResourceDictionary>
						<ResourceDictionary.ThemeDictionaries>
							<ColorPaletteResources x:Key="Light" Accent="Blue" BaseHigh="White"/>
							<ColorPaletteResources x:Key="Dark" Accent="LightBlue" BaseHigh="Black"/>
						</ResourceDictionary.ThemeDictionaries>
					</ResourceDictionary>
				</Grid.Resources>
				<TextBlock x:Name="TestTextBlock" Text="Test"/>
			</Grid>
			""";

		var grid = (Grid)XamlReader.Load(xaml);

		await UITestHelper.Load(grid);

		// Verify the ColorPaletteResources are correctly set in theme dictionaries
		var themeDictionaries = grid.Resources.ThemeDictionaries;
		Assert.IsTrue(themeDictionaries.ContainsKey("Light"));
		Assert.IsTrue(themeDictionaries.ContainsKey("Dark"));

		var lightPalette = themeDictionaries["Light"] as ColorPaletteResources;
		var darkPalette = themeDictionaries["Dark"] as ColorPaletteResources;

		Assert.IsNotNull(lightPalette);
		Assert.IsNotNull(darkPalette);

		Assert.AreEqual(Colors.Blue, lightPalette.Accent);
		Assert.AreEqual(Colors.White, lightPalette.BaseHigh);

		Assert.AreEqual(Colors.LightBlue, darkPalette.Accent);
		Assert.AreEqual(Colors.Black, darkPalette.BaseHigh);
	}

	[TestMethod]
	public void When_Count_Reflects_Set_Properties()
	{
		var cpr = new ColorPaletteResources();

		Assert.AreEqual(0, cpr.Count);

		cpr.Accent = Colors.Red;
		Assert.AreEqual(1, cpr.Count);

		cpr.BaseHigh = Colors.White;
		Assert.AreEqual(2, cpr.Count);

		cpr.Accent = null;
		Assert.AreEqual(1, cpr.Count);
	}

	[TestMethod]
	public void When_Clear_Removes_All_Colors()
	{
		var cpr = new ColorPaletteResources();

		cpr.Accent = Colors.Red;
		cpr.BaseHigh = Colors.White;
		cpr.ChromeLow = Colors.Gray;

		cpr.Clear();

		Assert.AreEqual(0, cpr.Count);
		Assert.IsNull(cpr.Accent);
		Assert.IsNull(cpr.BaseHigh);
		Assert.IsNull(cpr.ChromeLow);
	}

	[TestMethod]
	public void When_Inherits_From_ResourceDictionary()
	{
		var cpr = new ColorPaletteResources();

		// Verify it can be used as ResourceDictionary
		ResourceDictionary rd = cpr;
		Assert.IsNotNull(rd);

		// Verify standard dictionary operations work
		cpr["CustomKey"] = "CustomValue";
		Assert.AreEqual("CustomValue", cpr["CustomKey"]);

		// Verify color properties still work alongside custom keys
		cpr.Accent = Colors.Red;
		Assert.AreEqual(Colors.Red, cpr["SystemAccentColor"]);
		Assert.AreEqual("CustomValue", cpr["CustomKey"]);
	}
}
