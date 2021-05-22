using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uno;
using Uno.UI.Xaml;
using Windows.UI.Xaml;

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
#endif

			UpdateSource();
		}

		private void UpdateSource() => Source = new Uri(XamlFilePathHelper.AppXIdentifier + XamlFilePathHelper.WinUIThemeResourceURL);

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
			DependencyProperty.Register(nameof(UseCompactResources), typeof(bool), typeof(XamlControlsResources), new PropertyMetadata(false));

		[NotImplemented]
		public ControlsResourcesVersion ControlsResourcesVersion
		{
			get => (ControlsResourcesVersion)GetValue(ControlsResourcesVersionProperty);
			set => SetValue(ControlsResourcesVersionProperty, value);
		}

		[NotImplemented]
		public static DependencyProperty ControlsResourcesVersionProperty { get; } =
			DependencyProperty.Register(nameof(ControlsResourcesVersion), typeof(ControlsResourcesVersion), typeof(XamlControlsResources), new PropertyMetadata(ControlsResourcesVersion.Version1));
	}
}
