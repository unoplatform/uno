#nullable enable

using System;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;
using DirectUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.Tests.Windows_UI_Xaml;

/// <summary>
/// <see cref="MetadataAPI.TryGetDependencyPropertyByName"/> resolves a DependencyProperty from a
/// <c>{Name}Property</c> static member by reflection; it backs <c>PropertyAccess.ResolvePropertyName</c>
/// (PropertyPath binding resolution), so the same (Type, name) pair is queried repeatedly.
/// These tests pin the cache contract: positive results are resolved once, a null read from an
/// EXISTING member is never cached (its static initializer may not have run yet), lookups are safe
/// from several threads, and the ALC teardown sweep evicts collectible-context keys.
/// </summary>
[TestClass]
public class Given_MetadataAPI_DependencyPropertyCache
{
	[TestMethod]
	public void When_Resolved_Twice_Then_Member_Read_Once()
	{
		var type = typeof(MetadataApiRepeatProbe);

		var first = MetadataAPI.TryGetDependencyPropertyByName(type, "Counted");

		Assert.AreSame(TextBlock.TextProperty, first, "Pre-condition: the probe's static member must resolve.");
		Assert.AreEqual(1, MetadataApiRepeatProbe.ReadCount, "Pre-condition: the first lookup must read the member exactly once.");

		for (var i = 0; i < 10; i++)
		{
			Assert.AreSame(first, MetadataAPI.TryGetDependencyPropertyByName(type, "Counted"));
		}

		Assert.AreEqual(
			1,
			MetadataApiRepeatProbe.ReadCount,
			"Repeated lookups must be served from the cache — the member must not be re-read, and neither must the GetProperty/GetField reflection that precedes it.");
	}

	[TestMethod]
	public void When_Type_Has_No_Matching_Member_Then_Null_Is_Stable()
	{
		var type = typeof(MetadataApiRepeatProbe);

		// A type's member shape is fixed once loaded, so this miss is cached. What must hold either
		// way is that the answer never changes.
		Assert.IsNull(MetadataAPI.TryGetDependencyPropertyByName(type, "NoSuchThing"));
		Assert.IsNull(MetadataAPI.TryGetDependencyPropertyByName(type, "NoSuchThing"));
	}

	[TestMethod]
	public void When_Existing_Member_Reads_Null_Then_Miss_Not_Cached()
	{
		var type = typeof(MetadataApiLateInitProbe);

		Assert.IsNull(MetadataApiLateInitProbe.LateProperty, "Pre-condition: the member must start out unset.");
		Assert.IsNull(MetadataAPI.TryGetDependencyPropertyByName(type, "Late"));

		MetadataApiLateInitProbe.LateProperty = TextBlock.TextProperty;

		Assert.AreSame(
			TextBlock.TextProperty,
			MetadataAPI.TryGetDependencyPropertyByName(type, "Late"),
			"A null read from an EXISTING member only means its static initializer has not run yet; caching it would poison the entry for the rest of the process.");
	}

	[TestMethod]
	public void When_Resolved_Concurrently_Then_Cache_Stays_Consistent()
	{
		var type = typeof(MetadataApiConcurrencyProbe);

		var cached = MetadataAPI.TryGetDependencyPropertyByName(type, "Counted");
		Assert.AreSame(TextBlock.TextProperty, cached, "Pre-condition: the probe's static member must resolve.");

		var readsAfterWarmup = MetadataApiConcurrencyProbe.ReadCount;

		// Reads hit the warmed entry while the misses insert 64 new keys from every thread at once:
		// a non-concurrent dictionary corrupts its buckets (lost entries, or a spin in TryGetValue)
		// under exactly this mix.
		Parallel.For(0, 4096, i =>
		{
			Assert.AreSame(cached, MetadataAPI.TryGetDependencyPropertyByName(type, "Counted"));
			Assert.IsNull(MetadataAPI.TryGetDependencyPropertyByName(type, "Absent" + (i % 64).ToString()));
		});

		Assert.AreEqual(
			readsAfterWarmup,
			MetadataApiConcurrencyProbe.ReadCount,
			"Concurrent readers must all hit the already-cached entry.");
		Assert.AreSame(cached, MetadataAPI.TryGetDependencyPropertyByName(type, "Counted"));
	}

	[TestMethod]
	public void When_ClearCachesForNonDefaultAlc_Then_Collectible_Entry_Evicted_And_Default_Kept()
	{
		// Populating the cache pins the key Type, so a previewed app's element types would keep its
		// collectible AssemblyLoadContext alive after unload without this sweep.
		var defaultType = typeof(MetadataApiAlcProbe);

		Assert.AreSame(TextBlock.TextProperty, MetadataAPI.TryGetDependencyPropertyByName(defaultType, "Counted"));
		var defaultReadsAfterWarmup = MetadataApiAlcProbe.ReadCount;

		var collectibleAlc = new AssemblyLoadContext("Given_MetadataAPI_DependencyPropertyCache.collectible", isCollectible: true);
		try
		{
			var collectibleType = collectibleAlc
				.LoadFromAssemblyPath(defaultType.Assembly.Location)
				.GetType(defaultType.FullName!, throwOnError: true)!;

			Assert.IsTrue(collectibleType.IsCollectible, "Pre-condition: the probe copy must belong to the collectible ALC.");

			Assert.AreSame(TextBlock.TextProperty, MetadataAPI.TryGetDependencyPropertyByName(collectibleType, "Counted"));
			Assert.AreEqual(1, ReadCountOf(collectibleType), "Pre-condition: the first lookup must read the member once.");

			MetadataAPI.TryGetDependencyPropertyByName(collectibleType, "Counted");
			Assert.AreEqual(1, ReadCountOf(collectibleType), "Pre-condition: the collectible entry must be cached like any other.");

			MetadataAPI.ClearCachesForNonDefaultAlc();

			MetadataAPI.TryGetDependencyPropertyByName(collectibleType, "Counted");
			Assert.AreEqual(
				2,
				ReadCountOf(collectibleType),
				"The teardown sweep must evict the collectible-ALC key — a retained entry pins the previewed app's context after unload.");

			MetadataAPI.TryGetDependencyPropertyByName(defaultType, "Counted");
			Assert.AreEqual(
				defaultReadsAfterWarmup,
				MetadataApiAlcProbe.ReadCount,
				"The sweep must keep default-ALC (framework/host) keys — they are not what pins a dying context.");
		}
		finally
		{
			collectibleAlc.Unload();
		}

		static int ReadCountOf(Type probeType)
			=> (int)probeType.GetField(nameof(MetadataApiAlcProbe.ReadCount), BindingFlags.Public | BindingFlags.Static)!.GetValue(null)!;
	}
}

/// <summary>
/// Stands in for a WinUI-style <c>public static DependencyProperty FooProperty { get; }</c>, counting
/// how many times the member is actually read so cache hits are observable. Returning an existing
/// framework property (rather than registering one) keeps the probe free of global registry side effects,
/// including when a copy of it is loaded into a collectible ALC.
/// </summary>
public static class MetadataApiRepeatProbe
{
	public static int ReadCount;

	public static DependencyProperty CountedProperty
	{
		get
		{
			ReadCount++;
			return TextBlock.TextProperty;
		}
	}
}

public static class MetadataApiConcurrencyProbe
{
	public static int ReadCount;

	public static DependencyProperty CountedProperty
	{
		get
		{
			Interlocked.Increment(ref ReadCount);
			return TextBlock.TextProperty;
		}
	}
}

public static class MetadataApiAlcProbe
{
	public static int ReadCount;

	public static DependencyProperty CountedProperty
	{
		get
		{
			ReadCount++;
			return TextBlock.TextProperty;
		}
	}
}

/// <summary>
/// A <c>{Name}Property</c> member that exists but is only assigned later, standing in for a type
/// whose static initializer has not run at first lookup.
/// </summary>
public static class MetadataApiLateInitProbe
{
	public static DependencyProperty? LateProperty;
}
