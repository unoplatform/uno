#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Globalization;
using _ResourceLoader = Windows.ApplicationModel.Resources.ResourceLoader;

namespace Uno.UI.Tests.ResourceLoaderTests;

/// <summary>
/// A downstream host that loads previewed apps into their own collectible
/// <see cref="AssemblyLoadContext"/>s registers each app assembly via
/// <see cref="_ResourceLoader.AddLookupAssembly"/>. The process-lifetime lookup-assembly list
/// then keeps a strong reference to every app assembly for the process lifetime, pinning the
/// context after unload. <see cref="_ResourceLoader.ClearNonDefaultAlcAssemblies"/> (invoked from
/// the ALC cleanup hook) removes those non-default-ALC assemblies while keeping default-ALC ones.
/// </summary>
[TestClass]
public class Given_ResourceLoader_Alc
{
	[TestMethod]
	public void When_ClearNonDefaultAlcAssemblies_Then_Collectible_Removed_And_Default_Kept()
	{
		// A default-ALC assembly (this test assembly) plays the framework/host role; it must survive.
		var defaultAlcAssembly = typeof(Given_ResourceLoader_Alc).Assembly;

		var collectibleAlc = new AssemblyLoadContext("Given_ResourceLoader_Alc.collectible", isCollectible: true);
		try
		{
			// Loading into a collectible ALC yields a distinct Assembly instance whose load context
			// is the collectible ALC — the previewed-app-assembly stand-in.
			var collectibleAssembly = collectibleAlc.LoadFromAssemblyPath(defaultAlcAssembly.Location);
			Assert.AreSame(collectibleAlc, AssemblyLoadContext.GetLoadContext(collectibleAssembly), "Pre-condition: the loaded assembly must belong to the collectible ALC.");

			_ResourceLoader.AddLookupAssembly(defaultAlcAssembly);
			_ResourceLoader.AddLookupAssembly(collectibleAssembly);

			Assert.IsTrue(_ResourceLoader.ContainsLookupAssembly(defaultAlcAssembly), "Pre-condition: the default-ALC assembly must be registered.");
			Assert.IsTrue(_ResourceLoader.ContainsLookupAssembly(collectibleAssembly), "Pre-condition: the collectible-ALC assembly must be registered.");

			_ResourceLoader.ClearNonDefaultAlcAssemblies();

			Assert.IsFalse(
				_ResourceLoader.ContainsLookupAssembly(collectibleAssembly),
				"The sweep must drop the collectible-ALC lookup assembly; otherwise the static list pins the unloaded context.");
			Assert.IsTrue(
				_ResourceLoader.ContainsLookupAssembly(defaultAlcAssembly),
				"The sweep must keep default-ALC (framework/host) lookup assemblies.");
		}
		finally
		{
			collectibleAlc.Unload();
		}
	}

	[TestMethod]
	public void When_ClearAlcAssemblies_Scoped_Then_Other_Alc_Kept()
	{
		// Two live secondary apps: tearing one down must not destroy the other's registrations.
		// Removal is destructive (a dropped registration is never re-added), so the ALC-scoped
		// sweep — used when the dying ALC is identifiable — must only remove the dying context's
		// lookup assemblies.
		var defaultAlcAssembly = typeof(Given_ResourceLoader_Alc).Assembly;

		var dyingAlc = new AssemblyLoadContext("Given_ResourceLoader_Alc.dying", isCollectible: true);
		var siblingAlc = new AssemblyLoadContext("Given_ResourceLoader_Alc.sibling", isCollectible: true);
		try
		{
			var dyingAssembly = dyingAlc.LoadFromAssemblyPath(defaultAlcAssembly.Location);
			var siblingAssembly = siblingAlc.LoadFromAssemblyPath(defaultAlcAssembly.Location);

			_ResourceLoader.AddLookupAssembly(dyingAssembly);
			_ResourceLoader.AddLookupAssembly(siblingAssembly);

			Assert.IsTrue(_ResourceLoader.ContainsLookupAssembly(dyingAssembly), "Pre-condition: the dying ALC's assembly must be registered.");
			Assert.IsTrue(_ResourceLoader.ContainsLookupAssembly(siblingAssembly), "Pre-condition: the sibling ALC's assembly must be registered.");

			_ResourceLoader.ClearAlcAssemblies(dyingAlc);

			Assert.IsFalse(
				_ResourceLoader.ContainsLookupAssembly(dyingAssembly),
				"The scoped sweep must drop the dying ALC's lookup assembly; otherwise the static list pins the unloaded context.");
			Assert.IsTrue(
				_ResourceLoader.ContainsLookupAssembly(siblingAssembly),
				"The scoped sweep must keep a live sibling secondary app's lookup assembly — dropping it would break the sibling's resource lookups for the rest of the process lifetime.");
		}
		finally
		{
			// Remove the sibling registration so this test does not leak state into other tests.
			_ResourceLoader.ClearNonDefaultAlcAssemblies();
			dyingAlc.Unload();
			siblingAlc.Unload();
		}
	}

	[TestMethod]
	public void When_ClearNonDefaultAlcAssemblies_Then_Default_Resources_Still_Resolve()
	{
		// Regression guard: the sweep removes only the dying assemblies' registrations and parsed
		// markers — it must NOT disturb the surviving (default-ALC) assemblies' loaded resources
		// (an earlier destroy-and-rebuild implementation could leave every loader permanently
		// empty when a rebuild step failed). A default-ALC resource must still resolve after the
		// sweep. Uses the .upri resources embedded in this (default-ALC) test assembly.
		const string defaultLanguage = "en";
		const string uiTestResources = "Uno.UI.UnitTests/Resources";

		var previousCulture = CultureInfo.CurrentUICulture;
		var previousPlo = ApplicationLanguages.PrimaryLanguageOverride;
		var previousDefault = _ResourceLoader.DefaultLanguage;

		var defaultAlcAssembly = typeof(Given_ResourceLoader_Alc).Assembly;
		var collectibleAlc = new AssemblyLoadContext("Given_ResourceLoader_Alc.resolve", isCollectible: true);
		try
		{
			CultureInfo.CurrentUICulture = new CultureInfo("en-US");
			ApplicationLanguages.PrimaryLanguageOverride = defaultLanguage;
			_ResourceLoader.DefaultLanguage = defaultLanguage;

			_ResourceLoader.AddLookupAssembly(defaultAlcAssembly);
			Assert.AreEqual(
				"App70-en",
				_ResourceLoader.GetForCurrentView(uiTestResources).GetString("ApplicationName"),
				"Pre-condition: the default-ALC resource resolves before the sweep.");

			var collectibleAssembly = collectibleAlc.LoadFromAssemblyPath(defaultAlcAssembly.Location);
			_ResourceLoader.AddLookupAssembly(collectibleAssembly);

			_ResourceLoader.ClearNonDefaultAlcAssemblies();

			Assert.AreEqual(
				"App70-en",
				_ResourceLoader.GetForCurrentView(uiTestResources).GetString("ApplicationName"),
				"The sweep must rebuild the loaders from the remaining default-ALC assemblies; otherwise cleared dictionaries stay empty.");
		}
		finally
		{
			collectibleAlc.Unload();
			CultureInfo.CurrentUICulture = previousCulture;
			ApplicationLanguages.PrimaryLanguageOverride = previousPlo;
			_ResourceLoader.DefaultLanguage = previousDefault;
		}
	}

	[TestMethod]
	public void When_ClearAlcAssemblies_Scoped_Then_Collectible_Override_Removed_And_Host_Value_Restored()
	{
		// Reviewer scenario: a collectible previewed app overrides the SAME loader/culture/key as the
		// host. While the collectible assembly is registered its value must win; after its ALC is torn
		// down the key must resolve back to the host value — the collectible override must NOT outlive
		// its ALC (an earlier implementation dropped only the lookup assembly + parsed markers, leaving
		// the overriding value merged into loader._resources forever).
		//
		// Both ALCs load the same physical test assembly, so their embedded .upri are byte-identical
		// and cannot naturally diverge. The differing collectible value is therefore injected via
		// reflection at the exact merge-dictionary seam (loader._resources[culture][key]) that a
		// collectible .upri carrying its own ApplicationName would have written (last writer wins).
		// Everything else — the removal and the rebuild-from-survivors — exercises production code.
		const string defaultLanguage = "en";
		const string uiTestResources = "Uno.UI.UnitTests/Resources";
		const string overrideValue = "Collectible-Override";
		const string hostValue = "App70-en";

		var previousCulture = CultureInfo.CurrentUICulture;
		var previousPlo = ApplicationLanguages.PrimaryLanguageOverride;
		var previousDefault = _ResourceLoader.DefaultLanguage;

		var defaultAlcAssembly = typeof(Given_ResourceLoader_Alc).Assembly;
		var collectibleAlc = new AssemblyLoadContext("Given_ResourceLoader_Alc.override", isCollectible: true);
		try
		{
			CultureInfo.CurrentUICulture = new CultureInfo("en-US");
			ApplicationLanguages.PrimaryLanguageOverride = defaultLanguage;
			_ResourceLoader.DefaultLanguage = defaultLanguage;

			// Host registration establishes the baseline value and the loader context.
			_ResourceLoader.AddLookupAssembly(defaultAlcAssembly);
			Assert.AreEqual(
				hostValue,
				_ResourceLoader.GetForCurrentView(uiTestResources).GetString("ApplicationName"),
				"Pre-condition: the host resource resolves before the collectible override.");

			// A collectible previewed app registers and overrides the same key with a different value.
			var collectibleAssembly = collectibleAlc.LoadFromAssemblyPath(defaultAlcAssembly.Location);
			_ResourceLoader.AddLookupAssembly(collectibleAssembly);
			OverrideMergedResource(uiTestResources, defaultLanguage, "ApplicationName", overrideValue);

			Assert.AreEqual(
				overrideValue,
				_ResourceLoader.GetForCurrentView(uiTestResources).GetString("ApplicationName"),
				"The collectible override must win while its assembly is registered.");

			// Tear the collectible app down via the ALC-scoped sweep.
			_ResourceLoader.ClearAlcAssemblies(collectibleAlc);

			Assert.IsFalse(
				_ResourceLoader.ContainsLookupAssembly(collectibleAssembly),
				"The scoped sweep must drop the collectible-ALC lookup assembly.");
			Assert.AreEqual(
				hostValue,
				_ResourceLoader.GetForCurrentView(uiTestResources).GetString("ApplicationName"),
				"After the collectible ALC is torn down, the key must resolve back to the host value — the override must not outlive its ALC.");
		}
		finally
		{
			// Drop any leftover non-default registration so this test does not leak state.
			_ResourceLoader.ClearNonDefaultAlcAssemblies();
			collectibleAlc.Unload();
			CultureInfo.CurrentUICulture = previousCulture;
			ApplicationLanguages.PrimaryLanguageOverride = previousPlo;
			_ResourceLoader.DefaultLanguage = previousDefault;
		}
	}

	/// <summary>
	/// Injects a merged resource value at the private per-loader dictionary seam
	/// (<c>ResourceLoader._resources[culture][key]</c>) — the same place
	/// <c>ProcessResourceFile</c> writes — to simulate a collectible .upri overriding an existing
	/// key with a different value. Needed because both ALCs load the same physical assembly, so a
	/// naturally-loaded collectible .upri cannot carry a divergent value.
	/// </summary>
	private static void OverrideMergedResource(string loaderName, string culture, string key, string value)
	{
		var loader = _ResourceLoader.GetForCurrentView(loaderName);
		var resourcesField = typeof(_ResourceLoader).GetField("_resources", BindingFlags.Instance | BindingFlags.NonPublic)
			?? throw new InvalidOperationException("ResourceLoader._resources field was not found.");
		var resources = (Dictionary<string, Dictionary<string, string>>)resourcesField.GetValue(loader)!;
		if (!resources.TryGetValue(culture, out var map))
		{
			resources[culture] = map = new Dictionary<string, string>();
		}

		map[key] = value;
	}
}
