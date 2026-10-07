// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\dll-tabular\TabularControlsResources.cpp, tag winui3/release/2.5.4-experimental, commit 7b127093475

#nullable enable

using System;
using Uno.UI.Xaml;

namespace Microsoft.UI.Xaml.Controls.Tabular;

/// <summary>
/// Default styles and theme resources for the tabular controls (TableView). Merge it into the
/// application resources, after XamlControlsResources.
/// </summary>
[global::Windows.Foundation.Metadata.Experimental]
public sealed partial class TabularControlsResources : ResourceDictionary
{
	public TabularControlsResources()
	{
		// On Windows, we need to add theme resources manually.  We'll still add an instance of this element to get the rest of
		// what it does, though.
		// MUXControlsFactory::EnsureInitialized();
#if !__NETSTD_REFERENCE__
		// Uno specific: the same explicit registration XamlControlsResources performs, so the Tabular dictionaries
		// (registered by the Uno.UI.FluentTheme assembly) resolve even when this is the first theme dictionary created.
		Uno.UI.FluentTheme.GlobalStaticResources.Initialize();
		Uno.UI.FluentTheme.GlobalStaticResources.RegisterDefaultStyles();
		Uno.UI.FluentTheme.GlobalStaticResources.RegisterResourceDictionariesBySource();
#endif
		UpdateSource();
	}

	private void UpdateSource()
	{
		// TABULAR_BINARY_EMITS_THEME_RESOURCES is defined for the Tabular binary (dll-tabular\Microsoft.UI.Xaml.Common.props),
		// so the scaffolding branch ("Keep scaffolding source-free so it doesn't bind or require MUXC theme resources.") is not taken.
		const bool isPerf2026Enabled = false; // TODO: Decide based on opt-in flag, task.ms/60958581

		Uri uri = new(GetThemeResourcesUri());

		string GetThemeResourcesUri()
		{
			// Authority-less: a consuming app's build folds this component's PRI into its own and
			// drops component root map names. Path must match AppxPriInitialPath.
			string packagePrefix = XamlFilePathHelper.AppXIdentifier + XamlFilePathHelper.TabularRootNamespace + "/Themes/";
			// TODO Uno: Uno.UI.FluentTheme registers the merged Tabular theme resources under the themeresources.xaml name only.
			string postfix = isPerf2026Enabled ? "themeresources_perf2026.xaml" : XamlFilePathHelper.TabularThemeResourceFileName;

			return packagePrefix + postfix;
		}

		// Workaround a pre-RS5 XAML bug: changing ResourceDictionary.Source didn't clear ThemeDictionaries.
		ThemeDictionaries.Clear();
		Source = uri;

		// FUTURE: This remaining AcrylicBrush lookup is a workaround to force the Nullable<Double> type to be
		// registered as a known type, which somehow doesn't otherwise get registered for some apps which need it.
		// At some point, the underlying issue should be investigated and fixed.
		// Guard the lookup because scaffolding has no Default theme dictionary.
		const string c_AcrylicBackgroundFillColorDefaultBrush = "AcrylicBackgroundFillColorDefaultBrush";
		var themeDictionaries = ThemeDictionaries;
		if (themeDictionaries.ContainsKey("Default"))
		{
			if (themeDictionaries["Default"] is ResourceDictionary defaultThemeDictionary)
			{
				if (defaultThemeDictionary.ContainsKey(c_AcrylicBackgroundFillColorDefaultBrush, shouldCheckSystem: false))
				{
					_ = defaultThemeDictionary[c_AcrylicBackgroundFillColorDefaultBrush];
				}
			}
		}
	}
}
