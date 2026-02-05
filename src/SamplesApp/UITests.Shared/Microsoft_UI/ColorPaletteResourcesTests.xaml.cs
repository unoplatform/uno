using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Samples.Controls;
using Windows.UI;

namespace UITests.Shared.Microsoft_UI;

[Sample("Resources", Name = "ColorPaletteResources", IsManualTest = true, IgnoreInSnapshotTests = true)]
public sealed partial class ColorPaletteResourcesTests : UserControl
{
	private readonly ObservableCollection<ColorSwatchInfo> _activeOverrides = new();
	private readonly Dictionary<string, Border> _colorSwatches = new();
	private readonly Dictionary<string, Color> _currentColors = new();
	private ColorPaletteResources _lightPalette;
	private ColorPaletteResources _darkPalette;

	// Color property definitions
	private static readonly List<ColorPropertyInfo> AllColorProperties = new()
	{
		// Accent
		new("Accent", "SystemAccentColor", "Accent"),
		// Alt
		new("AltHigh", "SystemAltHighColor", "Alt"),
		new("AltLow", "SystemAltLowColor", "Alt"),
		new("AltMedium", "SystemAltMediumColor", "Alt"),
		new("AltMediumHigh", "SystemAltMediumHighColor", "Alt"),
		new("AltMediumLow", "SystemAltMediumLowColor", "Alt"),
		// Base
		new("BaseHigh", "SystemBaseHighColor", "Base"),
		new("BaseLow", "SystemBaseLowColor", "Base"),
		new("BaseMedium", "SystemBaseMediumColor", "Base"),
		new("BaseMediumHigh", "SystemBaseMediumHighColor", "Base"),
		new("BaseMediumLow", "SystemBaseMediumLowColor", "Base"),
		// Chrome
		new("ChromeAltLow", "SystemChromeAltLowColor", "Chrome"),
		new("ChromeBlackHigh", "SystemChromeBlackHighColor", "Chrome"),
		new("ChromeBlackLow", "SystemChromeBlackLowColor", "Chrome"),
		new("ChromeBlackMedium", "SystemChromeBlackMediumColor", "Chrome"),
		new("ChromeBlackMediumLow", "SystemChromeBlackMediumLowColor", "Chrome"),
		new("ChromeDisabledHigh", "SystemChromeDisabledHighColor", "Chrome"),
		new("ChromeDisabledLow", "SystemChromeDisabledLowColor", "Chrome"),
		new("ChromeGray", "SystemChromeGrayColor", "Chrome"),
		new("ChromeHigh", "SystemChromeHighColor", "Chrome"),
		new("ChromeLow", "SystemChromeLowColor", "Chrome"),
		new("ChromeMedium", "SystemChromeMediumColor", "Chrome"),
		new("ChromeMediumLow", "SystemChromeMediumLowColor", "Chrome"),
		new("ChromeWhite", "SystemChromeWhiteColor", "Chrome"),
		// Other
		new("ErrorText", "SystemErrorTextColor", "Other"),
		new("ListLow", "SystemListLowColor", "Other"),
		new("ListMedium", "SystemListMediumColor", "Other"),
	};

	public ColorPaletteResourcesTests()
	{
		this.InitializeComponent();
		this.Loaded += OnLoaded;
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		ColorSwatchesSummary.ItemsSource = _activeOverrides;

		// Get references to the named palettes from ThemeDictionaries
		// Note: "Default" key is used as the Dark theme fallback
		_lightPalette = (ColorPaletteResources)((ResourceDictionary)PreviewControlsContainer.Resources
			.ThemeDictionaries["Light"]).MergedDictionaries[0];
		_darkPalette = (ColorPaletteResources)((ResourceDictionary)PreviewControlsContainer.Resources
			.ThemeDictionaries["Default"]).MergedDictionaries[0];

		CreateColorEditors();
		InitializePalette();
	}

	private void CreateColorEditors()
	{
		var groupedProperties = AllColorProperties.GroupBy(p => p.Category).ToList();

		foreach (var group in groupedProperties)
		{
			var expander = new Microsoft.UI.Xaml.Controls.Expander
			{
				Header = $"{group.Key} ({group.Count()})",
				HorizontalAlignment = HorizontalAlignment.Stretch,
				HorizontalContentAlignment = HorizontalAlignment.Stretch,
				IsExpanded = group.Key == "Accent"
			};

			var stackPanel = new StackPanel { Spacing = 8, Padding = new Thickness(8) };

			foreach (var prop in group)
			{
				var editor = CreateColorEditor(prop);
				stackPanel.Children.Add(editor);
			}

			expander.Content = stackPanel;
			ColorEditorsPanel.Children.Add(expander);
		}
	}

	private Grid CreateColorEditor(ColorPropertyInfo prop)
	{
		var grid = new Grid { ColumnSpacing = 8 };
		grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		var textStack = new StackPanel();
		textStack.Children.Add(new TextBlock { Text = prop.Name, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
		textStack.Children.Add(new TextBlock
		{
			Text = prop.SystemKey,
			Style = (Style)Application.Current.Resources["CaptionTextBlockStyle"],
			Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"]
		});
		grid.Children.Add(textStack);

		var swatch = new Border
		{
			Width = 32,
			Height = 32,
			CornerRadius = new CornerRadius(2),
			BorderThickness = new Thickness(1),
			BorderBrush = (Brush)Application.Current.Resources["ControlStrokeColorDefaultBrush"],
			Background = new SolidColorBrush(Colors.Transparent)
		};
		_colorSwatches[prop.Name] = swatch;

		var colorPicker = new Microsoft.UI.Xaml.Controls.ColorPicker { IsAlphaEnabled = true };
		colorPicker.ColorChanged += (s, args) => OnColorChanged(prop.Name, args.NewColor);

		var flyout = new Flyout { Content = colorPicker };

		var button = new Button
		{
			Width = 40,
			Height = 40,
			Padding = new Thickness(0),
			CornerRadius = new CornerRadius(4),
			Content = swatch,
			Flyout = flyout
		};
		Grid.SetColumn(button, 1);
		grid.Children.Add(button);

		return grid;
	}

	private void InitializePalette()
	{
		_currentColors.Clear();
		_activeOverrides.Clear();

		// Clear all colors from both palette instances by creating fresh instances
		ClearPalette(_lightPalette);
		ClearPalette(_darkPalette);

		// Force theme update to apply the cleared palettes
		ForceThemeUpdate();
		UpdateAllSwatches();
	}

	private static void ClearPalette(ColorPaletteResources palette)
	{
		// Reset all color properties to null (unset)
		palette.Accent = null;
		palette.AltHigh = null;
		palette.AltLow = null;
		palette.AltMedium = null;
		palette.AltMediumHigh = null;
		palette.AltMediumLow = null;
		palette.BaseHigh = null;
		palette.BaseLow = null;
		palette.BaseMedium = null;
		palette.BaseMediumHigh = null;
		palette.BaseMediumLow = null;
		palette.ChromeAltLow = null;
		palette.ChromeBlackHigh = null;
		palette.ChromeBlackLow = null;
		palette.ChromeBlackMedium = null;
		palette.ChromeBlackMediumLow = null;
		palette.ChromeDisabledHigh = null;
		palette.ChromeDisabledLow = null;
		palette.ChromeGray = null;
		palette.ChromeHigh = null;
		palette.ChromeLow = null;
		palette.ChromeMedium = null;
		palette.ChromeMediumLow = null;
		palette.ChromeWhite = null;
		palette.ErrorText = null;
		palette.ListLow = null;
		palette.ListMedium = null;
	}

	private void ApplyPaletteToContainer()
	{
		// Update colors on both Light and Dark palette instances
		foreach (var kvp in _currentColors)
		{
			SetColorOnPalette(_lightPalette, kvp.Key, kvp.Value);
			SetColorOnPalette(_darkPalette, kvp.Key, kvp.Value);
		}

		// Force theme update to trigger resource re-resolution
		ForceThemeUpdate();
	}

	private void ForceThemeUpdate()
	{
		// Use DispatcherQueue to allow UI to process theme changes (pattern from Fluent Theme Editor)
		_ = DispatcherQueue.TryEnqueue(() =>
		{
			var current = PreviewControlsContainer.RequestedTheme;
			if (current == ElementTheme.Light)
			{
				PreviewControlsContainer.RequestedTheme = ElementTheme.Dark;
				PreviewControlsContainer.RequestedTheme = ElementTheme.Light;
			}
			else if (current == ElementTheme.Dark)
			{
				PreviewControlsContainer.RequestedTheme = ElementTheme.Light;
				PreviewControlsContainer.RequestedTheme = ElementTheme.Dark;
			}
			else // Default
			{
				PreviewControlsContainer.RequestedTheme = ElementTheme.Light;
				PreviewControlsContainer.RequestedTheme = ElementTheme.Dark;
				PreviewControlsContainer.RequestedTheme = ElementTheme.Default;
			}
		});
	}

	private static void SetColorOnPalette(ColorPaletteResources palette, string propertyName, Color color)
	{
		switch (propertyName)
		{
			case "Accent": palette.Accent = color; break;
			case "AltHigh": palette.AltHigh = color; break;
			case "AltLow": palette.AltLow = color; break;
			case "AltMedium": palette.AltMedium = color; break;
			case "AltMediumHigh": palette.AltMediumHigh = color; break;
			case "AltMediumLow": palette.AltMediumLow = color; break;
			case "BaseHigh": palette.BaseHigh = color; break;
			case "BaseLow": palette.BaseLow = color; break;
			case "BaseMedium": palette.BaseMedium = color; break;
			case "BaseMediumHigh": palette.BaseMediumHigh = color; break;
			case "BaseMediumLow": palette.BaseMediumLow = color; break;
			case "ChromeAltLow": palette.ChromeAltLow = color; break;
			case "ChromeBlackHigh": palette.ChromeBlackHigh = color; break;
			case "ChromeBlackLow": palette.ChromeBlackLow = color; break;
			case "ChromeBlackMedium": palette.ChromeBlackMedium = color; break;
			case "ChromeBlackMediumLow": palette.ChromeBlackMediumLow = color; break;
			case "ChromeDisabledHigh": palette.ChromeDisabledHigh = color; break;
			case "ChromeDisabledLow": palette.ChromeDisabledLow = color; break;
			case "ChromeGray": palette.ChromeGray = color; break;
			case "ChromeHigh": palette.ChromeHigh = color; break;
			case "ChromeLow": palette.ChromeLow = color; break;
			case "ChromeMedium": palette.ChromeMedium = color; break;
			case "ChromeMediumLow": palette.ChromeMediumLow = color; break;
			case "ChromeWhite": palette.ChromeWhite = color; break;
			case "ErrorText": palette.ErrorText = color; break;
			case "ListLow": palette.ListLow = color; break;
			case "ListMedium": palette.ListMedium = color; break;
		}
	}

	private void UpdateAllSwatches()
	{
		foreach (var prop in AllColorProperties)
		{
			var currentColor = GetCurrentColor(prop.Name);
			if (_colorSwatches.TryGetValue(prop.Name, out var swatch))
			{
				swatch.Background = new SolidColorBrush(currentColor);
			}
		}
	}

	private Color GetCurrentColor(string propertyName)
	{
		// Get the color from current overrides if set
		if (_currentColors.TryGetValue(propertyName, out var overrideColor))
		{
			return overrideColor;
		}

		// Try to get from theme resources
		var prop = AllColorProperties.FirstOrDefault(p => p.Name == propertyName);
		if (prop != null && Application.Current.Resources.TryGetValue(prop.SystemKey, out var resource) && resource is Color themeColor)
		{
			return themeColor;
		}

		return Colors.Transparent;
	}

	private void OnColorChanged(string propertyName, Color newColor)
	{
		// Store the color in our dictionary
		_currentColors[propertyName] = newColor;

		// Update the swatch
		if (_colorSwatches.TryGetValue(propertyName, out var swatch))
		{
			swatch.Background = new SolidColorBrush(newColor);
		}

		// Update active overrides list
		var prop = AllColorProperties.FirstOrDefault(p => p.Name == propertyName);
		var existingOverride = _activeOverrides.FirstOrDefault(x => x.Name == propertyName);
		if (existingOverride != null)
		{
			existingOverride.Color = new SolidColorBrush(newColor);
			existingOverride.HexValue = ColorToHex(newColor);
		}
		else if (prop != null)
		{
			_activeOverrides.Add(new ColorSwatchInfo
			{
				Name = propertyName,
				SystemKey = prop.SystemKey,
				Color = new SolidColorBrush(newColor),
				HexValue = ColorToHex(newColor)
			});
		}

		// Update colors on both palette instances directly
		SetColorOnPalette(_lightPalette, propertyName, newColor);
		SetColorOnPalette(_darkPalette, propertyName, newColor);

		// Force theme update to trigger resource re-resolution
		ForceThemeUpdate();
	}

	private static string ColorToHex(Color c) => $"#{c.A:X2}{c.R:X2}{c.G:X2}{c.B:X2}";

	#region Preset Themes

	private void OnResetClick(object sender, RoutedEventArgs e)
	{
		InitializePalette();
	}

	private void OnOceanClick(object sender, RoutedEventArgs e)
	{
		InitializePalette();
		ApplyPreset(new Dictionary<string, Color>
		{
			["Accent"] = Color.FromArgb(255, 0, 119, 182),       // #0077B6
			["BaseHigh"] = Color.FromArgb(255, 3, 4, 94),        // #03045E
			["ChromeHigh"] = Color.FromArgb(255, 144, 224, 239), // #90E0EF
			["BaseMedium"] = Color.FromArgb(255, 0, 150, 199),   // #0096C7
			["ChromeMedium"] = Color.FromArgb(255, 72, 202, 228) // #48CAE4
		});
	}

	private void OnForestClick(object sender, RoutedEventArgs e)
	{
		InitializePalette();
		ApplyPreset(new Dictionary<string, Color>
		{
			["Accent"] = Color.FromArgb(255, 46, 125, 50),       // #2E7D32
			["BaseHigh"] = Color.FromArgb(255, 27, 94, 32),      // #1B5E20
			["ChromeHigh"] = Color.FromArgb(255, 165, 214, 167), // #A5D6A7
			["BaseMedium"] = Color.FromArgb(255, 56, 142, 60),   // #388E3C
			["ChromeMedium"] = Color.FromArgb(255, 129, 199, 132) // #81C784
		});
	}

	private void OnSunsetClick(object sender, RoutedEventArgs e)
	{
		InitializePalette();
		ApplyPreset(new Dictionary<string, Color>
		{
			["Accent"] = Color.FromArgb(255, 255, 87, 34),       // #FF5722
			["BaseHigh"] = Color.FromArgb(255, 191, 54, 12),     // #BF360C
			["ChromeHigh"] = Color.FromArgb(255, 255, 171, 145), // #FFAB91
			["BaseMedium"] = Color.FromArgb(255, 244, 81, 30),   // #F4511E
			["ChromeMedium"] = Color.FromArgb(255, 255, 138, 101) // #FF8A65
		});
	}

	private void ApplyPreset(Dictionary<string, Color> colors)
	{
		foreach (var kvp in colors)
		{
			// Store in our dictionary
			_currentColors[kvp.Key] = kvp.Value;

			// Update swatch
			if (_colorSwatches.TryGetValue(kvp.Key, out var swatch))
			{
				swatch.Background = new SolidColorBrush(kvp.Value);
			}

			// Add to active overrides
			var prop = AllColorProperties.FirstOrDefault(p => p.Name == kvp.Key);
			if (prop != null)
			{
				_activeOverrides.Add(new ColorSwatchInfo
				{
					Name = kvp.Key,
					SystemKey = prop.SystemKey,
					Color = new SolidColorBrush(kvp.Value),
					HexValue = ColorToHex(kvp.Value)
				});
			}
		}

		// Create new palette instance and replace in MergedDictionaries
		ApplyPaletteToContainer();
	}

	#endregion
}

public class ColorPropertyInfo
{
	public string Name { get; }
	public string SystemKey { get; }
	public string Category { get; }

	public ColorPropertyInfo(string name, string systemKey, string category)
	{
		Name = name;
		SystemKey = systemKey;
		Category = category;
	}
}

public class ColorSwatchInfo
{
	public string Name { get; set; }
	public string SystemKey { get; set; }
	public SolidColorBrush Color { get; set; }
	public string HexValue { get; set; }
}
