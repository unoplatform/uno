using System;
using Uno;
using Uno.Extensions;
using Uno.UI.Xaml;
using Windows.UI.Xaml;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls
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
