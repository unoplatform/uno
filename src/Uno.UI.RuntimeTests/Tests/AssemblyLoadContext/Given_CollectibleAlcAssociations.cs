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
using Windows.UI;

namespace Uno.UI.RuntimeTests.Tests.AssemblyLoadContext;

/// <summary>
/// A shared (host-lifetime) resource consumed by a secondary-ALC element records that element as
/// its InheritanceContext parent (<c>DependencyObjectStore._associatedParent</c>). Nothing
/// un-associates it when the element's AssemblyLoadContext unloads, so the host resource pins the
/// collectible ALC forever. These tests stage that association with an element whose type lives in
/// a collectible (RunAndCollect) assembly and assert that
/// <see cref="Application.CleanupNonDefaultAlcCaches"/> releases it.
/// </summary>
[TestClass]
[RunsOnUIThread]
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
	public void When_HostResourceAssociatedToCollectibleElement_Then_CleanupReleasesAssociation()
	{
		var weakElement = StageHostResourceAssociation();

		Application.CleanupNonDefaultAlcCaches();

		for (var i = 0; i < 10 && weakElement.IsAlive; i++)
		{
			GC.Collect();
			GC.WaitForPendingFinalizers();
		}

		Assert.IsFalse(
			weakElement.IsAlive,
			"The host-lifetime brush's InheritanceContext association must be cleared during ALC " +
			"teardown cleanup; otherwise the brush pins the collectible element (and its entire " +
			"AssemblyLoadContext) for the host resource's lifetime.");
	}

	[TestMethod]
	public void When_ThemeDictionaryResourceAssociatedToCollectibleElement_Then_CleanupReleasesAssociation()
	{
		var weakElement = StageThemeDictionaryAssociation();

		Application.CleanupNonDefaultAlcCaches();

		for (var i = 0; i < 10 && weakElement.IsAlive; i++)
		{
			GC.Collect();
			GC.WaitForPendingFinalizers();
		}

		Assert.IsFalse(
			weakElement.IsAlive,
			"Associations recorded on resources inside (active) theme dictionaries must also be " +
			"cleared during ALC teardown cleanup.");
	}

	// Staging happens in non-inlined frames so no test-method local keeps the element alive when
	// the collection assertion runs.

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static WeakReference StageHostResourceAssociation()
	{
		var brush = new SolidColorBrush(Colors.Red);
		Application.Current.Resources[ProbeBrushKey] = brush;

		var element = CreateCollectibleBorder();

		// First consumption records the element as the brush's InheritanceContext parent.
		element.Background = brush;

		return new WeakReference(element);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private WeakReference StageThemeDictionaryAssociation()
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

		var element = CreateCollectibleBorder();
		element.Background = brush;

		return new WeakReference(element);
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
