#if HAS_UNO
#nullable enable

using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml;
using Uno.UI;

namespace Uno.UI.RuntimeTests.Tests.HotReload;

/// <summary>
/// Pins the refresh count returned by <c>ClientHotReloadProcessor.UpdateResourceDictionaries</c>.
/// That count gates the per-application binding re-evaluation in
/// <c>ClientHotReloadProcessor.RefreshResourcesForApp</c>: <c>UpdateResourceBindingsForHotReload()</c>
/// runs only when at least one source-backed merged dictionary was actually refreshed. A wrong
/// count therefore either skips a needed binding refresh (stale UI after a hot reload) or runs
/// it for apps the update did not touch.
/// </summary>
/// <remarks>
/// The processor's members are private and live in <c>Uno.UI.RemoteControl</c>, which only some
/// test heads reference — so the type and methods are resolved via reflection and the test is
/// inconclusive where the assembly is unavailable. The hot-reload flow is modeled faithfully:
/// a source-backed dictionary is registered, merged, then re-registered (what the re-invoked
/// SG registration does on metadata update) before the refresh walk runs.
/// </remarks>
[TestClass]
[RunsOnUIThread]
public class Given_HotReload_UpdateResourceDictionaries
{
	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaX11)]
	public void When_UpdatedSources_Then_RefreshCountMatchesAndOnlyMatchedEntriesReload()
	{
		var processorType = Type.GetType(
			"Uno.UI.RemoteControl.HotReload.ClientHotReloadProcessor, Uno.UI.RemoteControl",
			throwOnError: false);
		if (processorType is null)
		{
			Assert.Inconclusive("Uno.UI.RemoteControl is not available on this target.");
		}

		var updateMethod = processorType!.GetMethod(
			"UpdateResourceDictionaries",
			BindingFlags.NonPublic | BindingFlags.Static);
		Assert.IsNotNull(updateMethod, "ClientHotReloadProcessor.UpdateResourceDictionaries must exist.");

		var normalizeMethod = processorType.GetMethod(
			"NormalizeResourceDictionarySource",
			BindingFlags.NonPublic | BindingFlags.Static);
		Assert.IsNotNull(normalizeMethod, "ClientHotReloadProcessor.NormalizeResourceDictionarySource must exist.");

		int Update(ResourceDictionary root, params string[] sources)
			=> (int)updateMethod!.Invoke(
				null,
				new object[] { new HashSet<string>(sources, StringComparer.OrdinalIgnoreCase), root })!;

		string Normalize(string uri)
			=> (string)normalizeMethod!.Invoke(null, new object[] { new Uri(uri) })!;

		// Guid-suffixed URIs keep the global registry free of cross-test interference.
		var unique = Guid.NewGuid().ToString("N");
		var uriA = $"ms-appx:///RefreshCountTests/{unique}/A.xaml";
		var uriB = $"ms-appx:///RefreshCountTests/{unique}/B.xaml";

		ResourceResolver.RegisterResourceDictionaryBySource(uriA, context: null, dictionary: () => new ResourceDictionary { ["Marker"] = "A-v1" });
		ResourceResolver.RegisterResourceDictionaryBySource(uriB, context: null, dictionary: () => new ResourceDictionary { ["Marker"] = "B-v1" });

		// dictA sits at the top level; dictB is nested inside a plain (source-less) dictionary,
		// mirroring overrides referenced from typed theme dictionaries.
		var dictA = new ResourceDictionary { Source = new Uri(uriA) };
		var dictB = new ResourceDictionary { Source = new Uri(uriB) };
		var nesting = new ResourceDictionary { MergedDictionaries = { dictB } };
		var root = new ResourceDictionary { MergedDictionaries = { dictA, nesting } };

		// A metadata update rebuilds the dictionaries: the re-invoked SG registration replaces
		// the registered factories before the refresh walk runs.
		ResourceResolver.RegisterResourceDictionaryBySource(uriA, context: null, dictionary: () => new ResourceDictionary { ["Marker"] = "A-v2" });
		ResourceResolver.RegisterResourceDictionaryBySource(uriB, context: null, dictionary: () => new ResourceDictionary { ["Marker"] = "B-v2" });

		// No source matches: the count must be 0 — this is what makes RefreshResourcesForApp
		// skip UpdateResourceBindingsForHotReload for untouched applications.
		Assert.AreEqual(
			0,
			Update(root, $"RefreshCountTests/{unique}/DoesNotExist.xaml"),
			"An update matching no merged dictionary must report zero refreshes.");
		Assert.AreSame(dictA, root.MergedDictionaries[0], "Unmatched dictionaries must not be reloaded.");
		Assert.AreSame(dictB, nesting.MergedDictionaries[0], "Unmatched nested dictionaries must not be reloaded.");

		// One top-level match: count 1, only the matched entry is replaced.
		Assert.AreEqual(
			1,
			Update(root, Normalize(uriA)),
			"An update matching one top-level dictionary must report exactly one refresh.");
		Assert.AreNotSame(dictA, root.MergedDictionaries[0], "The matched dictionary must be reloaded.");
		Assert.AreEqual("A-v2", root.MergedDictionaries[0]["Marker"], "The reload must pull the re-registered (post-update) content.");
		Assert.AreSame(dictB, nesting.MergedDictionaries[0], "A non-matched nested dictionary must not be reloaded.");

		// Nested match: the walk must reach dictionaries nested under source-less entries.
		Assert.AreEqual(
			1,
			Update(root, Normalize(uriB)),
			"An update matching one nested dictionary must report exactly one refresh.");
		Assert.AreNotSame(dictB, nesting.MergedDictionaries[0], "The matched nested dictionary must be reloaded.");
		Assert.AreEqual("B-v2", nesting.MergedDictionaries[0]["Marker"], "The nested reload must pull the re-registered content.");
	}
}
#endif
