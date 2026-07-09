#nullable enable

using System;
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
	public void When_ClearNonDefaultAlcAssemblies_Then_Default_Resources_Still_Resolve()
	{
		// Regression guard: the sweep clears every loader's dictionaries (ClearResources) and then
		// rebuilds from the remaining default-ALC assemblies. If the parsed-resource markers are not
		// also cleared, ProcessResourceFile skips the still-registered default-ALC files as "already
		// parsed" and the loaders stay empty — so a default-ALC resource must still resolve after the
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
}
