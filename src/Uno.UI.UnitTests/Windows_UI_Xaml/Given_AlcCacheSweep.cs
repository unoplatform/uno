#nullable enable

using System;
using System.Collections.Generic;
using System.Runtime.Loader;
using Microsoft.UI.Xaml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Helpers;

namespace Uno.UI.Tests.Windows_UI_Xaml;

/// <summary>
/// Covers the shared ALC teardown-sweep primitives:
/// <list type="bullet">
/// <item><see cref="AlcCacheSweep.RemoveNonDefaultAlcEntries{TValue}"/> — the single engine every
/// rebuildable Type-keyed sweep delegates to, including <c>UIElementNativeRegistrar._classNames</c>
/// (WASM/JS-interop bound, so exercised here through its engine) and <c>HtmlElementHelper</c>.</item>
/// <item><see cref="Application.RunCleanupStep"/> — the per-sweep fault isolator that keeps one
/// throwing sweep from aborting the rest of the teardown chain (and silently re-leaking the ALC).</item>
/// </list>
/// </summary>
[TestClass]
public class Given_AlcCacheSweep
{
	[TestMethod]
	public void When_RemoveNonDefaultAlcEntries_Then_Collectible_Key_Dropped_And_Default_Kept()
	{
		// This is the exact engine UIElementNativeRegistrar._classNames (Type -> registration id) uses
		// on WASM: a collectible-ALC element type must be dropped (it pins the previewed app's context),
		// while default-ALC (framework/host) and a LIVE SIBLING's registrations must survive. The
		// registrar itself is WASM/JS-interop bound, so its drop logic is asserted here on the shared
		// engine using a Type -> int dictionary that mirrors _classNames.
		var collectibleAlc = new AssemblyLoadContext("Given_AlcCacheSweep.collectible", isCollectible: true);
		try
		{
			var collectibleKey = collectibleAlc
				.LoadFromAssemblyPath(typeof(Given_AlcCacheSweep).Assembly.Location)
				.GetType(typeof(Given_AlcCacheSweep).FullName!, throwOnError: true)!;

			Assert.IsTrue(collectibleKey.IsCollectible, "Pre-condition: the stand-in key must belong to the collectible ALC.");

			var defaultKey = typeof(Given_AlcCacheSweep);
			var registrations = new Dictionary<Type, int>
			{
				[defaultKey] = 1,
				[collectibleKey] = 2,
			};

			var removed = AlcCacheSweep.RemoveNonDefaultAlcEntries(registrations);

			Assert.AreEqual(1, removed, "Exactly the one collectible-ALC entry must be removed.");
			Assert.IsFalse(
				registrations.ContainsKey(collectibleKey),
				"The sweep must drop the collectible-ALC key; otherwise the registrar pins the previewed app's context after unload.");
			Assert.IsTrue(
				registrations.ContainsKey(defaultKey),
				"The sweep must keep default-ALC (framework/host) and live-sibling keys — these caches rebuild on demand and must not lose live registrations.");
		}
		finally
		{
			collectibleAlc.Unload();
		}
	}

	[TestMethod]
	public void When_A_Cleanup_Step_Throws_Then_Later_Steps_Still_Run()
	{
		// The teardown chokepoint runs each sweep through RunCleanupStep so a single failure cannot
		// abort the rest of the chain — the central release mechanisms (visual-tree unpin walk,
		// per-WindowId teardown) must still execute, otherwise the ALC silently stays pinned while
		// CloseAlcWindow reports success.
		var firstRan = false;
		var laterRan = false;

		Application.RunCleanupStep("first-ok", () => firstRan = true);
		Application.RunCleanupStep("throwing", static () => throw new InvalidOperationException("injected sweep fault"));
		Application.RunCleanupStep("later-ok", () => laterRan = true);

		Assert.IsTrue(firstRan, "A step before the fault must run.");
		Assert.IsTrue(laterRan, "A step after a throwing sweep must still run — one failing sweep must not abort the teardown chain.");
	}
}
