﻿using System;
using Uno;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI.Xaml;
using Microsoft.UI.Xaml;

namespace Microsoft.UI.Xaml.Controls
{
	public sealed partial class XamlControlsResources : ResourceDictionary
	{
		private const ControlsResourcesVersion MaxSupportedResourcesVersion = ControlsResourcesVersion.Version2;

		private static bool _isUsingResourcesVersion2 = true;

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

		private void UpdateSource()
		{
			var requestedVersion = ControlsResourcesVersion;
			if (ControlsResourcesVersion > MaxSupportedResourcesVersion)
			{
				if (this.Log().IsEnabled(LogLevel.Warning))
				{
					this.Log().LogWarning($"" +
						$"WinUI resources version {ControlsResourcesVersion} is not supported " +
						$"in Uno Platform yet. Falling back to {MaxSupportedResourcesVersion} styles.");
				}
				requestedVersion = MaxSupportedResourcesVersion;
			}

			requestedVersion = ControlsResourcesVersion.Version2; // Force version 2, as 1 is no longer supported

#if !__NETSTD_REFERENCE__
			Uno.UI.FluentTheme.v2.GlobalStaticResources.Initialize();
			Uno.UI.FluentTheme.v2.GlobalStaticResources.RegisterDefaultStyles();
			Uno.UI.FluentTheme.v2.GlobalStaticResources.RegisterResourceDictionariesBySource();
#endif

			Source = new Uri(XamlFilePathHelper.AppXIdentifier + XamlFilePathHelper.GetWinUIThemeResourceUrl((int)requestedVersion));

			_isUsingResourcesVersion2 = requestedVersion == ControlsResourcesVersion.Version2;
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

		public ControlsResourcesVersion ControlsResourcesVersion
		{
			get => (ControlsResourcesVersion)GetValue(ControlsResourcesVersionProperty);
			set => SetValue(ControlsResourcesVersionProperty, value);
		}

		public static DependencyProperty ControlsResourcesVersionProperty { get; } =
			DependencyProperty.Register(nameof(ControlsResourcesVersion), typeof(ControlsResourcesVersion), typeof(XamlControlsResources), new PropertyMetadata(ControlsResourcesVersion.Version2, OnControlsResourcesVersionChanged));

		private static void OnControlsResourcesVersionChanged(DependencyObject owner, DependencyPropertyChangedEventArgs args)
		{
			var resources = owner as XamlControlsResources;
			resources?.UpdateSource();
		}
	}
}
