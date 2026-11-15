#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using SampleControl.Presentation;
using Uno.UI.RuntimeTests;
using Uno.UI.Samples.Helper;
using Windows.UI;

namespace SamplesApp.Tests;

[TestClass]
[RunsOnUIThread]
public class Given_ShellResources
{
	// WinUI 3 SystemFillColorSuccess per theme; proves the requested theme was really applied.
	private static readonly Color _lightSuccess = Color.FromArgb(0xFF, 0x0F, 0x7B, 0x0F);
	private static readonly Color _darkSuccess = Color.FromArgb(0xFF, 0x6C, 0xCB, 0x5F);

	private static ResourceDictionary? _shellResources;

	private static ResourceDictionary ShellResources
		=> _shellResources ??= new ResourceDictionary { Source = new Uri(ShellThemeBrushes.ResourcesUri) };

	// The shell's theme usually differs from the app's (it is set on the root content), so both must follow the element.
	[TestMethod]
	[DataRow(ElementTheme.Light)]
	[DataRow(ElementTheme.Dark)]
	public async Task When_Themed_Brushes_Follow_Element_Theme(ElementTheme theme)
	{
		var root = await LoadThemedAsync(theme);
		var themeKey = ShellThemeBrushes.GetThemeKey(theme, isHighContrast: false);

		Assert.AreEqual(theme == ElementTheme.Dark ? _darkSuccess : _lightSuccess, GetBackground(root, "ShellTestPassedBrush"));
		foreach (var key in ShellThemeBrushes.ShellBrushKeys)
		{
			Assert.AreEqual(GetColor(key, themeKey), GetBackground(root, key), $"{key} in {theme}: XAML and ShellThemeBrushes disagree");
		}

		Assert.AreEqual(GetColor("CardBackgroundFillColorDefaultBrush", themeKey), GetBackground(root, "Card"));
	}

	[TestMethod]
	[DataRow(ElementTheme.Light)]
	[DataRow(ElementTheme.Dark)]
	public async Task When_Themed_Styles_Resolve_Shape(ElementTheme theme)
	{
		var root = await LoadThemedAsync(theme);

		var card = (Border)root.FindName("Card");
		Assert.AreEqual(new CornerRadius(8), card.CornerRadius);
		Assert.AreEqual(new Thickness(1), card.BorderThickness);
		Assert.AreEqual(new Thickness(16, 12, 16, 12), card.Padding);

		var tag = (Border)root.FindName("Tag");
		Assert.AreEqual(new CornerRadius(4), tag.CornerRadius);

		var mono = (TextBlock)root.FindName("Mono");
		Assert.AreEqual(12d, mono.FontSize);
		Assert.IsTrue(mono.IsTextSelectionEnabled);
	}

	// Consolas on Windows, the host's own monospace family elsewhere (ShellMonospace).
	[TestMethod]
	public async Task When_Monospace_Style_Has_Fixed_Advance()
	{
		if (OperatingSystem.IsBrowser())
		{
			Assert.Inconclusive("The browser has no local monospace family to resolve.");
		}

		var root = await LoadThemedAsync(ElementTheme.Dark);

		var narrow = ((TextBlock)root.FindName("MonoNarrow")).ActualWidth;
		var wide = ((TextBlock)root.FindName("MonoWide")).ActualWidth;
		Assert.IsTrue(narrow > 0, "The text should be measured");
		Assert.AreEqual(wide, narrow, 0.5, "Narrow and wide glyphs should have the same advance");
	}

	[TestMethod]
	public void When_Theme_Dictionaries_Have_No_Default()
	{
		var themes = ShellResources.ThemeDictionaries;

		Assert.IsFalse(themes.ContainsKey("Default"));
		Assert.AreEqual(3, themes.Count);
		Assert.AreEqual(56d, ShellResources["ShellHeaderHeight"]);
		Assert.AreEqual(32d, ShellResources["ShellRowMinHeight"]);
	}

	[TestMethod]
	[DataRow(ShellThemeBrushes.LightKey, 1d)]
	[DataRow(ShellThemeBrushes.DarkKey, 1d)]
	[DataRow(ShellThemeBrushes.HighContrastKey, 2d)]
	public void When_Every_Theme_Defines_Every_Token(string themeKey, double cardBorder)
	{
		var theme = (ResourceDictionary)ShellResources.ThemeDictionaries[themeKey];

		Assert.AreEqual(new Thickness(cardBorder), theme["ShellCardBorderThickness"]);
		Assert.AreEqual(ShellThemeBrushes.ShellBrushKeys.Count() + 1, theme.Count, "Every theme defines the same tokens");
		foreach (var key in ShellThemeBrushes.ShellBrushKeys)
		{
			Assert.IsTrue(theme.ContainsKey(key), $"{key} missing in {themeKey}");
			Assert.IsInstanceOfType(ShellThemeBrushes.Get(key, themeKey), typeof(SolidColorBrush), $"{key} in {themeKey}");
		}
	}

	[TestMethod]
	[DataRow(ShellThemeBrushes.LightKey)]
	[DataRow(ShellThemeBrushes.DarkKey)]
	public void When_Getting_Brush_From_Code_Theme_Is_Respected(string themeKey)
		=> Assert.AreEqual(themeKey == ShellThemeBrushes.DarkKey ? _darkSuccess : _lightSuccess, GetColor("ShellTestPassedBrush", themeKey));

	[TestMethod]
	public void When_Getting_Unknown_Brush_Returns_Null()
		=> Assert.IsNull(ShellThemeBrushes.Get("ShellNoSuchBrush", ShellThemeBrushes.DarkKey));

	[TestMethod]
	[DataRow(ElementTheme.Dark, false, ShellThemeBrushes.DarkKey)]
	[DataRow(ElementTheme.Light, false, ShellThemeBrushes.LightKey)]
	[DataRow(ElementTheme.Dark, true, ShellThemeBrushes.HighContrastKey)]
	public void When_Mapping_Theme_Key(ElementTheme theme, bool isHighContrast, string expected)
		=> Assert.AreEqual(expected, ShellThemeBrushes.GetThemeKey(theme, isHighContrast));

	[TestMethod]
	public void When_Shell_Functions_Get_Nulls()
	{
		Assert.AreEqual(0, ShellFunctions.Take(null, 8).Count);
		Assert.AreEqual(Visibility.Collapsed, ShellFunctions.ShowDescription(null, false, false));
		Assert.AreEqual(Visibility.Collapsed, ShellFunctions.VisibleIfNotEmpty(" "));
		Assert.AreEqual(string.Empty, ShellFunctions.RowName(null, false, false));
	}

	[TestMethod]
	public void When_Shell_Functions_Shape_Values()
	{
		Assert.AreEqual(8, ShellFunctions.Take(new List<int>(new int[20]), 8).Count);
		Assert.AreEqual(Visibility.Visible, ShellFunctions.ShowDescription("Text", false, false));
		Assert.AreEqual(Visibility.Collapsed, ShellFunctions.ShowDescription("Text", true, false));
		Assert.AreEqual(Visibility.Collapsed, ShellFunctions.ShowDescription("Text", false, true));
		Assert.AreEqual("Button_Events, manual test, favorite", ShellFunctions.RowName("Button_Events", true, true));
		Assert.AreEqual("Remove from favorites", ShellFunctions.FavoriteLabel(true));
		Assert.IsTrue(ShellFunctions.Not(false));
	}

	[TestMethod]
	public void When_Shell_Has_View_Model_Header_Title_Is_Bound()
	{
		var vm = SampleChooserViewModel.Instance;
		var title = (TextBlock)vm.Owner.FindName("ShellSampleTitle");

		Assert.AreSame(vm, vm.Owner.ViewModel);
		Assert.IsNotNull(vm.CurrentSelectedSample);
		Assert.AreEqual(vm.CurrentSelectedSample.ControlName, title.Text, "The x:Bind title should show the current sample");
	}

	private static async Task<FrameworkElement> LoadThemedAsync(ElementTheme theme)
	{
		var brushBorders = string.Concat(ShellThemeBrushes.ShellBrushKeys.Select(key => $$"""<Border x:Name="{{key}}" Background="{ThemeResource {{key}}}" />"""));

		var root = (FrameworkElement)XamlReader.Load($$"""
			<Grid xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
				xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
				RequestedTheme="{{theme}}">
				<Grid.Resources>
					<ResourceDictionary>
						<ResourceDictionary.MergedDictionaries>
							<ResourceDictionary Source="{{ShellThemeBrushes.ResourcesUri}}" />
						</ResourceDictionary.MergedDictionaries>
					</ResourceDictionary>
				</Grid.Resources>
				<StackPanel>
					{{brushBorders}}
					<Border x:Name="Tag" Style="{StaticResource ShellTagStyle}" />
					<Border x:Name="Card" Style="{StaticResource ShellCardStyle}" />
					<TextBlock x:Name="Mono" Style="{StaticResource ShellMonospaceTextStyle}" Text="Mono" />
					<TextBlock x:Name="MonoNarrow" HorizontalAlignment="Left" Style="{StaticResource ShellMonospaceTextStyle}" Text="iiiiiiii" />
					<TextBlock x:Name="MonoWide" HorizontalAlignment="Left" Style="{StaticResource ShellMonospaceTextStyle}" Text="WWWWWWWW" />
				</StackPanel>
			</Grid>
			""");

		TestServices.WindowHelper.WindowContent = root;
		await TestServices.WindowHelper.WaitForLoaded(root);
		await TestServices.WindowHelper.WaitForIdle();

		return root;
	}

	private static Color GetColor(string key, string themeKey)
		=> ((SolidColorBrush)ShellThemeBrushes.Get(key, themeKey)!).Color;

	private static Color GetBackground(FrameworkElement root, string name)
		=> ((SolidColorBrush)((Border)root.FindName(name)).Background).Color;
}
