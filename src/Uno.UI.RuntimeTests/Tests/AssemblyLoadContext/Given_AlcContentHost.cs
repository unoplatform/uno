#if HAS_UNO
#nullable enable

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Uno.UI.DataBinding;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.Xaml;
using Uno.UI.Xaml.Controls;
using Private.Infrastructure;
using Uno.Foundation.Logging;

namespace Uno.UI.RuntimeTests.Tests.AssemblyLoadContext;

[TestClass]
[RunsOnUIThread]
[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Types manipulated here have been marked earlier")]
[UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "Types manipulated here have been marked earlier")]
[UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "Types manipulated here have been marked earlier")]
[UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Types manipulated here have been marked earlier")]
public class Given_AlcContentHost
{
	private TestAssemblyLoadContext? _testAlc;
	private TestAssemblyLoadContext? _testAlcSecondary;
	private AlcContentHost? _contentHost;

	// The host's bindable-metadata provider, captured before a secondary ALC app overwrites the
	// process-wide BindableMetadata.Provider with its own (ALC-scoped) provider.
	private IBindableMetadataProvider? _originalBindableMetadataProvider;

	[TestInitialize]
	public void Setup()
	{
		_originalBindableMetadataProvider = BindableMetadata.Provider;
	}

	[TestCleanup]
	public void Cleanup()
	{
		// Clean up the static content host override
		WindowHelper.ContentHostOverride = null;

		_contentHost = null;

		if (_testAlc is not null)
		{
			_testAlc.Unload();
			_testAlc = null;
		}

		if (_testAlcSecondary is not null)
		{
			_testAlcSecondary.Unload();
			_testAlcSecondary = null;
		}

		// A secondary ALC app overwrites the process-wide BindableMetadata.Provider with its own
		// (ALC-scoped) provider, and ALC teardown then nulls it because it belongs to a non-default ALC.
		// Nothing restores the host's provider, so every later test's binding/metadata resolution NREs
		// (or silently falls back to reflection). Put the host's provider back.
		BindableMetadata.Provider = _originalBindableMetadataProvider;

		// Stop later (non-ALC) tests from consulting this run's now-stale secondary apps: the flag is a
		// one-way latch that gates all secondary-app resource fallback. An ALC test that needs it simply
		// re-sets it via StartSecondaryAlcAppAsync. We deliberately do NOT clear _applicationsByAlc here:
		// it is a ConditionalWeakTable that self-heals on GC, and sibling ALC tests resolve against those
		// still-registered apps through the non-deterministic AssemblyName->ALC lookup — dropping them
		// eagerly makes those lookups miss and fall back to the host.
		Application.HasSecondaryApps = false;
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaX11)] // Disabled on macOS for unknown core dump
	public async Task When_AlcContentHost_Then_ResourcesInherited()
	{
		var contentHost = await StartSecondaryAlcAppAsync();

		Assert.IsTrue(contentHost.Resources.ContainsKey("TestAccentBrush"), "ContentHost should project resources from the secondary Application");
		var accentBrush = contentHost.Resources["TestAccentBrush"] as SolidColorBrush;
		Assert.IsNotNull(accentBrush, "TestAccentBrush should be a SolidColorBrush");

		var root = contentHost.Content as FrameworkElement;
		Assert.IsNotNull(root, "Secondary content should be a FrameworkElement");
		var titleTextBlock = root.FindName("TitleTextBlock") as TextBlock;
		Assert.IsNotNull(titleTextBlock, "TitleTextBlock should be discoverable via FindName");

		Assert.AreSame(accentBrush, titleTextBlock!.Foreground, "Sub-app visuals should consume brushes from the projected resources");
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaX11)] // Disabled on macOS for unknown core dump
	public async Task When_AlcContentHost_Then_MergedDictionariesInherited()
	{
		var contentHost = await StartSecondaryAlcAppAsync();
		Assert.IsTrue(contentHost.Resources.MergedDictionaries.Count > 0, "ContentHost should surface merged dictionaries from the secondary Application");

		var mergedDictionary = contentHost.Resources.MergedDictionaries
			.FirstOrDefault(dict => dict.ContainsKey("TestTextBlockStyle"));
		Assert.IsNotNull(mergedDictionary, "Merged dictionaries should contain TestTextBlockStyle");

		var projectedStyle = mergedDictionary!["TestTextBlockStyle"] as Style;
		Assert.IsNotNull(projectedStyle, "TestTextBlockStyle should be available as a Style");

		var root = contentHost.Content as FrameworkElement;
		Assert.IsNotNull(root, "Secondary content should be a FrameworkElement");
		var statusTextBlock = root.FindName("StatusTextBlock") as TextBlock;
		Assert.IsNotNull(statusTextBlock, "StatusTextBlock should be discoverable via FindName");

		Assert.AreSame(projectedStyle, statusTextBlock!.Style, "Merged dictionary styles should apply to secondary visuals");
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaX11)] // Disabled on macOS for unknown core dump
	public async Task When_SecondaryAlcApp_Then_ContentHosted()
	{
		var contentHost = await StartSecondaryAlcAppAsync();

		Assert.IsNotNull(contentHost.Content, "ContentHost.Content should be set by the secondary app");
		Assert.IsTrue(contentHost.Resources.ContainsKey("TestAccentBrush"),
			"TestAccentBrush should be available from secondary app resources");

		var testBrush = contentHost.Resources["TestAccentBrush"] as SolidColorBrush;
		Assert.IsNotNull(testBrush, "TestAccentBrush should be a SolidColorBrush");
		Assert.AreEqual(Windows.UI.Color.FromArgb(0xFF, 0x6B, 0x4C, 0xE0), testBrush!.Color,
			"TestAccentBrush should have the expected color");
	}

	/// <summary>
	/// Validates that ResourceDictionary.Source in App.xaml correctly resolves to the ALC-specific
	/// dictionary when loaded from a secondary AssemblyLoadContext. This tests the fix for the issue
	/// where both primary and secondary ALCs with the same resource URI pattern (e.g.,
	/// &lt;ResourceDictionary Source="AppResources.xaml"/&gt;) would resolve to the primary ALC's dictionary.
	/// </summary>
	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaX11)]
	public async Task When_AlcAppXaml_HasSourceDictionary_Then_ResolvesToAlcSpecificDictionary()
	{
		var contentHost = await StartSecondaryAlcAppAsync();

		// Verify the AlcApp-specific identifier resource is accessible
		// This resource is defined in AlcApp/AppResources.xaml which is included via
		// <ResourceDictionary Source="ms-appx:///AppResources.xaml" /> in AlcApp/App.xaml
		var mergedDictionary = contentHost.Resources.MergedDictionaries
			.FirstOrDefault(dict => dict.ContainsKey("AlcAppIdentifier"));
		Assert.IsNotNull(mergedDictionary,
			"AlcAppIdentifier should be found in merged dictionaries loaded via Source attribute");

		var identifier = mergedDictionary!["AlcAppIdentifier"] as string;
		Assert.AreEqual("Uno.UI.RuntimeTests.AlcApp", identifier,
			"AlcAppIdentifier should have the expected value from the AlcApp's AppResources.xaml");

		// Verify the AlcApp-specific brush is accessible and has the correct color
		Assert.IsTrue(mergedDictionary.ContainsKey("AlcAppSpecificBrush"),
			"AlcAppSpecificBrush should be found in merged dictionaries loaded via Source attribute");

		var alcBrush = mergedDictionary["AlcAppSpecificBrush"] as SolidColorBrush;
		Assert.IsNotNull(alcBrush, "AlcAppSpecificBrush should be a SolidColorBrush");
		Assert.AreEqual(Windows.UI.Color.FromArgb(0xFF, 0x00, 0xAA, 0x55), alcBrush!.Color,
			"AlcAppSpecificBrush should have the color defined in AlcApp's AppResources.xaml (#FF00AA55)");
	}

	/// <summary>
	/// Validates that a ResourceDictionary.Source referencing a file in a subdirectory
	/// (e.g., "ms-appx:///Styles/CustomColors.xaml") correctly resolves when loaded from a
	/// secondary AssemblyLoadContext. This tests the ambient ALC context fix where the
	/// fallback path in RetrieveDictionaryForSource(string, string) now checks the
	/// ALC-scoped registry via ResourceResolver.CurrentResolutionAlc.
	/// </summary>
	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaX11)]
	public async Task When_AlcAppXaml_HasSubdirectorySourceDictionary_Then_ResolvesCorrectly()
	{
		var contentHost = await StartSecondaryAlcAppAsync();

		// Find the merged dictionary that contains the subdirectory resource identifier.
		// This resource is defined in AlcApp/Styles/CustomColors.xaml which is included via
		// <ResourceDictionary Source="ms-appx:///Styles/CustomColors.xaml" /> in AlcApp/App.xaml
		var mergedDictionary = contentHost.Resources.MergedDictionaries
			.FirstOrDefault(dict => dict.ContainsKey("SubdirResourceIdentifier"));
		Assert.IsNotNull(mergedDictionary,
			"SubdirResourceIdentifier should be found in merged dictionaries loaded via subdirectory Source attribute");

		var identifier = mergedDictionary!["SubdirResourceIdentifier"] as string;
		Assert.AreEqual("AlcApp.Styles.CustomColors", identifier,
			"SubdirResourceIdentifier should have the expected value from the AlcApp's Styles/CustomColors.xaml");

		// Verify the subdirectory brush is accessible and has the correct color
		Assert.IsTrue(mergedDictionary.ContainsKey("SubdirCustomBrush"),
			"SubdirCustomBrush should be found in merged dictionaries loaded via subdirectory Source attribute");

		var subdirBrush = mergedDictionary["SubdirCustomBrush"] as SolidColorBrush;
		Assert.IsNotNull(subdirBrush, "SubdirCustomBrush should be a SolidColorBrush");
		Assert.AreEqual(Windows.UI.Color.FromArgb(0xFF, 0x33, 0x99, 0xFF), subdirBrush!.Color,
			"SubdirCustomBrush should have the color defined in AlcApp's Styles/CustomColors.xaml (#FF3399FF)");
	}

	/// <summary>
	/// Regression test for theme switching: when a brush living in a default-ALC (shared) assembly references
	/// a color via {StaticResource X} and X is defined ONLY in a secondary ALC's
	/// Application.Resources, ResourceResolver.TryTopLevelRetrieval used to fall back to
	/// Application.Current (the host) and miss the resource — causing the brush to be
	/// materialized with a transparent default Color. This fixture asserts that the
	/// secondary-ALC fallback added to TryTopLevelRetrieval finds the resource.
	/// </summary>
	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaX11)]
	public async Task When_TopLevelLookupFromHostContext_Then_FallsBackToSecondaryAlcApp()
	{
		await StartSecondaryAlcAppAsync();

		// Sanity: AlcAppOnlyColor is intentionally absent from the host Application.Resources
		Assert.IsFalse(
			Application.Current.Resources.ContainsKey("AlcAppOnlyColor"),
			"Sanity check: AlcAppOnlyColor must not exist in Application.Current.Resources");

		// Build a parse context whose AssemblyName resolves to the test assembly (default ALC),
		// matching the situation a brush in Uno.Themes.WinUI sees: parseContext.AssemblyLoadContext
		// resolves to the default ALC, not the secondary one that holds the color.
		var parseContext = new XamlParseContext
		{
			AssemblyName = typeof(Given_AlcContentHost).Assembly.GetName().Name
		};

		var key = new SpecializedResourceDictionary.ResourceKey("AlcAppOnlyColor");
		var found = ResourceResolver.TryTopLevelRetrieval(key, parseContext, out var value);

		Assert.IsTrue(found,
			"TryTopLevelRetrieval must fall back to secondary-ALC Application.Resources " +
			"when the resource is missing from the host app.");
		Assert.IsInstanceOfType<Windows.UI.Color>(value);
		Assert.AreEqual(
			Windows.UI.Color.FromArgb(0xFF, 0x11, 0x22, 0x33),
			(Windows.UI.Color)value,
			"Returned color must match AlcAppOnlyColor (#FF112233) defined in the secondary ALC.");
	}

	/// <summary>
	/// End-to-end regression test:
	/// SolidColorBrush whose Color is bound via {StaticResource} to a Color defined in
	/// the secondary ALC must materialize with the correct Color rather than the default
	/// transparent value. Without the fix the lazy materialization returns a brush with
	/// Color = #00000000.
	/// </summary>
	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaX11)]
	public async Task When_LazyBrushReferencesSecondaryAlcColor_Then_MaterializesWithCorrectColor()
	{
		var contentHost = await StartSecondaryAlcAppAsync();

		var mergedDictionary = contentHost.Resources.MergedDictionaries
			.FirstOrDefault(dict => dict.ContainsKey("AlcAppLazyBrush"));
		Assert.IsNotNull(mergedDictionary,
			"AlcAppLazyBrush should be discoverable through the projected merged dictionaries.");

		var lazyBrush = mergedDictionary!["AlcAppLazyBrush"] as SolidColorBrush;
		Assert.IsNotNull(lazyBrush,
			"AlcAppLazyBrush should materialize to a SolidColorBrush.");
		Assert.AreEqual(
			Windows.UI.Color.FromArgb(0xFF, 0x11, 0x22, 0x33),
			lazyBrush!.Color,
			"AlcAppLazyBrush.Color must be resolved from AlcAppOnlyColor (#FF112233). " +
			"A transparent (#00000000) value indicates the secondary-ALC fallback did not run.");
	}

	/// <summary>
	/// Regression test for the secondary-ALC color bleed into the host: when both the host
	/// (Application.Current) and a secondary-ALC app define the same resource key, a lookup
	/// originating from a host / default-ALC parse context MUST return the host's value.
	/// The earlier "Prioritize secondary-ALC apps over host" behavior iterated all secondary
	/// apps before the host even for default-ALC contexts
	/// </summary>
	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaX11)]
	public async Task When_TopLevelLookupFromHostContext_HasHostValue_Then_HostWinsOverSecondaryAlcApp()
	{
		await StartSecondaryAlcAppAsync();

		// AlcAppOnlyColor is defined in the secondary ALC's AlcAppResources.xaml as #FF112233.
		// Register a *different* value for the same key in the host so we can distinguish
		// whether the lookup returned the host's value or bled in the secondary's.
		var hostColor = Windows.UI.Color.FromArgb(0xFF, 0xAA, 0xBB, 0xCC);
		const string sharedKey = "AlcAppOnlyColor";

		var hadPrior = Application.Current.Resources.TryGetValue(sharedKey, out var priorValue);
		Application.Current.Resources[sharedKey] = hostColor;
		try
		{
			// Default-ALC parse context — what host XAML emits when its own {ThemeResource} /
			// {StaticResource} bindings fire.
			var parseContext = new XamlParseContext
			{
				AssemblyName = typeof(Given_AlcContentHost).Assembly.GetName().Name
			};

			var key = new SpecializedResourceDictionary.ResourceKey(sharedKey);
			var found = ResourceResolver.TryTopLevelRetrieval(key, parseContext, out var value);

			Assert.IsTrue(found, "Lookup should succeed (host has the key).");
			Assert.IsInstanceOfType<Windows.UI.Color>(value);
			Assert.AreEqual(
				hostColor,
				(Windows.UI.Color)value,
				"Host UI lookups must NOT pick up a same-key value from a secondary-ALC app. " +
				"Returning the secondary's #FF112233 here means secondary-app theme colors are " +
				"bleeding into the host's chrome.");
		}
		finally
		{
			if (hadPrior)
			{
				Application.Current.Resources[sharedKey] = priorValue;
			}
			else
			{
				Application.Current.Resources.Remove(sharedKey);
			}
		}
	}

	/// <summary>
	/// Counterpart to <c>When_TopLevelLookupFromHostContext_HasHostValue_Then_HostWinsOverSecondaryAlcApp</c>:
	/// when a secondary-ALC parse context drives the lookup and BOTH the host and the secondary
	/// app define the same key, the secondary app's value must win (priority step 1 in
	/// <c>ResourceResolver.TryTopLevelRetrieval</c> — <c>contextApp.Resources</c> is queried
	/// before <c>Application.Current</c>). This is what allows a secondary app's theme overrides
	/// (e.g. a brand <c>ColorOverrideDictionary</c>) to take precedence over the host's same-key
	/// defaults for XAML owned by that secondary app.
	/// </summary>
	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaX11)]
	public async Task When_TopLevelLookupFromSecondaryAlcContext_HasHostValue_Then_SecondaryWins()
	{
		await StartSecondaryAlcAppAsync();

		// AlcAppOnlyColor in the secondary ALC's AlcAppResources.xaml is #FF112233.
		// Define a *different* value for the same key in the host.
		var hostColor = Windows.UI.Color.FromArgb(0xFF, 0xAA, 0xBB, 0xCC);
		const string sharedKey = "AlcAppOnlyColor";

		var hadPrior = Application.Current.Resources.TryGetValue(sharedKey, out var priorValue);
		Application.Current.Resources[sharedKey] = hostColor;
		try
		{
			// Parse context owned by this test's secondary ALC. Bind the ALC explicitly rather than
			// by AssemblyName: the lazy AssemblyName->ALC resolver picks the first loaded assembly with
			// a matching name, which is non-deterministic once earlier tests have loaded AlcApp into
			// other (not-yet-collected) ALCs — it can resolve to a stale ALC whose app is gone and fall
			// back to the host. (Same explicit-binding fix the sibling-iteration test already uses.)
			var parseContext = new XamlParseContext
			{
				AssemblyLoadContext = _testAlc
			};

			var key = new SpecializedResourceDictionary.ResourceKey(sharedKey);
			var found = ResourceResolver.TryTopLevelRetrieval(key, parseContext, out var value);

			Assert.IsTrue(found, "Lookup should succeed (both host and secondary have the key).");
			Assert.IsInstanceOfType<Windows.UI.Color>(value);
			Assert.AreEqual(
				Windows.UI.Color.FromArgb(0xFF, 0x11, 0x22, 0x33),
				(Windows.UI.Color)value,
				"Secondary-ALC parse context must return the secondary app's value, not the host's. " +
				"Returning the host's #FFAABBCC here means contextApp.Resources is not being queried " +
				"before Application.Current — secondary-app theme overrides would be silently lost.");
		}
		finally
		{
			if (hadPrior)
			{
				Application.Current.Resources[sharedKey] = priorValue;
			}
			else
			{
				Application.Current.Resources.Remove(sharedKey);
			}
		}
	}

	/// <summary>
	/// Sibling-iteration regression test (priority step 2):
	/// when a lookup originates from secondary app A's parse context and the key is NOT in
	/// A's resources, <c>ResourceResolver.TryTopLevelRetrieval</c> must walk the other
	/// registered secondary apps before falling back to the host. Without this branch, a
	/// shared brush in app A that depends on a color defined only in a sibling secondary
	/// app B would be force-resolved against the host (and likely miss).
	/// </summary>
	/// <remarks>
	/// Loads the AlcApp DLL into a separate collectible ALC and constructs <c>AlcTestApp.AppB</c>
	/// inside it. <c>AppB</c> has a distinct <c>Type.FullName</c> from <c>AlcTestApp.App</c>, so
	/// the same-type dedupe in <c>EnumerateSecondaryApplications</c> does not collapse the two
	/// registrations.
	/// </remarks>
	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaX11)]
	public async Task When_TopLevelLookupFromSecondaryAlcContext_Then_FallsBackToSiblingSecondaryApp()
	{
		await StartSecondaryAlcAppAsync();

		// Load AlcApp into a second ALC and construct AppB there. AppB is a minimal
		// Application subclass (no XAML/InitializeComponent) — base ctor's Current = this
		// registers it under the new ALC.
		var siblingApp = await RegisterSiblingAlcApplicationAsync("AlcTestApp.AppB");

		// Seed a key only AppB knows about. AppB starts with empty Resources (no XAML).
		const string siblingOnlyKey = "AlcAppSiblingOnlyColor";
		var siblingColor = Windows.UI.Color.FromArgb(0xFF, 0x44, 0xCC, 0x88);
		siblingApp.Resources[siblingOnlyKey] = siblingColor;

		Assert.IsFalse(
			Application.Current.Resources.ContainsKey(siblingOnlyKey),
			"Sanity: the sibling-only key must not be in the host.");

		var primaryAlcApp = Application.GetForAssemblyLoadContext(_testAlc!);
		Assert.IsNotNull(primaryAlcApp, "Primary secondary-ALC app should be registered.");
		Assert.IsFalse(
			primaryAlcApp!.Resources.ContainsKey(siblingOnlyKey),
			"Sanity: the sibling-only key must not be in the primary secondary app either.");

		// Parse context owned by the primary secondary ALC: contextApp resolves to it,
		// misses the key, then EnumerateSecondaryApplications yields AppB → hit.
		// Note: bind the ALC explicitly. The lazy resolver picks the first assembly with a
		// matching AssemblyName, which is non-deterministic when both ALCs have AlcApp loaded.
		var parseContext = new XamlParseContext
		{
			AssemblyLoadContext = _testAlc
		};

		var key = new SpecializedResourceDictionary.ResourceKey(siblingOnlyKey);
		var found = ResourceResolver.TryTopLevelRetrieval(key, parseContext, out var value);

		Assert.IsTrue(found,
			"Sibling iteration must locate keys defined only in another registered secondary app.");
		Assert.AreEqual(
			siblingColor,
			(Windows.UI.Color)value,
			"Returned color must come from the sibling secondary app (AppB).");
	}

	/// <summary>
	/// Hot-reload bump regression test:
	/// when a parse context references a STALE (hot-reloaded-out) secondary ALC,
	/// <c>ResourceResolver.TryTopLevelRetrieval</c> must redirect the lookup to the latest
	/// registered <c>Application</c> with the same <c>Type.FullName</c>
	/// (via <c>Application.GetLatestSecondaryApplicationForType</c>). Without the bump the
	/// lookup would read stale resources from the previous build's <c>Application</c>.
	/// </summary>
	/// <remarks>
	/// Loads the AlcApp DLL into a second collectible ALC and constructs another
	/// <c>AlcTestApp.App</c> instance there. Both registrations share <c>Type.FullName</c>,
	/// so the dedupe in <c>EnumerateSecondaryApplications</c> retains only the newest; the
	/// bump operates on the (un-deduped) raw <c>_applicationsByAlc</c> registry inside
	/// <c>GetLatestSecondaryApplicationForType</c>.
	/// </remarks>
	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaX11)]
	public async Task When_StaleAlcParseContext_Then_BumpsToLiveSecondaryApp()
	{
		await StartSecondaryAlcAppAsync();

		var staleApp = Application.GetForAssemblyLoadContext(_testAlc!);
		Assert.IsNotNull(staleApp, "Primary (stale) secondary-ALC app should be registered.");

		// Mutate the stale app's resource so the bump's effect is observable.
		const string sharedKey = "AlcAppOnlyColor";
		var staleColor = Windows.UI.Color.FromArgb(0xFF, 0xDE, 0xAD, 0x00);
		staleApp!.Resources[sharedKey] = staleColor;

		// Load AlcApp DLL into a second collectible ALC and construct another App instance.
		// Its AlcRegistrationId will be higher than the stale app's, so it is "live".
		var liveApp = await RegisterSiblingAlcApplicationAsync("AlcTestApp.App");

		var liveColor = Windows.UI.Color.FromArgb(0xFF, 0xBE, 0xEF, 0x00);
		liveApp.Resources[sharedKey] = liveColor;

		Assert.AreNotSame(staleApp, liveApp,
			"Sanity: the two ALC loads must produce distinct Application instances.");

		// Parse context points at the STALE ALC — what a XAML-generated __ParseContext_
		// from the previous build would carry after a hot reload.
		var parseContext = new XamlParseContext
		{
			AssemblyLoadContext = _testAlc
		};

		var key = new SpecializedResourceDictionary.ResourceKey(sharedKey);
		var found = ResourceResolver.TryTopLevelRetrieval(key, parseContext, out var value);

		Assert.IsTrue(found, "Lookup should succeed against the live app's resources.");
		Assert.AreEqual(
			liveColor,
			(Windows.UI.Color)value,
			"Bump must redirect the lookup from the stale Application instance to the most " +
			"recently registered one. Returning the stale color means " +
			"GetLatestSecondaryApplicationForType was not consulted.");
	}

	/// <summary>
	/// Loads the AlcApp DLL into a fresh collectible ALC and constructs the specified
	/// <see cref="Application"/> subclass there via reflection. The base
	/// <see cref="Application"/> constructor's <c>Current = this</c> assignment registers
	/// the instance in <c>_applicationsByAlc</c> under the new ALC, giving it a higher
	/// <c>AlcRegistrationId</c> than the primary secondary app.
	/// </summary>
	private async Task<Application> RegisterSiblingAlcApplicationAsync(string appTypeFullName)
	{
		var alcAppPath = await BuildAlcAppAsync();
		Assert.IsNotNull(alcAppPath, "AlcApp build should succeed for sibling ALC.");

		var alcAppDirectory = Path.GetDirectoryName(alcAppPath)!;
		_testAlcSecondary = new TestAssemblyLoadContext(alcAppDirectory);
		var alcAppAssembly = _testAlcSecondary.LoadFromAssemblyPath(alcAppPath);
		Assert.IsNotNull(alcAppAssembly, "AlcApp assembly should be loaded into sibling ALC.");

		var appType = alcAppAssembly.GetType(appTypeFullName);
		Assert.IsNotNull(appType, $"{appTypeFullName} should be discoverable in the sibling ALC.");

		Application? siblingApp = null;
		Exception? constructionError = null;

		// Construct on a dedicated thread so AssemblyLoadContext.GetLoadContext picks up
		// the sibling ALC during the Application's base constructor; avoid colliding with
		// the primary AlcApp's dispatcher / window initialization on the UI thread.
		var thread = new System.Threading.Thread(() =>
		{
			try
			{
				siblingApp = (Application?)Activator.CreateInstance(appType!);
			}
			catch (Exception ex)
			{
				constructionError = ex;
			}
		})
		{
			IsBackground = true,
			Name = $"AlcApp-Sibling-{appTypeFullName}"
		};
		thread.Start();
		thread.Join();

		if (constructionError is not null)
		{
			throw new InvalidOperationException(
				$"Failed to construct {appTypeFullName} in sibling ALC: {constructionError.Message}",
				constructionError);
		}

		Assert.IsNotNull(siblingApp, $"{appTypeFullName} instance should be constructed.");

		// Confirm it landed in the sibling ALC (sanity-check the ALC affinity).
		// Fully qualify: the test file's namespace ends in 'AssemblyLoadContext', so bare
		// AssemblyLoadContext resolves to the namespace, not the type.
		var resolvedAlc = System.Runtime.Loader.AssemblyLoadContext.GetLoadContext(siblingApp!.GetType().Assembly);
		Assert.AreSame(_testAlcSecondary, resolvedAlc,
			"Sibling app's type must be loaded into the sibling ALC, not the primary one.");

		return siblingApp!;
	}

	[TestMethod]
	public void When_ContentChanges_Then_ContentChangedEventFires()
	{
		// No UITestHelper.Load — AlcContentHost doesn't load in the test tree
		// without a full secondary-ALC setup. The event fires synchronously
		// from OnContentChanged, so we don't need it in the visual tree.
		var host = new AlcContentHost();

		int fireCount = 0;
		object? lastSender = null;
		host.ContentChanged += (sender, _) =>
		{
			fireCount++;
			lastSender = sender;
		};

		// Set first content — event should fire once.
		var content1 = new Border { Background = new SolidColorBrush(Windows.UI.Colors.Blue) };
		host.Content = content1;

		Assert.AreEqual(1, fireCount, "ContentChanged should fire once after the first content is set.");
		Assert.AreSame(host, lastSender, "Sender should be the AlcContentHost instance.");

		// Replace content — event should fire again.
		var content2 = new TextBlock { Text = "Hello" };
		host.Content = content2;

		Assert.AreEqual(2, fireCount, "ContentChanged should fire again when content is replaced.");

		// Set content to null — event should fire for null too.
		host.Content = null;

		Assert.AreEqual(3, fireCount, "ContentChanged should fire when content is set to null.");
	}

	[TestMethod]
	public void When_ContentChangedEventSubscribed_Then_UpdateMergedResourcesCompletedFirst()
	{
		var host = new AlcContentHost();

		bool? hadResourcesAtEventTime = null;
		host.ContentChanged += (_, _) =>
		{
			hadResourcesAtEventTime = true;
		};

		host.Content = new Border();
		Assert.IsTrue(hadResourcesAtEventTime == true, "Event should have fired after content was set.");
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaX11)]
	public async Task When_AlcWindow_Activate_Then_ActivatedEventRaised()
	{
		var (contentHost, alcWindow) = await StartSecondaryAlcAppWithWindowAsync();

		bool activatedFired = false;
		Windows.UI.Core.CoreWindowActivationState? activationState = null;

		alcWindow.Activated += (sender, args) =>
		{
			activatedFired = true;
			activationState = args.WindowActivationState;
		};

		alcWindow.Activate();

		Assert.IsTrue(activatedFired, "Activated event should fire when Activate() is called on ALC window");
		Assert.AreEqual(Windows.UI.Core.CoreWindowActivationState.CodeActivated, activationState,
			"Activation state should be CodeActivated");
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaX11)]
	public async Task When_AlcWindow_Activate_WithFrameNavigation_Then_ActivatedEventRaised()
	{
		var (contentHost, alcWindow) = await StartSecondaryAlcAppWithWindowAsync(new[] { "--use-frame" });

		bool activatedFired = false;
		Windows.UI.Core.CoreWindowActivationState? activationState = null;

		alcWindow.Activated += (sender, args) =>
		{
			activatedFired = true;
			activationState = args.WindowActivationState;
		};

		alcWindow.Activate();

		Assert.IsTrue(activatedFired, "Activated event should fire when Activate() is called on ALC window with Frame navigation");
		Assert.AreEqual(Windows.UI.Core.CoreWindowActivationState.CodeActivated, activationState,
			"Activation state should be CodeActivated when using Frame navigation");
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaX11)]
	public async Task When_AlcWindow_ActivatedRegisteredBeforeContent_Then_ActivatedEventRaised()
	{
		var (_, alcWindow) = await StartSecondaryAlcAppWithWindowAsync(new[] { "--defer-content" });

		bool activatedFired = false;
		Windows.UI.Core.CoreWindowActivationState? activationState = null;

		alcWindow.Activated += (sender, args) =>
		{
			activatedFired = true;
			activationState = args.WindowActivationState;
		};

		ApplyDeferredContentFromSecondaryApp();
		await TestServices.WindowHelper.WaitForIdle();

		alcWindow.Activate();

		Assert.IsTrue(activatedFired, "Activated event should fire when registered before content is set");
		Assert.AreEqual(Windows.UI.Core.CoreWindowActivationState.CodeActivated, activationState,
			"Activation state should be CodeActivated when activated after deferred content is applied");
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaX11)]
	public async Task When_AlcWindow_ActivatedRegisteredBeforeContent_WithFrameNavigation_Then_ActivatedEventRaised()
	{
		var (_, alcWindow) = await StartSecondaryAlcAppWithWindowAsync(new[] { "--defer-content", "--use-frame" });

		bool activatedFired = false;
		Windows.UI.Core.CoreWindowActivationState? activationState = null;

		alcWindow.Activated += (sender, args) =>
		{
			activatedFired = true;
			activationState = args.WindowActivationState;
		};

		ApplyDeferredContentFromSecondaryApp();
		await TestServices.WindowHelper.WaitForIdle();

		alcWindow.Activate();

		Assert.IsTrue(activatedFired, "Activated event should fire when registered before Frame content is set");
		Assert.AreEqual(Windows.UI.Core.CoreWindowActivationState.CodeActivated, activationState,
			"Activation state should be CodeActivated when activated after deferred Frame content is applied");
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaX11)]
	public async Task When_AlcWindow_Then_VisibleReturnsHostVisibility()
	{
		var (contentHost, alcWindow) = await StartSecondaryAlcAppWithWindowAsync();

		// Window should be visible when content host is loaded
		Assert.IsTrue(alcWindow.Visible, "ALC window Visible should be true when ContentHostOverride is loaded");
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaX11)]
	public async Task When_AlcWindow_Then_BoundsMatchesHostBounds()
	{
		var (contentHost, alcWindow) = await StartSecondaryAlcAppWithWindowAsync();

		// Set a specific size on the content host
		contentHost.Width = 400;
		contentHost.Height = 300;
		await TestServices.WindowHelper.WaitForIdle();

		var bounds = alcWindow.Bounds;
		Assert.AreEqual(400, bounds.Width, 1, "ALC window Bounds.Width should match ContentHostOverride.ActualWidth");
		Assert.AreEqual(300, bounds.Height, 1, "ALC window Bounds.Height should match ContentHostOverride.ActualHeight");
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaX11)]
	public async Task When_AlcWindow_HostSizeChanges_Then_SizeChangedEventRaised()
	{
		var (contentHost, alcWindow) = await StartSecondaryAlcAppWithWindowAsync();

		bool sizeChangedFired = false;
		Windows.Foundation.Size? newSize = null;

		alcWindow.SizeChanged += (sender, args) =>
		{
			sizeChangedFired = true;
			newSize = args.Size;
		};

		// Change the content host size
		contentHost.Width = 500;
		contentHost.Height = 400;
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsTrue(sizeChangedFired, "SizeChanged event should fire when ContentHostOverride size changes");
		Assert.IsNotNull(newSize, "SizeChanged args should contain new size");
		Assert.AreEqual(500, newSize!.Value.Width, 1, "New size width should match");
		Assert.AreEqual(400, newSize!.Value.Height, 1, "New size height should match");
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaX11)]
	public async Task When_AlcWindow_Close_Then_ClosedEventRaised()
	{
		var (contentHost, alcWindow) = await StartSecondaryAlcAppWithWindowAsync();

		bool closedFired = false;
		alcWindow.Closed += (sender, args) =>
		{
			closedFired = true;
		};

		alcWindow.Close();

		Assert.IsTrue(closedFired, "Closed event should fire when Close() is called on ALC window");
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaX11)]
	public async Task When_AlcWindow_Close_Then_ContentClearedFromHost()
	{
		var (contentHost, alcWindow) = await StartSecondaryAlcAppWithWindowAsync();

		Assert.IsNotNull(contentHost.Content, "Content should be set before close");

		alcWindow.Close();
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsNull(contentHost.Content, "Content should be cleared from host after Close()");
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaX11)]
	public async Task When_AlcWindow_Close_Then_VisibilityChangedEventRaised()
	{
		var (contentHost, alcWindow) = await StartSecondaryAlcAppWithWindowAsync();

		bool visibilityChangedFired = false;
		bool? newVisibility = null;

		alcWindow.VisibilityChanged += (sender, args) =>
		{
			visibilityChangedFired = true;
			newVisibility = args.Visible;
		};

		alcWindow.Close();

		Assert.IsTrue(visibilityChangedFired, "VisibilityChanged event should fire when Close() is called");
		Assert.IsFalse(newVisibility, "Visibility should be false after Close()");
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaX11)]
	public async Task When_AlcWindow_Close_Then_VisibleReturnsFalse()
	{
		var (contentHost, alcWindow) = await StartSecondaryAlcAppWithWindowAsync();

		Assert.IsTrue(alcWindow.Visible, "Window should be visible before close");

		alcWindow.Close();

		Assert.IsFalse(alcWindow.Visible, "Window Visible should be false after Close()");
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaX11)]
	public async Task When_AlcWindow_ClosedEventHandled_Then_CloseIsCancelled()
	{
		var (contentHost, alcWindow) = await StartSecondaryAlcAppWithWindowAsync();

		alcWindow.Closed += (sender, args) =>
		{
			args.Handled = true; // Cancel the close
		};

		alcWindow.Close();
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsNotNull(contentHost.Content, "Content should NOT be cleared when Closed event is handled");
		Assert.IsTrue(alcWindow.Visible, "Window should still be visible when close is cancelled");
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaX11)]
	public async Task When_AlcWindow_ActivateAfterClose_Then_ThrowsException()
	{
		var (contentHost, alcWindow) = await StartSecondaryAlcAppWithWindowAsync();

		alcWindow.Close();

		bool threwException = false;
		try
		{
			alcWindow.Activate();
		}
		catch (InvalidOperationException)
		{
			threwException = true;
		}

		Assert.IsTrue(threwException, "Activate() should throw InvalidOperationException after Close()");
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaX11)]
	public async Task When_AlcWindow_Then_NotInApplicationHelperWindows()
	{
		var (contentHost, alcWindow) = await StartSecondaryAlcAppWithWindowAsync();

		var windows = Uno.UI.ApplicationHelper.Windows;
		Assert.IsFalse(windows.Contains(alcWindow),
			"ALC window should NOT be in ApplicationHelper.Windows to avoid blocking app closure");
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaX11)]
	public async Task When_AlcContentHost_Then_FrameContentNavigates()
	{
		var contentHost = await StartSecondaryAlcAppAsync(new[] { "--use-frame" });

		var frame = contentHost.Content as Frame;
		Assert.IsNotNull(frame, "ContentHost.Content should be a Frame when --use-frame is specified");

		var page = frame!.Content as FrameworkElement;
		Assert.IsNotNull(page, "Frame should navigate to MainPage and set its Content");

		var titleTextBlock = page!.FindName("TitleTextBlock") as TextBlock;
		Assert.IsNotNull(titleTextBlock, "TitleTextBlock should be discoverable in the navigated page");
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaX11)]
	public async Task When_SecondaryAlcApp_Then_AlcWindowModeEnabled()
	{
		// This test verifies that the ALC window is properly set up in ALC mode.
		// The _alcState is set when content from a secondary ALC is set on the window,
		// which triggers the ALC-specific behavior (content redirection, event forwarding).

		var (contentHost, alcWindow) = await StartSecondaryAlcAppWithWindowAsync();

		// Get the main app's ContentRoot and verify its InputManager IS initialized
		var mainAppContentRoot = Uno.UI.Xaml.Core.VisualTree.GetContentRootForElement(contentHost);
		Assert.IsNotNull(mainAppContentRoot, "Main app ContentRoot should be found");
		Assert.IsTrue(mainAppContentRoot!.InputManager.Initialized,
			"Main app's InputManager should be initialized");

		var alcWindowType = alcWindow.GetType();

		// Verify the ALC window is operating in ALC mode (content redirected).
		// The _alcState is set when content from a secondary ALC is set, which is
		// the reliable way to detect ALC mode.
		var isAlcWindowProperty = alcWindowType.GetProperty("IsAlcWindow",
			BindingFlags.NonPublic | BindingFlags.Instance);
		Assert.IsNotNull(isAlcWindowProperty, "IsAlcWindow property should exist on Window");

		var isAlcWindow = (bool)isAlcWindowProperty!.GetValue(alcWindow)!;
		Assert.IsTrue(isAlcWindow,
			"ALC window should report IsAlcWindow = true after content is set from secondary ALC");

		// The secondary ALC content is hosted inside the main app's visual tree
		// (via ContentHostOverride), so it should use the main app's InputManager.
		Assert.IsNotNull(contentHost.Content,
			"Secondary ALC content should be hosted in the main app's ContentHostOverride");
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaX11)]
	public async Task When_SecondaryAlcApp_Then_KeyboardInputStillWorks()
	{
		// This test verifies that keyboard input continues to work after loading a secondary ALC app.
		// Before the fix, initializing a secondary ALC app would overwrite the TypeScript keyboard
		// handler, breaking keyboard input for the entire application.

		// Create a TextBox in the main app to test keyboard input
		var mainAppTextBox = new TextBox { Name = "MainAppTextBox" };
		var container = new StackPanel
		{
			Children =
			{
				mainAppTextBox,
				new AlcContentHost() // Will host the secondary ALC content
			}
		};

		_contentHost = (AlcContentHost)container.Children[1];

		// Set up the container as the window content
		TestServices.WindowHelper.WindowContent = container;
		await TestServices.WindowHelper.WaitForLoaded(container);
		await TestServices.WindowHelper.WaitForIdle();

		// Verify keyboard input works BEFORE loading secondary ALC
		mainAppTextBox.Focus(FocusState.Programmatic);
		await TestServices.WindowHelper.WaitForIdle();

		await TestServices.KeyboardHelper.InputText("before", mainAppTextBox);
		await TestServices.WindowHelper.WaitForIdle();

		Assert.AreEqual("before", mainAppTextBox.Text,
			"Keyboard input should work before loading secondary ALC app");

		// Clear the textbox
		mainAppTextBox.Text = "";
		await TestServices.WindowHelper.WaitForIdle();

		// Now load the secondary ALC app
		WindowHelper.ContentHostOverride = _contentHost;

		var alcAppPath = await BuildAlcAppAsync();
		Assert.IsNotNull(alcAppPath, "AlcApp build should succeed");

		var alcAppDirectory = Path.GetDirectoryName(alcAppPath)!;
		_testAlc = new TestAssemblyLoadContext(alcAppDirectory);
		var alcAppAssembly = _testAlc.LoadFromAssemblyPath(alcAppPath);

		var programType = alcAppAssembly.GetType("AlcTestApp.Program");
		var mainMethod = programType!.GetMethod("Main", BindingFlags.Public | BindingFlags.Static);
		var appType = alcAppAssembly.GetType("AlcTestApp.App");

		var mainThread = new System.Threading.Thread(() =>
		{
			try
			{
				mainMethod!.Invoke(null, new object[] { Array.Empty<string>() });
			}
			catch (Exception ex)
			{
				this.Log().Error("Secondary ALC app execution failed", ex);
			}
		})
		{
			IsBackground = true,
			Name = "AlcApp-Main"
		};
		mainThread.Start();

		await WaitForSecondaryWindowAsync(appType!);
		await TestServices.WindowHelper.WaitForIdle();

		// Verify secondary ALC content is loaded
		Assert.IsNotNull(_contentHost.Content,
			"Secondary ALC content should be loaded in ContentHost");

		// Verify keyboard input still works AFTER loading secondary ALC
		mainAppTextBox.Focus(FocusState.Programmatic);
		await TestServices.WindowHelper.WaitForIdle();

		await TestServices.KeyboardHelper.InputText("after", mainAppTextBox);
		await TestServices.WindowHelper.WaitForIdle();

		Assert.AreEqual("after", mainAppTextBox.Text,
			"Keyboard input should still work after loading secondary ALC app");
	}

	/// <summary>
	/// Regression test for cross-ALC theme bleed during hot reload. A host app (Dark) renders a
	/// secondary-ALC consumer app (Light) inside an <see cref="AlcContentHost"/>. When the inner
	/// app's hot-reload resource refresh runs (<c>UpdateResourceBindingsForHotReload</c>, invoked
	/// per secondary app by <c>ClientHotReloadProcessor.RefreshResourcesForApp</c>), it must NOT
	/// re-theme the host's shell to its own theme.
	/// </summary>
	/// <remarks>
	/// Root cause guarded: <c>Application.OnResourcesChanged</c> iterates the process-global
	/// content-root list and applied <c>this.InternalRequestedTheme</c> to every root — so the
	/// inner (Light) app re-themed the host's (Dark) content root. The fix derives the theme from
	/// the app that OWNS each content root. Two host probes are asserted: a non-themed Border
	/// (inherits the host app theme) and a Border nested under an explicit <c>RequestedTheme=Dark</c>
	/// element (mirrors studio.live's themed shell root), so a run on the unfixed code pinpoints
	/// exactly which shell elements flip.
	/// </remarks>
	[TestMethod]
	[RequiresFullWindow]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaX11)]
	public async Task When_SecondaryAlcApp_HotReloadsResources_Then_HostThemeUnchanged()
	{
		var hostWasExplicit = Application.Current.IsThemeSetExplicitly;
		var hostOriginal = Application.Current.RequestedTheme;
		var hadSecondaryApps = Application.HasSecondaryApps;

		Application.HasSecondaryApps = true;
		Application.Current.SetExplicitRequestedTheme(ApplicationTheme.Dark);
		try
		{
			// Host shell probes, siblings of the ALC host:
			//  - probeA inherits the host app theme (no explicit RequestedTheme).
			//  - probeBChild sits under an explicitly Dark element (mirrors AppRoot's themed subtree).
			var probeA = new Border { Width = 20, Height = 20 };
			var probeBChild = new Border { Width = 20, Height = 20 };
			var probeB = new Border { RequestedTheme = ElementTheme.Dark, Child = probeBChild };

			_contentHost = new AlcContentHost();
			var container = new StackPanel { Children = { probeA, probeB, _contentHost } };

			WindowHelper.ContentHostOverride = _contentHost;
			TestServices.WindowHelper.WindowContent = container;
			await TestServices.WindowHelper.WaitForLoaded(container);
			await TestServices.WindowHelper.WaitForIdle();

			// Baseline established before any secondary app exists: the host shell is Dark.
			Assert.AreEqual(ElementTheme.Dark, probeA.ActualTheme, "Baseline: non-themed host probe must be Dark.");
			Assert.AreEqual(ElementTheme.Dark, probeBChild.ActualTheme, "Baseline: explicitly-Dark host subtree must be Dark.");

			// Boot the secondary ALC app into _contentHost.
			var secondaryApp = await BootSecondaryAlcAppAsync();

			// Force the inner app to Light so host (Dark) != secondary (Light) deterministically.
			secondaryApp.SetExplicitRequestedTheme(ApplicationTheme.Light);
			await TestServices.WindowHelper.WaitForIdle();

			// Act: the inner (Light) app performs its hot-reload resource refresh — exactly what
			// ClientHotReloadProcessor.RefreshResourcesForApp invokes on each secondary app.
			secondaryApp.UpdateResourceBindingsForHotReload();
			await TestServices.WindowHelper.WaitForIdle();

			// Assert: the inner app's Light theme must not have bled onto the host shell.
			Assert.AreEqual(ElementTheme.Dark, probeA.ActualTheme,
				"A secondary (Light) ALC app's hot-reload resource refresh re-themed the host's non-themed " +
				"shell to Light. OnResourcesChanged applied the inner app's theme to the host content root.");
			Assert.AreEqual(ElementTheme.Dark, probeBChild.ActualTheme,
				"A secondary (Light) ALC app's hot-reload resource refresh re-themed the host's " +
				"explicitly-Dark shell subtree to Light.");
		}
		finally
		{
			Application.HasSecondaryApps = hadSecondaryApps;

			if (hostWasExplicit)
			{
				Application.Current.SetExplicitRequestedTheme(hostOriginal);
			}
			else
			{
				Application.Current.SetExplicitRequestedTheme(null);
			}
		}
	}

	/// <summary>
	/// Reverse direction of <see cref="When_SecondaryAlcApp_HotReloadsResources_Then_HostThemeUnchanged"/>:
	/// with a secondary ALC app present, a HOST theme switch must still re-theme the host's own
	/// content root, and must not mutate the secondary app's requested theme.
	/// </summary>
	/// <remarks>
	/// Guards the owning-app lookup in <c>Application.OnResourcesChanged</c> against misattribution in
	/// the host→secondary direction: if <c>GetOwningApplication</c> wrongly resolved the host's content
	/// root to the secondary app, the host's theme switches would stop taking effect (the root would be
	/// re-themed with the secondary app's theme instead of the host's new one).
	/// </remarks>
	[TestMethod]
	[RequiresFullWindow]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaX11)]
	public async Task When_HostThemeChanges_WithSecondaryAlcApp_Then_HostThemeStillApplies()
	{
		var hostWasExplicit = Application.Current.IsThemeSetExplicitly;
		var hostOriginal = Application.Current.RequestedTheme;
		var hadSecondaryApps = Application.HasSecondaryApps;

		Application.HasSecondaryApps = true;
		Application.Current.SetExplicitRequestedTheme(ApplicationTheme.Dark);
		try
		{
			// Non-themed host probe: follows the host app theme.
			var probe = new Border { Width = 20, Height = 20 };

			_contentHost = new AlcContentHost();
			var container = new StackPanel { Children = { probe, _contentHost } };

			WindowHelper.ContentHostOverride = _contentHost;
			TestServices.WindowHelper.WindowContent = container;
			await TestServices.WindowHelper.WaitForLoaded(container);
			await TestServices.WindowHelper.WaitForIdle();

			Assert.AreEqual(ElementTheme.Dark, probe.ActualTheme, "Baseline: host probe must be Dark.");

			var secondaryApp = await BootSecondaryAlcAppAsync();
			secondaryApp.SetExplicitRequestedTheme(ApplicationTheme.Light);
			await TestServices.WindowHelper.WaitForIdle();

			// Act 1: host switches Dark -> Light while the secondary (Light) app is loaded.
			Application.Current.SetExplicitRequestedTheme(ApplicationTheme.Light);
			await TestServices.WindowHelper.WaitForIdle();

			Assert.AreEqual(ElementTheme.Light, probe.ActualTheme,
				"A host theme switch must still re-theme the host's own content root when secondary " +
				"ALC apps are present. The owning-app lookup in OnResourcesChanged misattributed the " +
				"host's content root.");

			// Act 2: host switches back Light -> Dark. The secondary app stays explicitly Light, so
			// this transition proves the two apps' themes are independent in this direction too.
			Application.Current.SetExplicitRequestedTheme(ApplicationTheme.Dark);
			await TestServices.WindowHelper.WaitForIdle();

			Assert.AreEqual(ElementTheme.Dark, probe.ActualTheme,
				"The host's second theme switch (back to Dark) must re-theme its content root.");
			Assert.AreEqual(ApplicationTheme.Light, secondaryApp.RequestedTheme,
				"Host theme switches must not mutate the secondary ALC app's requested theme.");
		}
		finally
		{
			Application.HasSecondaryApps = hadSecondaryApps;

			if (hostWasExplicit)
			{
				Application.Current.SetExplicitRequestedTheme(hostOriginal);
			}
			else
			{
				Application.Current.SetExplicitRequestedTheme(null);
			}
		}
	}

	/// <summary>
	/// Pins the content-root ownership mechanism itself. A secondary ALC app's window roots either
	/// null content (redirected to the <see cref="AlcContentHost"/>) or a shared default-ALC framework
	/// type — in both cases inferring ownership from the root content's <c>Type</c> misattributes the
	/// root to the host. Ownership must instead come from the window's creator ALC, captured at
	/// construction (<c>Window.OwnerAssemblyLoadContext</c>).
	/// </summary>
	[TestMethod]
	[RequiresFullWindow]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaX11)]
	public async Task When_SecondaryAlcWindow_RootContentIsSharedOrNull_Then_OwnedBySecondaryApp()
	{
		var hadSecondaryApps = Application.HasSecondaryApps;
		try
		{
			var (contentHost, alcWindow) = await StartSecondaryAlcAppWithWindowAsync();

			var secondaryApp = Application.GetForAssemblyLoadContext(_testAlc!);
			Assert.IsNotNull(secondaryApp, "Secondary ALC app must be registered.");

			// The window created by the secondary app's code must carry its creator's ALC.
			Assert.AreSame(_testAlc, alcWindow.OwnerAssemblyLoadContext,
				"A window constructed by secondary-ALC code must be tagged with that ALC at construction.");

			var alcContentRoot = alcWindow.RootElement?.XamlRoot?.VisualTree?.ContentRoot;
			Assert.IsNotNull(alcContentRoot, "The secondary app's window must have a content root.");

			// Precondition making the pin meaningful: the root content gives type-based inference
			// nothing to identify the secondary app by (null, or a shared default-ALC type).
			var rootContent = alcContentRoot!.XamlRoot?.Content;
			if (rootContent is not null)
			{
				Assert.AreSame(
					System.Runtime.Loader.AssemblyLoadContext.Default,
					System.Runtime.Loader.AssemblyLoadContext.GetLoadContext(rootContent.GetType().Assembly),
					"Test precondition: the secondary window's root content is expected to be null or a " +
					"shared default-ALC type, the case where type-based ownership inference fails.");
			}

			var getOwningApplication = typeof(Application).GetMethod(
				"GetOwningApplication",
				BindingFlags.NonPublic | BindingFlags.Static);
			Assert.IsNotNull(getOwningApplication,
				"Application.GetOwningApplication must exist on enhanced-lifecycle targets.");

			var alcRootOwner = getOwningApplication!.Invoke(null, new object?[] { alcContentRoot });
			Assert.AreSame(secondaryApp, alcRootOwner,
				"The secondary app's content root must be owned by the secondary app even though its " +
				"root content type cannot identify it. Owner-window ALC tagging has regressed; theme " +
				"refreshes will attribute this root to the host.");

			// Contrast: the host's content root (hosting the AlcContentHost) maps to the host app.
			var hostContentRoot = contentHost.XamlRoot?.VisualTree?.ContentRoot;
			Assert.IsNotNull(hostContentRoot, "The host's content root must be reachable from the AlcContentHost.");

			var hostRootOwner = getOwningApplication.Invoke(null, new object?[] { hostContentRoot });
			Assert.AreSame(Application.Current, hostRootOwner,
				"The host's content root must remain owned by the host application.");
		}
		finally
		{
			Application.HasSecondaryApps = hadSecondaryApps;
		}
	}

	/// <summary>
	/// Boots the AlcApp test application into a secondary ALC, assuming the caller has already set up
	/// <see cref="WindowHelper.ContentHostOverride"/> and the window content. Returns the registered
	/// secondary <see cref="Application"/>. Use <see cref="StartSecondaryAlcAppAsync"/> instead when the
	/// content host should be the whole window content.
	/// </summary>
	private async Task<Application> BootSecondaryAlcAppAsync()
	{
		var alcAppPath = await BuildAlcAppAsync();
		Assert.IsNotNull(alcAppPath, "AlcApp build should succeed");
		var alcAppDirectory = Path.GetDirectoryName(alcAppPath)!;
		_testAlc = new TestAssemblyLoadContext(alcAppDirectory);
		var alcAppAssembly = _testAlc.LoadFromAssemblyPath(alcAppPath);
		var programType = alcAppAssembly.GetType("AlcTestApp.Program");
		Assert.IsNotNull(programType, "Program type should be found in AlcApp assembly");
		var mainMethod = programType!.GetMethod("Main", BindingFlags.Public | BindingFlags.Static);
		Assert.IsNotNull(mainMethod, "Main method should exist");
		var appType = alcAppAssembly.GetType("AlcTestApp.App");
		Assert.IsNotNull(appType, "App type should be found in AlcApp assembly");

		var mainThread = new System.Threading.Thread(() =>
		{
			try
			{
				mainMethod!.Invoke(null, new object[] { Array.Empty<string>() });
			}
			catch (Exception ex)
			{
				this.Log().Error("Secondary ALC app execution failed", ex);
			}
		})
		{
			IsBackground = true,
			Name = "AlcApp-Main"
		};
		mainThread.Start();
		await WaitForSecondaryWindowAsync(appType!);
		await TestServices.WindowHelper.WaitForIdle();

		var secondaryApp = Application.GetForAssemblyLoadContext(_testAlc!);
		Assert.IsNotNull(secondaryApp, "Secondary ALC app must be registered.");
		return secondaryApp!;
	}

	private async Task<(AlcContentHost contentHost, Window alcWindow)> StartSecondaryAlcAppWithWindowAsync(string[]? launchArguments = null)
	{
		var contentHost = await StartSecondaryAlcAppAsync(launchArguments);

		// Get the Window from the secondary ALC app via reflection
		var alcAppAssembly = _testAlc!.Assemblies.First(a => a.GetName().Name == "Uno.UI.RuntimeTests.AlcApp");
		var appType = alcAppAssembly.GetType("AlcTestApp.App");
		Assert.IsNotNull(appType, "App type should be found");

		var testWindowField = appType!.GetField("TestWindow", BindingFlags.NonPublic | BindingFlags.Static);
		Assert.IsNotNull(testWindowField, "TestWindow field should be found");

		var alcWindow = testWindowField!.GetValue(null) as Window;
		Assert.IsNotNull(alcWindow, "TestWindow should be a Window instance");

		return (contentHost, alcWindow!);
	}

	private async Task<AlcContentHost> StartSecondaryAlcAppAsync(string[]? launchArguments = null)
	{
		var alcAppPath = await BuildAlcAppAsync();
		Assert.IsNotNull(alcAppPath, "AlcApp build should succeed");
		Assert.IsTrue(File.Exists(alcAppPath), $"AlcApp assembly should exist at {alcAppPath}");

		var alcAppDirectory = Path.GetDirectoryName(alcAppPath)!;
		_testAlc = new TestAssemblyLoadContext(alcAppDirectory);
		var alcAppAssembly = _testAlc.LoadFromAssemblyPath(alcAppPath);
		Assert.IsNotNull(alcAppAssembly, "AlcApp assembly should be loaded");

		var programType = alcAppAssembly.GetType("AlcTestApp.Program");
		Assert.IsNotNull(programType, "Program type should be found in AlcApp assembly");

		var mainMethod = programType!.GetMethod("Main",
			BindingFlags.Public | BindingFlags.Static);
		Assert.IsNotNull(mainMethod, "Main method should exist");

		var appType = alcAppAssembly.GetType("AlcTestApp.App");
		Assert.IsNotNull(appType, "App type should be found in AlcApp assembly");

		// Enable ALC-aware resource resolution before loading the secondary app
		Application.HasSecondaryApps = true;

		_contentHost = new AlcContentHost();
		WindowHelper.ContentHostOverride = _contentHost;
		TestServices.WindowHelper.WindowContent = _contentHost;

		var mainThread = new System.Threading.Thread(() =>
		{
			try
			{
				mainMethod!.Invoke(null, new object[] { launchArguments ?? Array.Empty<string>() });
			}
			catch (Exception ex)
			{
				this.Log().Error("Secondary ALC app execution failed", ex);
			}
		})
		{
			IsBackground = true,
			Name = "AlcApp-Main"
		};
		mainThread.Start();

		await WaitForSecondaryWindowAsync(appType!);
		await TestServices.WindowHelper.WaitForIdle();

		return _contentHost!;
	}

	private void ApplyDeferredContentFromSecondaryApp()
	{
		var alcAppAssembly = _testAlc!.Assemblies.First(a => a.GetName().Name == "Uno.UI.RuntimeTests.AlcApp");
		var appType = alcAppAssembly.GetType("AlcTestApp.App");
		Assert.IsNotNull(appType, "App type should be found in AlcApp assembly");

		var applyDeferredContent = appType!.GetMethod("ApplyDeferredContent", BindingFlags.NonPublic | BindingFlags.Static);
		Assert.IsNotNull(applyDeferredContent, "ApplyDeferredContent should be available on AlcTestApp.App");

		applyDeferredContent!.Invoke(null, null);
	}

	private static async Task WaitForSecondaryWindowAsync(Type appType)
	{
		var testWindowProperty = appType.GetField("TestWindow", BindingFlags.NonPublic | BindingFlags.Static);
		Assert.IsNotNull(testWindowProperty, "App.TestWindow property should be discoverable via reflection");

		var maxWaitTime = TimeSpan.FromSeconds(30);
		var waitTimer = Stopwatch.StartNew();
		while (waitTimer.Elapsed < maxWaitTime)
		{
			if (testWindowProperty!.GetValue(null) is not null)
			{
				return;
			}

			await Task.Delay(100);
		}

		throw new InvalidOperationException("Timed out waiting for AlcTestApp.App.TestWindow to be assigned.");
	}

	private static string GetAlcAppPath()
	{
		var basePath = Path.GetDirectoryName(Application.Current.GetType().Assembly.Location)!;

		var searchPaths = new[] {
			Path.Combine(basePath, "..", "..", "..", "..", "..", "Uno.UI.RuntimeTests", "Tests", "AssemblyLoadContext", "AlcApp"),
			Path.Combine(basePath, "..", "..", "src", "Uno.UI.RuntimeTests", "Tests", "AssemblyLoadContext", "AlcApp"),
			Path.Combine(basePath, "..", "..", ".."), // CI
		};

		var hrAppPath = searchPaths
			.Where(p => File.Exists(Path.Combine(p, "Uno.UI.RuntimeTests.AlcApp.csproj")))
			.FirstOrDefault();

		if (hrAppPath is null)
		{
			throw new InvalidOperationException("Unable to find AlcApp folder in " + string.Join(", ", searchPaths));
		}

		return hrAppPath;
	}

	/// <summary>
	/// Builds the AlcApp test project and returns the path to the compiled assembly.
	/// </summary>
	private async Task<string?> BuildAlcAppAsync()
	{
		var alcAppProjectPath = Path.Combine(GetAlcAppPath(), "Uno.UI.RuntimeTests.AlcApp.csproj");
		Assert.IsTrue(File.Exists(alcAppProjectPath), $"AlcApp project should exist at {alcAppProjectPath}");

		// Determine target framework and configuration
		var targetFramework =
#if NET10_0
			"net10.0";
#elif NET9_0
			"net9.0";
#else
#error This .NET version is not yet supported by the test project build script. Supported versions: NET10_0, NET9_0. To add support, add a new '#elif NETXX_X' block with the appropriate targetFramework string.
#endif

		// The CI environment builds build tooling in debug (related to the HR tests)
		var configuration = "Debug";

		// Build the project using dotnet CLI
		var alcAppDir = Path.GetDirectoryName(alcAppProjectPath)!;
		var outputPath = Path.Combine(alcAppDir, "bin", configuration, targetFramework);
		var assemblyPath = Path.Combine(outputPath, "Uno.UI.RuntimeTests.AlcApp.dll");

		// Need to build the project
		var startInfo = new System.Diagnostics.ProcessStartInfo
		{
			FileName = "dotnet",
			Arguments = $"build \"{alcAppProjectPath}\" -c {configuration} -f {targetFramework}",
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
			CreateNoWindow = true,
			WorkingDirectory = alcAppDir
		};

		using var process = System.Diagnostics.Process.Start(startInfo);
		Assert.IsNotNull(process, "dotnet build process should start");

		var outputTask = process.StandardOutput.ReadToEndAsync();
		var errorTask = process.StandardError.ReadToEndAsync();

		await Task.WhenAll(outputTask, errorTask);
		await process.WaitForExitAsync();

		if (process.ExitCode != 0)
		{
			Assert.Fail($"AlcApp build failed with exit code {process.ExitCode}.\nOutput: {outputTask.Result}\nError: {errorTask.Result}");
		}

		Assert.IsTrue(File.Exists(assemblyPath),
			$"AlcApp assembly should exist after build at {assemblyPath}.\nBuild output: {outputTask.Result}");

		return assemblyPath;
	}

}
#endif
