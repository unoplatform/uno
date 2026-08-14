using System;
using Uno;
using Uno.UI.Xaml;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace Microsoft.UI.Xaml.Controls
{
	public sealed partial class XamlControlsResources : ResourceDictionary
	{
		public XamlControlsResources()
		{
#if !__NETSTD_REFERENCE__

			// Perform manually what the SourceGenerator is doing during app.xaml.cs InitializeComponent.
			// Using explicit registration allows for the styles to be linked out when not used
			Uno.UI.FluentTheme.GlobalStaticResources.Initialize();
			Uno.UI.FluentTheme.GlobalStaticResources.RegisterDefaultStyles();
			Uno.UI.FluentTheme.GlobalStaticResources.RegisterResourceDictionariesBySource();

			Uno.UI.FluentTheme.v2.GlobalStaticResources.Initialize();
			Uno.UI.FluentTheme.v2.GlobalStaticResources.RegisterDefaultStyles();
			Uno.UI.FluentTheme.v2.GlobalStaticResources.RegisterResourceDictionariesBySource();
#endif

			Source = new Uri(XamlFilePathHelper.AppXIdentifier + XamlFilePathHelper.WinUIThemeResourceURL);

			// Our ported Fluent dictionaries omit the TintLuminosityOpacity that WinUI sets
			// inline on these AcrylicBrush resources; without it the luminosity layer computes
			// an alpha far too low and acrylic renders nearly opaque.
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
			DependencyProperty.Register(nameof(UseCompactResources), typeof(bool), typeof(XamlControlsResources), new FrameworkPropertyMetadata(false));
	}
}
