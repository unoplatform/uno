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
	public void When_Scoped_Cleanup_Then_Sibling_Alc_Application_Registration_Kept()
	{
		// The ALC → Application registry is DESTRUCTIVE state, not a rebuildable cache: it is
		// populated only by each secondary app's constructor and never rebuilt on demand, and
		// GetForAssemblyLoadContext / secondary-app resource fallback / window ownership / theme
		// lookups all read it. Tearing ONE app down (dying ALC known) must therefore remove only
		// that ALC's registration — a wide sweep would unregister a live sibling secondary app
		// and permanently break its lookups.
		var dyingAlc = new AssemblyLoadContext("Given_AlcCacheSweep.dyingApp", isCollectible: true);
		var siblingAlc = new AssemblyLoadContext("Given_AlcCacheSweep.siblingApp", isCollectible: true);
		try
		{
			var dyingApp = CreateAlcApplication(dyingAlc);
			var siblingApp = CreateAlcApplication(siblingAlc);

			Assert.AreSame(dyingApp, Application.GetForAssemblyLoadContext(dyingAlc), "Pre-condition: the dying ALC's application must be registered.");
			Assert.AreSame(siblingApp, Application.GetForAssemblyLoadContext(siblingAlc), "Pre-condition: the sibling ALC's application must be registered.");

			Application.CleanupNonDefaultAlcCaches(dyingAlc);

			Assert.IsNull(
				Application.GetForAssemblyLoadContext(dyingAlc),
				"The scoped sweep must drop the dying ALC's Application registration; otherwise the registry pins the unloaded context.");
			Assert.AreSame(
				siblingApp,
				Application.GetForAssemblyLoadContext(siblingAlc),
				"The scoped sweep must keep a live sibling's Application registration — it is never re-created, so dropping it breaks GetForAssemblyLoadContext/ownership/theme lookups for the sibling.");
		}
		finally
		{
			Application.RemoveAlcApplication(siblingAlc);
			Application.RemoveAlcApplication(dyingAlc);
			dyingAlc.Unload();
			siblingAlc.Unload();
		}
	}

	[TestMethod]
	public void When_Unscoped_NonDestructive_Cleanup_Then_Alc_Application_Registrations_Kept()
	{
		// A per-window teardown that could not identify its dying ALC clears the rebuildable
		// caches but must SKIP the destructive sweeps — including the Application registry —
		// rather than unregister every live secondary app.
		var aliveAlc = new AssemblyLoadContext("Given_AlcCacheSweep.aliveApp", isCollectible: true);
		try
		{
			var aliveApp = CreateAlcApplication(aliveAlc);
			Assert.AreSame(aliveApp, Application.GetForAssemblyLoadContext(aliveAlc), "Pre-condition: the application must be registered.");

			Application.CleanupNonDefaultAlcCaches(dyingAlc: null);

			Assert.AreSame(
				aliveApp,
				Application.GetForAssemblyLoadContext(aliveAlc),
				"A per-window teardown with an unknown dying ALC must not unregister a live secondary app's Application — the registration is never re-created.");
		}
		finally
		{
			Application.RemoveAlcApplication(aliveAlc);
			aliveAlc.Unload();
		}
	}

	/// <summary>
	/// Instantiates <see cref="AlcSweepTestApplication"/> from a copy of this test assembly loaded
	/// into <paramref name="alc"/> — the base <see cref="Application"/> constructor registers a
	/// non-default-ALC instance in the ALC → Application registry, the same path a real secondary
	/// app takes at bootstrap.
	/// </summary>
	private static Application CreateAlcApplication(AssemblyLoadContext alc)
	{
		var assembly = alc.LoadFromAssemblyPath(typeof(Given_AlcCacheSweep).Assembly.Location);
		var appType = assembly.GetType(typeof(AlcSweepTestApplication).FullName!, throwOnError: true)!;
		return (Application)Activator.CreateInstance(appType)!;
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

/// <summary>
/// Minimal <see cref="Application"/> subclass instantiated from copies of this test assembly
/// loaded into collectible ALCs. The base constructor's non-default-ALC branch registers the
/// instance in the ALC → Application registry, exactly like a real secondary app's bootstrap.
/// </summary>
public class AlcSweepTestApplication : Application
{
}
