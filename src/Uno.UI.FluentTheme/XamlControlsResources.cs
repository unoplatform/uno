using System;
using Uno;
using Uno.UI.Xaml;
using Microsoft.UI.Xaml;

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

			Source = new Uri(XamlFilePathHelper.AppXIdentifier + XamlFilePathHelper.WinUIThemeResourceURL);

			ApplyStyleOptimizations();
		}

		/// <summary>
		/// Overlays the optimized (perf2026) control styles when the app opted in through
		/// <see cref="Microsoft.UI.Xaml.Settings.XamlChangeId.DefaultStyleOptimizations"/>.
		/// </summary>
		private void ApplyStyleOptimizations()
		{
			if (Microsoft.UI.Xaml.Settings.XamlOptionalChanges.IsChangeEnabled(Microsoft.UI.Xaml.Settings.XamlChangeId.DefaultStyleOptimizations))
			{
				OverlayFrom(new Uno.UI.FluentTheme.Perf2026Resources());
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
