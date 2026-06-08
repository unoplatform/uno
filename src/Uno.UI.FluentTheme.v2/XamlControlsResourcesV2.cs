using System;
using Uno;
using Uno.Extensions;
using Uno.UI.Xaml;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace Microsoft.UI.Xaml.Controls
{
	public sealed partial class XamlControlsResourcesV2 : ResourceDictionary
	{
		public XamlControlsResourcesV2()
		{
#if !__NETSTD_REFERENCE__

			// Perform manually what the SourceGenerator is doing during app.xaml.cs InitializeComponent.
			// Using explicit registration allows for the styles to be linked out when not used
			Uno.UI.FluentTheme.v2.GlobalStaticResources.Initialize();
			Uno.UI.FluentTheme.v2.GlobalStaticResources.RegisterDefaultStyles();
			Uno.UI.FluentTheme.v2.GlobalStaticResources.RegisterResourceDictionariesBySource();
#endif

			UpdateSource();
		}

		private void UpdateSource()
		{
			Source = new Uri(XamlFilePathHelper.AppXIdentifier + XamlFilePathHelper.GetWinUIThemeResourceUrl(2));

			// WinUI sets TintLuminosityOpacity programmatically on acrylic brushes
			// because nullable doubles couldn't be set in XAML on older Windows versions.
			// Without these values, the luminosity layer uses a computed alpha that is
			// far too low, causing acrylic to appear nearly opaque/dark.
			UpdateAcrylicBrushes();
		}

		private void UpdateAcrylicBrushes()
		{
			if (ThemeDictionaries.TryGetValue("Default", out var darkTheme))
			{
				UpdateAcrylicBrushesDarkTheme(darkTheme);
			}

			if (ThemeDictionaries.TryGetValue("Light", out var lightTheme))
			{
				UpdateAcrylicBrushesLightTheme(lightTheme);
			}
		}

		private static void UpdateAcrylicBrushesLightTheme(object themeDictionary)
		{
			if (themeDictionary is ResourceDictionary dictionary)
			{
				UpdateTintLuminosityOpacity(dictionary, "AcrylicBackgroundFillColorDefaultBrush", 0.85);
				UpdateTintLuminosityOpacity(dictionary, "AcrylicInAppFillColorDefaultBrush", 0.85);
				UpdateTintLuminosityOpacity(dictionary, "AcrylicBackgroundFillColorDefaultInverseBrush", 0.96);
				UpdateTintLuminosityOpacity(dictionary, "AcrylicInAppFillColorDefaultInverseBrush", 0.96);
				UpdateTintLuminosityOpacity(dictionary, "AcrylicBackgroundFillColorBaseBrush", 0.9);
				UpdateTintLuminosityOpacity(dictionary, "AcrylicInAppFillColorBaseBrush", 0.9);
				UpdateTintLuminosityOpacity(dictionary, "AccentAcrylicBackgroundFillColorDefaultBrush", 0.9);
				UpdateTintLuminosityOpacity(dictionary, "AccentAcrylicInAppFillColorDefaultBrush", 0.9);
				UpdateTintLuminosityOpacity(dictionary, "AccentAcrylicBackgroundFillColorBaseBrush", 0.9);
				UpdateTintLuminosityOpacity(dictionary, "AccentAcrylicInAppFillColorBaseBrush", 0.9);
			}
		}

		private static void UpdateAcrylicBrushesDarkTheme(object themeDictionary)
		{
			if (themeDictionary is ResourceDictionary dictionary)
			{
				UpdateTintLuminosityOpacity(dictionary, "AcrylicBackgroundFillColorDefaultBrush", 0.96);
				UpdateTintLuminosityOpacity(dictionary, "AcrylicInAppFillColorDefaultBrush", 0.96);
				UpdateTintLuminosityOpacity(dictionary, "AcrylicBackgroundFillColorDefaultInverseBrush", 0.85);
				UpdateTintLuminosityOpacity(dictionary, "AcrylicInAppFillColorDefaultInverseBrush", 0.85);
				UpdateTintLuminosityOpacity(dictionary, "AcrylicBackgroundFillColorBaseBrush", 0.96);
				UpdateTintLuminosityOpacity(dictionary, "AcrylicInAppFillColorBaseBrush", 0.96);
				UpdateTintLuminosityOpacity(dictionary, "AccentAcrylicBackgroundFillColorDefaultBrush", 0.8);
				UpdateTintLuminosityOpacity(dictionary, "AccentAcrylicInAppFillColorDefaultBrush", 0.8);
				UpdateTintLuminosityOpacity(dictionary, "AccentAcrylicBackgroundFillColorBaseBrush", 0.8);
				UpdateTintLuminosityOpacity(dictionary, "AccentAcrylicInAppFillColorBaseBrush", 0.8);
			}
		}

		private static void UpdateTintLuminosityOpacity(ResourceDictionary dictionary, string brushKey, double luminosityValue)
		{
			if (dictionary.TryGetValue(brushKey, out var value) && value is AcrylicBrush brush)
			{
				brush.TintLuminosityOpacity = luminosityValue;
			}
		}

		[NotImplemented]
		public static void EnsureRevealLights(UIElement element) { }

		[NotImplemented]
		public bool UseCompactResources
		{
			get => (bool)GetValue(UseCompactResourcesProperty);
			set => SetValue(UseCompactResourcesProperty, value);
		}

		[NotImplemented]
		public static DependencyProperty UseCompactResourcesProperty { get; } =
			DependencyProperty.Register(nameof(UseCompactResources), typeof(bool), typeof(XamlControlsResourcesV2), new FrameworkPropertyMetadata(false));

		public object ControlsResourcesVersion { get; set; }
	}
}
