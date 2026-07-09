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
/// control types; those keys then pin the app's context after unload.
/// <see cref="Style.ClearCachesForNonDefaultAlc"/> (called from the ALC cleanup hook) drops the
/// non-default-ALC keys while keeping default-ALC ones.
/// </summary>
[TestClass]
public class Given_ResidualTypeStatics_Alc
{
	[TestMethod]
	public void When_ClearCachesForNonDefaultAlc_Then_UseUWPDefaultStylesOverride_Swept()
	{
		var overrides = FeatureConfiguration.Style.UseUWPDefaultStylesOverride;

		// A default-ALC key stands in for a framework/host control type; it must survive.
		var defaultAlcKey = typeof(Given_ResidualTypeStatics_Alc);

		var collectibleAlc = new AssemblyLoadContext("Given_ResidualTypeStatics_Alc.collectible", isCollectible: true);
		try
		{
			var collectibleKey = collectibleAlc
				.LoadFromAssemblyPath(defaultAlcKey.Assembly.Location)
				.GetType(defaultAlcKey.FullName!, throwOnError: true)!;

			overrides[defaultAlcKey] = false;
			overrides[collectibleKey] = false;

			Assert.IsTrue(overrides.ContainsKey(collectibleKey), "Pre-condition: the collectible-ALC key must be present.");

			Style.ClearCachesForNonDefaultAlc();

			Assert.IsFalse(
				overrides.ContainsKey(collectibleKey),
				"The sweep must drop the collectible-ALC override key; otherwise it pins the unloaded context.");
			Assert.IsTrue(
				overrides.ContainsKey(defaultAlcKey),
				"The sweep must keep default-ALC (framework/host) override keys.");
		}
		finally
		{
			overrides.Remove(defaultAlcKey);
			collectibleAlc.Unload();
		}
	}
}
