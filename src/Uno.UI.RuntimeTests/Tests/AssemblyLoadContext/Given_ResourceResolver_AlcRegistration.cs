#if HAS_UNO
#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.Xaml;
using Private.Infrastructure;

namespace Uno.UI.RuntimeTests.Tests.AssemblyLoadContext;

/// <summary>
/// Validates the ALC routing in <see cref="ResourceResolver.RegisterResourceDictionaryBySource(string, XamlParseContext, Func{ResourceDictionary}, string)"/>.
///
/// Before the fix this overload wrote unconditionally to the global <c>_registeredDictionariesByUri</c>
/// dictionary. When a library was loaded into multiple <see cref="AssemblyLoadContext"/>s
/// (e.g. a HotDesign client assembly loaded into both the host's default ALC and a
/// per-sample inner ALC), each ALC's SG-generated <c>GlobalStaticResources.RegisterResourceDictionariesBySource()</c>
/// would register the same assembly-prefixed URI (e.g.
/// <c>ms-appx:///Uno.UI.HotDesign.Client.Core/Styles/Flyout.xaml</c>) into the global
/// dict, and last-writer-wins. The host's <c>Application.Resources</c> merge then pulled
/// in the inner-ALC factory's Style / ControlTemplate. Tearing down the inner ALC
/// removed the type-keyed default-style entries those templates relied on, leaving
/// flyout content rendered at 0×0.
///
/// The fix detects the calling assembly's ALC via the delegate's
/// <c>Method.DeclaringType.Assembly</c> and routes non-default-ALC registrations into
/// <c>_registeredDictionariesByUriByAlc[alc]</c>, never overwriting the global dict
/// from a secondary ALC.
/// </summary>
[TestClass]
public class Given_ResourceResolver_AlcRegistration
{
	private TestAssemblyLoadContext? _testAlc;

	[TestCleanup]
	public void Cleanup()
	{
		_testAlc?.Unload();
		_testAlc = null;
	}

	/// <summary>
	/// Loading AlcApp into a secondary ALC runs its SG-generated
	/// <c>GlobalStaticResources.RegisterResourceDictionariesBySource()</c> in that ALC.
	/// Those registrations target the 3/4-arg overload of
	/// <see cref="ResourceResolver.RegisterResourceDictionaryBySource(string, XamlParseContext, Func{ResourceDictionary}, string)"/>
	/// with assembly-prefixed URIs. After the fix, they must land in the ALC-scoped
	/// registry — not the global one — so the host's same-URI lookups remain insulated.
	/// </summary>
	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaX11)]
	public async Task When_SecondaryAlcAppLoaded_Then_AssemblyPrefixedUrisInAlcScopedRegistry()
	{
		await LoadAlcAppIntoSecondaryAlcAsync();

		// AlcApp emits SG registrations for every XAML ResourceDictionary it owns. We
		// pick one assembly-prefixed URI that the 3/4-arg overload writes — the same
		// shape that was clobbering host entries in the original bug.
		const string AssemblyPrefixedUri = "ms-appx:///Uno.UI.RuntimeTests.AlcApp/AlcAppResources.xaml";

		var globalRegistry = GetGlobalRegistry();
		var alcRegistry = GetAlcRegistry(_testAlc!);

		Assert.IsFalse(
			globalRegistry.ContainsKey(AssemblyPrefixedUri),
			$"Secondary-ALC SG-emitted registrations must NOT be written to the global " +
			$"_registeredDictionariesByUri dict. Found '{AssemblyPrefixedUri}' there — the fix " +
			$"in ResourceResolver.RegisterResourceDictionaryBySource (4-arg overload) has regressed " +
			$"and inner-ALC factories will clobber host entries again, re-introducing the " +
			$"orphan-template flyout bug.");

		Assert.IsTrue(
			alcRegistry.ContainsKey(AssemblyPrefixedUri),
			$"Secondary-ALC SG-emitted registration for '{AssemblyPrefixedUri}' should have " +
			$"landed in _registeredDictionariesByUriByAlc[secondaryAlc]. The fix routes " +
			$"non-default-ALC delegates here via dictionary.Method.DeclaringType.Assembly's ALC.");
	}

	/// <summary>
	/// Direct unit-style verification: registering a delegate whose declaring type lives
	/// in a non-default ALC must land in that ALC's scoped registry, not the global one,
	/// even when the dictionary uses an assembly-prefixed URI (the path that the 4-arg
	/// overload services).
	/// </summary>
	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaX11)]
	public async Task When_DelegateFromSecondaryAlcRegistered_Then_RoutedToAlcScopedRegistry()
	{
		var secondaryAlcFactory = await CreateSecondaryAlcFactoryAsync();
		var declaringAlc = global::System.Runtime.Loader.AssemblyLoadContext.GetLoadContext(secondaryAlcFactory.Method.DeclaringType!.Assembly);
		Assert.AreSame(_testAlc, declaringAlc,
			"Test precondition: the delegate's declaring type must resolve to the test's secondary ALC.");

		// Use a guid-suffixed URI so the test is independent of any other state in the
		// global / ALC-scoped registries.
		var uri = "ms-appx:///AlcRoutingTest/" + Guid.NewGuid().ToString("N") + "/Test.xaml";

		ResourceResolver.RegisterResourceDictionaryBySource(
			uri,
			context: null,
			dictionary: secondaryAlcFactory,
			filePath: null);

		Assert.IsFalse(GetGlobalRegistry().ContainsKey(uri),
			"Secondary-ALC registration must not be written to the global dict.");

		Assert.IsTrue(GetAlcRegistry(_testAlc!).TryGetValue(uri, out var routedFactory),
			"Secondary-ALC registration must be written to the ALC-scoped registry for that ALC.");
		Assert.AreSame(secondaryAlcFactory, routedFactory,
			"The exact delegate registered should be retrievable from the ALC-scoped registry.");
	}

	/// <summary>
	/// The actual bug-fix scenario: when both the default ALC and a secondary ALC
	/// register a dictionary under the same URI (the exact collision that caused the
	/// HotDesign flyout regression), the default ALC's global-registry entry must be
	/// preserved. Before the fix, the secondary registration would silently overwrite
	/// the default-ALC entry in the global dict.
	/// </summary>
	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaX11)]
	public async Task When_DefaultAlcRegistersFirst_AndSecondaryAlcRegistersSame_Then_DefaultIsPreserved()
	{
		var secondaryAlcFactory = await CreateSecondaryAlcFactoryAsync();

		// Use a guid-suffixed URI so we don't fight any pre-existing real registration.
		var sharedUri = "ms-appx:///AlcRoutingTest/" + Guid.NewGuid().ToString("N") + "/Shared.xaml";

		// A default-ALC factory: lambdas declared inside this test method live in the
		// default ALC (the test assembly is loaded there). Use a sentinel ResourceDictionary
		// instance so we can verify the global dict still resolves to this exact factory.
		var defaultDictionary = new ResourceDictionary();
		Func<ResourceDictionary> defaultAlcFactory = () => defaultDictionary;

		Assert.AreSame(
			global::System.Runtime.Loader.AssemblyLoadContext.Default,
			global::System.Runtime.Loader.AssemblyLoadContext.GetLoadContext(defaultAlcFactory.Method.DeclaringType!.Assembly),
			"Test precondition: the default-ALC factory's declaring type must resolve to the default ALC.");

		// Default-ALC registration first.
		ResourceResolver.RegisterResourceDictionaryBySource(
			sharedUri,
			context: null,
			dictionary: defaultAlcFactory,
			filePath: null);

		Assert.IsTrue(GetGlobalRegistry().TryGetValue(sharedUri, out var initial),
			"Sanity: default-ALC registration should land in the global registry.");
		Assert.AreSame(defaultAlcFactory, initial,
			"Sanity: the default-ALC factory should be the one stored in the global registry.");

		// Secondary-ALC registration of the SAME URI — this is what would have clobbered
		// the default's entry before the fix.
		ResourceResolver.RegisterResourceDictionaryBySource(
			sharedUri,
			context: null,
			dictionary: secondaryAlcFactory,
			filePath: null);

		Assert.IsTrue(GetGlobalRegistry().TryGetValue(sharedUri, out var afterSecondary),
			"Default-ALC entry must remain in the global registry after a same-URI secondary-ALC registration.");
		Assert.AreSame(defaultAlcFactory, afterSecondary,
			"Secondary-ALC registration of the same URI must not overwrite the default-ALC entry " +
			"in _registeredDictionariesByUri. Overwriting causes the host's Application.Resources " +
			"merge to pull in the secondary ALC's Style/Template instances; tearing down that ALC " +
			"orphans the templates and breaks default-style lookup for the controls they emit.");

		Assert.IsTrue(GetAlcRegistry(_testAlc!).TryGetValue(sharedUri, out var alcEntry),
			"Secondary-ALC version of the URI should be present in the ALC-scoped registry.");
		Assert.AreSame(secondaryAlcFactory, alcEntry,
			"The secondary-ALC delegate should be stored under the secondary ALC's scoped registry.");
	}

	private async Task<Func<ResourceDictionary>> CreateSecondaryAlcFactoryAsync()
	{
		await LoadAlcAppIntoSecondaryAlcAsync();

		var alcAppAssembly = _testAlc!.Assemblies
			.First(a => a.GetName().Name == "Uno.UI.RuntimeTests.AlcApp");

		var factoryType = alcAppAssembly.GetType("AlcTestApp.ResourceDictionaryTestFactory");
		Assert.IsNotNull(factoryType,
			"AlcTestApp.ResourceDictionaryTestFactory must be discoverable in the secondary ALC.");

		var factoryMethod = factoryType!.GetMethod(
			"Create",
			BindingFlags.Public | BindingFlags.Static);
		Assert.IsNotNull(factoryMethod,
			"AlcTestApp.ResourceDictionaryTestFactory.Create must exist as a public static method.");

		// The created delegate's Method.DeclaringType.Assembly is ResourceDictionaryTestFactory's
		// assembly, which has been loaded into _testAlc — exactly the input shape that
		// AlcApp's SG-generated GlobalStaticResources.RegisterResourceDictionariesBySource()
		// passes to ResourceResolver.RegisterResourceDictionaryBySource.
		return (Func<ResourceDictionary>)Delegate.CreateDelegate(
			typeof(Func<ResourceDictionary>),
			factoryMethod!);
	}

	private async Task LoadAlcAppIntoSecondaryAlcAsync()
	{
		if (_testAlc is not null)
		{
			return;
		}

		var alcAppPath = await BuildAlcAppAsync();
		Assert.IsNotNull(alcAppPath, "AlcApp build must succeed.");
		Assert.IsTrue(File.Exists(alcAppPath!), $"AlcApp assembly must exist at {alcAppPath}.");

		var alcAppDirectory = Path.GetDirectoryName(alcAppPath)!;
		_testAlc = new TestAssemblyLoadContext(alcAppDirectory);

		var alcAppAssembly = _testAlc.LoadFromAssemblyPath(alcAppPath!);
		Assert.IsNotNull(alcAppAssembly, "AlcApp assembly must load into the secondary ALC.");

		// Drive the SG-emitted RegisterResourceDictionariesBySource() directly — that's
		// the code path under test. Doing this rather than constructing the App avoids
		// dragging the full application bootstrap (windows, dispatcher, etc.) into a
		// test that only needs to observe the registry side-effects.
		var globalStaticResourcesType = alcAppAssembly.GetType("AlcTestApp.GlobalStaticResources");
		Assert.IsNotNull(globalStaticResourcesType,
			"AlcApp's SG-generated GlobalStaticResources type must be present.");

		var registerMethod = globalStaticResourcesType!.GetMethod(
			"RegisterResourceDictionariesBySource",
			BindingFlags.Public | BindingFlags.Static);
		Assert.IsNotNull(registerMethod,
			"AlcApp's GlobalStaticResources.RegisterResourceDictionariesBySource() must exist as a public static method.");

		registerMethod!.Invoke(null, null);

		await Task.CompletedTask;
	}

	private async Task<string?> BuildAlcAppAsync()
	{
		// AlcApp is normally built ad-hoc by Given_AlcContentHost via dotnet build;
		// in tests that only need the assembly loaded (no full app startup), we accept
		// the pre-built copy from a prior test run rather than driving the SDK build
		// twice. Locate it via the same search heuristic as Given_AlcContentHost.
		var basePath = Path.GetDirectoryName(Application.Current.GetType().Assembly.Location)!;
		var searchPaths = new[]
		{
			Path.Combine(basePath, "..", "..", "..", "..", "..", "Uno.UI.RuntimeTests", "Tests", "AssemblyLoadContext", "AlcApp"),
			Path.Combine(basePath, "..", "..", "src", "Uno.UI.RuntimeTests", "Tests", "AssemblyLoadContext", "AlcApp"),
			Path.Combine(basePath, "..", "..", ".."),
		};

		var projectFolder = searchPaths
			.FirstOrDefault(p => File.Exists(Path.Combine(p, "Uno.UI.RuntimeTests.AlcApp.csproj")));
		Assert.IsNotNull(projectFolder,
			$"Could not locate AlcApp project folder in any of: {string.Join(", ", searchPaths)}");

		// Resolve the most recent pre-built assembly under any TFM/Configuration.
		// We can't rely on a hard-coded path because the SDK and configuration vary
		// across runs.
		var binFolder = Path.Combine(projectFolder!, "bin");
		if (!Directory.Exists(binFolder))
		{
			// Fall back to triggering a build inline via Given_AlcContentHost's existing
			// helper would be ideal, but to avoid duplication we just fail with a clear
			// message. In CI / local test runs, AlcApp is built as part of the suite.
			Assert.Fail(
				$"AlcApp has not been built (no '{binFolder}' folder). Run any test in " +
				$"Given_AlcContentHost first, or rebuild the runtime tests solution, so that " +
				$"the AlcApp assembly is available for this test.");
		}

		var candidate = Directory.EnumerateFiles(binFolder, "Uno.UI.RuntimeTests.AlcApp.dll", SearchOption.AllDirectories)
			.OrderByDescending(File.GetLastWriteTimeUtc)
			.FirstOrDefault();

		Assert.IsNotNull(candidate,
			$"No Uno.UI.RuntimeTests.AlcApp.dll found under {binFolder}. Build the AlcApp project first.");

		// Async signature kept for parity with Given_AlcContentHost.BuildAlcAppAsync so
		// follow-ups that need to drive an SDK build can drop in without callers changing.
		await Task.CompletedTask;

		return candidate;
	}

	private static Dictionary<string, Func<ResourceDictionary>> GetGlobalRegistry()
	{
		var field = typeof(ResourceResolver).GetField(
			"_registeredDictionariesByUri",
			BindingFlags.NonPublic | BindingFlags.Static);
		Assert.IsNotNull(field, "ResourceResolver._registeredDictionariesByUri must be present.");
		return (Dictionary<string, Func<ResourceDictionary>>)field!.GetValue(null)!;
	}

	private static Dictionary<string, Func<ResourceDictionary>> GetAlcRegistry(global::System.Runtime.Loader.AssemblyLoadContext alc)
	{
		var field = typeof(ResourceResolver).GetField(
			"_registeredDictionariesByUriByAlc",
			BindingFlags.NonPublic | BindingFlags.Static);
		Assert.IsNotNull(field, "ResourceResolver._registeredDictionariesByUriByAlc must be present.");

		var alcDict = (Dictionary<global::System.Runtime.Loader.AssemblyLoadContext, Dictionary<string, Func<ResourceDictionary>>>)field!.GetValue(null)!;
		Assert.IsTrue(alcDict.TryGetValue(alc, out var registry),
			"The provided ALC has no entry in _registeredDictionariesByUriByAlc. " +
			"This usually means no SG-emitted code ran in that ALC, or all its registrations " +
			"were incorrectly routed to the global dict (the regression this test guards against).");
		return registry!;
	}
}

#endif
