using System;
using Uno;
using Uno.Extensions;
using Uno.UI.Xaml;
using Windows.UI.Xaml;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls
{
	public sealed partial class XamlControlsResourcesV1 : ResourceDictionary
	{
		public XamlControlsResourcesV1()
		{
#if !__NETSTD_REFERENCE__

			// Perform manually what the SourceGenerator is doing during app.xaml.cs InitializeComponent.
			// Using explicit registration allows for the styles to be linked out when not used
			Uno.UI.FluentTheme.v1.GlobalStaticResources.Initialize();
			Uno.UI.FluentTheme.v1.GlobalStaticResources.RegisterDefaultStyles();
			Uno.UI.FluentTheme.v1.GlobalStaticResources.RegisterResourceDictionariesBySource();
#endif

			UpdateSource();
		}

		private void UpdateSource()
		{
			Source = new Uri(XamlFilePathHelper.AppXIdentifier + XamlFilePathHelper.GetWinUIThemeResourceUrl(1));
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
			DependencyProperty.Register(nameof(UseCompactResources), typeof(bool), typeof(XamlControlsResourcesV1), new FrameworkPropertyMetadata(false));

		public object ControlsResourcesVersion { get; set; }

	}
}
