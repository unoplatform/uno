#nullable enable

using System;
using System.Runtime.Loader;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml;
using Uno.UI;

namespace Uno.UI.Tests.Windows_UI_Xaml;

/// <summary>
/// <see cref="FeatureConfiguration.Style.UseUWPDefaultStylesOverride"/> is a process-lifetime
/// dictionary keyed by control <see cref="Type"/>. A downstream host that loads previewed apps
/// into their own collectible AssemblyLoadContexts may see an app configure overrides for its own
/// control types; those keys then pin the app's context after unload. Because the dictionary is
/// USER CONFIGURATION (never rebuilt), the sweep
/// (<see cref="Style.RemoveAlcScopedUserStyleOverrides"/>, called from the ALC cleanup hook) is
/// scoped to the DYING context only: default-ALC keys and a live sibling secondary ALC's keys
/// must both survive — a wholesale all-non-default sweep would silently delete a live app's
/// configuration.
/// </summary>
[TestClass]
public class Given_ResidualTypeStatics_Alc
{
	[TestMethod]
	public void When_RemoveAlcScopedUserStyleOverrides_Then_Dying_Alc_Swept_And_Siblings_Kept()
	{
		var overrides = FeatureConfiguration.Style.UseUWPDefaultStylesOverride;

		// A default-ALC key stands in for a framework/host control type; it must survive.
		var defaultAlcKey = typeof(Given_ResidualTypeStatics_Alc);

		var dyingAlc = new AssemblyLoadContext("Given_ResidualTypeStatics_Alc.dying", isCollectible: true);
		var siblingAlc = new AssemblyLoadContext("Given_ResidualTypeStatics_Alc.sibling", isCollectible: true);
		Type? siblingKey = null;
		try
		{
			var dyingKey = dyingAlc
				.LoadFromAssemblyPath(defaultAlcKey.Assembly.Location)
				.GetType(defaultAlcKey.FullName!, throwOnError: true)!;
			siblingKey = siblingAlc
				.LoadFromAssemblyPath(defaultAlcKey.Assembly.Location)
				.GetType(defaultAlcKey.FullName!, throwOnError: true)!;

			overrides[defaultAlcKey] = false;
			overrides[dyingKey] = false;
			overrides[siblingKey] = false;

			Assert.IsTrue(overrides.ContainsKey(dyingKey), "Pre-condition: the dying ALC's key must be present.");

			Style.RemoveAlcScopedUserStyleOverrides(dyingAlc);

			Assert.IsFalse(
				overrides.ContainsKey(dyingKey),
				"The sweep must drop the dying ALC's override key; otherwise it pins the unloaded context.");
			Assert.IsTrue(
				overrides.ContainsKey(siblingKey),
				"The sweep must keep a live sibling secondary ALC's override key — user configuration is never rebuilt, so dropping it would silently change the sibling app's default-style resolution.");
			Assert.IsTrue(
				overrides.ContainsKey(defaultAlcKey),
				"The sweep must keep default-ALC (framework/host) override keys.");
		}
		finally
		{
			overrides.Remove(defaultAlcKey);
			if (siblingKey is not null)
			{
				overrides.Remove(siblingKey);
			}

			dyingAlc.Unload();
			siblingAlc.Unload();
		}
	}

	[TestMethod]
	public void When_ClearCachesForNonDefaultAlc_Then_User_Overrides_Not_Touched()
	{
		// Guard: the rebuildable-cache sweep must NOT reach into the user-configuration dictionary.
		var overrides = FeatureConfiguration.Style.UseUWPDefaultStylesOverride;

		var collectibleAlc = new AssemblyLoadContext("Given_ResidualTypeStatics_Alc.cacheclear", isCollectible: true);
		Type? collectibleKey = null;
		try
		{
			collectibleKey = collectibleAlc
				.LoadFromAssemblyPath(typeof(Given_ResidualTypeStatics_Alc).Assembly.Location)
				.GetType(typeof(Given_ResidualTypeStatics_Alc).FullName!, throwOnError: true)!;

			overrides[collectibleKey] = false;

			Style.ClearCachesForNonDefaultAlc();

			Assert.IsTrue(
				overrides.ContainsKey(collectibleKey),
				"ClearCachesForNonDefaultAlc must not sweep user configuration: overrides are removed only by the ALC-scoped RemoveAlcScopedUserStyleOverrides.");
		}
		finally
		{
			if (collectibleKey is not null)
			{
				overrides.Remove(collectibleKey);
			}

			collectibleAlc.Unload();
		}
	}
}
