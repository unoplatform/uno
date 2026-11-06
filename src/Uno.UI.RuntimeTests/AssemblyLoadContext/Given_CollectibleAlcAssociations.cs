#if HAS_UNO
#nullable enable

using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;
using Windows.UI;

namespace Uno.UI.RuntimeTests.Tests.AssemblyLoadContext;

/// <summary>
/// A shared (host-lifetime) resource consumed by a secondary-ALC element records that element as
/// its InheritanceContext parent (<c>DependencyObject._associatedParentRef</c>, a weak reference).
/// Nothing resets it when the element's AssemblyLoadContext unloads. These tests stage that
/// association with an element whose type lives in a collectible (RunAndCollect) assembly, keep the
/// element alive across <see cref="Application.CleanupNonDefaultAlcCaches"/>, and assert that the
/// sweep drops the stale association so the resource re-associates with its next live consumer (a
/// stale parent would otherwise count as a second parent and disable inheritance for good). The
/// association is observed directly: a ResourceDictionary item never inherits DataContext
/// (<c>DependencyObject.IsResourceDictionaryItem</c>), so bindings on it cannot reveal its parent.
/// </summary>
[TestClass]
[RunsOnUIThread]
// The sweep assertions run identically on every Skia target, but the trailing collection check depends
// on GC reclaiming the released collectible element within a bounded GC.Collect() loop. The WASM and
// UIKit (Mono) runtimes have non-deterministic/conservative GC and don't reliably reclaim it, so the
// check is unreliable there. Coverage stays on CoreCLR-backed Skia (Desktop/Android) + WinAppSDK.
[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaWasm | RuntimeTestPlatforms.SkiaUIKit)]
public class Given_CollectibleAlcAssociations
{
	private const string ProbeBrushKey = "CollectibleAssociationProbeBrush";
	private const string ProbeThemeBrushKey = "CollectibleAssociationProbeThemeBrush";

	private ResourceDictionary? _stagedThemedDictionary;

	[TestCleanup]
	public void Cleanup()
	{
		Application.Current.Resources.Remove(ProbeBrushKey);

		if (_stagedThemedDictionary is not null)
		{
			Application.Current.Resources.MergedDictionaries.Remove(_stagedThemedDictionary);
			_stagedThemedDictionary = null;
		}
	}

	[TestMethod]
	public void When_HostResourceAssociatedToCollectibleElement_Then_CleanupClearsStaleAssociation()
	{
		var brush = new SolidColorBrush(Colors.Red);
		Application.Current.Resources[ProbeBrushKey] = brush;

		var elementHolder = StageAssociation(brush);

		// Explicit all-secondary sweep: this test exercises the global-teardown (unscoped) semantics.
		Application.CleanupAllSecondaryAlcCaches();

		AssertSweepClearedAssociation(brush, elementHolder);
	}

	[TestMethod]
	public void When_CollectibleElementCollectedBeforeCleanup_Then_CleanupFreesExpiredAssociation()
	{
		var brush = new SolidColorBrush(Colors.Green);
		Application.Current.Resources[ProbeBrushKey] = brush;

		// The element is released and collected before the sweep runs, so the association has expired but its slot
		// is still taken: without the sweep freeing it, the next consumer would count as a second parent.
		var weakElement = ReleaseElement(StageAssociation(brush));

		for (var i = 0; i < 10 && weakElement.IsAlive; i++)
		{
			GC.Collect();
			GC.WaitForPendingFinalizers();
		}

		Assert.IsFalse(weakElement.IsAlive, "Pre-condition: the collectible element must be collected before the sweep");
		Assert.IsNull(brush.AssociatedParent, "Pre-condition: the association must resolve to nothing once the element is collected");

		Application.CleanupAllSecondaryAlcCaches();

		var consumer = new Border();
		consumer.Background = brush;
		Assert.AreSame(consumer, brush.AssociatedParent, "ALC teardown cleanup must free the slot of an already-collected parent so the resource re-associates with its next live consumer.");
	}

	[TestMethod]
	public void When_ThemeDictionaryResourceAssociatedToCollectibleElement_Then_CleanupClearsStaleAssociation()
	{
		var brush = new SolidColorBrush(Colors.Blue);

		// Register the probe under every theme key so resolution succeeds (and materializes the
		// active-theme path) regardless of the test environment's active theme.
		var themed = new ResourceDictionary();
		foreach (var themeKey in new[] { "Light", "Dark", "Default" })
		{
			themed.ThemeDictionaries[themeKey] = new ResourceDictionary { [ProbeThemeBrushKey] = brush };
		}

		Application.Current.Resources.MergedDictionaries.Add(themed);
		_stagedThemedDictionary = themed;

		// Resolve through the theme set so the active-theme path is materialized.
		Assert.IsTrue(themed.TryGetValue(ProbeThemeBrushKey, out var resolved), "Pre-condition: the themed brush must resolve");
		Assert.AreSame(brush, resolved, "Pre-condition: resolution must yield the staged brush");

		var elementHolder = StageAssociation(brush);

		// Explicit all-secondary sweep: this test exercises the global-teardown (unscoped) semantics.
		Application.CleanupAllSecondaryAlcCaches();

		AssertSweepClearedAssociation(brush, elementHolder);
	}

	// Staging happens in non-inlined frames so no test-method local keeps the element alive once the
	// holder releases it; until then the holder keeps it alive across the sweep.

	/// <summary>
	/// Makes the collectible element the brush's single InheritanceContext parent and returns a holder
	/// that keeps the element alive.
	/// </summary>
	[MethodImpl(MethodImplOptions.NoInlining)]
	private static object?[] StageAssociation(SolidColorBrush brush)
	{
		var element = CreateCollectibleBorder();

		// First consumption records the element as the brush's InheritanceContext parent.
		element.Background = brush;
		Assert.AreSame(element, brush.AssociatedParent, "Pre-condition: the collectible element must be recorded as the resource's parent");

		return new object?[] { element };
	}

	private static void AssertSweepClearedAssociation(SolidColorBrush brush, object?[] elementHolder)
	{
		// The element is still alive, so only the sweep can have dropped the association.
		Assert.IsNull(brush.AssociatedParent, "ALC teardown cleanup must clear the resource's stale association with the collectible element.");

		// With the stale association gone, the next live consumer is the resource's single parent again
		// instead of a second parent that would disable inheritance for good.
		var consumer = new Border();
		consumer.Background = brush;
		Assert.AreSame(consumer, brush.AssociatedParent, "After ALC teardown cleanup the resource must re-associate with its next live consumer.");

		// Secondary check: once released, nothing retains the collectible element. The release happens in a
		// non-inlined frame so no temp of this frame keeps the element alive during the collection loop.
		var weakElement = ReleaseElement(elementHolder);

		for (var i = 0; i < 10 && weakElement.IsAlive; i++)
		{
			GC.Collect();
			GC.WaitForPendingFinalizers();
		}

		Assert.IsFalse(weakElement.IsAlive, "Nothing must retain the collectible element once it is released.");
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static WeakReference ReleaseElement(object?[] elementHolder)
	{
		var weakElement = new WeakReference(elementHolder[0]);
		elementHolder[0] = null;
		return weakElement;
	}

	/// <summary>
	/// Emits a <see cref="Border"/> subclass into a RunAndCollect (collectible) assembly — the
	/// same collectibility shape as an element type from an unloaded secondary app, without
	/// needing to stage a full secondary application.
	/// </summary>
	[MethodImpl(MethodImplOptions.NoInlining)]
	private static Border CreateCollectibleBorder()
	{
		if (!RuntimeFeature.IsDynamicCodeSupported)
		{
			Assert.Inconclusive("Reflection.Emit (RunAndCollect) is not supported on this target.");
		}

		var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
			new AssemblyName("CollectibleAssociationProbe"),
			AssemblyBuilderAccess.RunAndCollect);
		var typeBuilder = assemblyBuilder
			.DefineDynamicModule("main")
			.DefineType("CollectibleBorder", TypeAttributes.Public, typeof(Border));

		var borderType = typeBuilder.CreateType()!;
		Assert.IsTrue(borderType.Assembly.IsCollectible, "Pre-condition: the probe element type must be collectible");

		return (Border)Activator.CreateInstance(borderType)!;
	}
}
#endif
