#nullable enable

using System;
using System.Runtime.Loader;
using System.Threading;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI;

namespace Uno.UI.Tests.Windows_UI_Xaml;

/// <summary>
/// <see cref="PagePool"/> behaviour: TTL enforcement on dequeue (entries age-checked
/// individually), the drop-all path used when pooling gets disabled, re-enabling after a disable,
/// per-pool (per-Frame) isolation, and the ALC teardown sweep keeping default-ALC page types.
/// The scavenger loop itself is dispatcher-scheduled; its eviction/drop logic is exercised
/// through the <see cref="PagePool.EvictStaleEntries"/> / <see cref="PagePool.DropAllPooledInstances"/> seams.
/// </summary>
[TestClass]
public class Given_PagePool
{
	private bool _previousIsPoolingEnabled;
	private TimeSpan _previousTimeToLive;

	[TestInitialize]
	public void Init()
	{
		UnitTestsApp.App.EnsureApplication();

		_previousIsPoolingEnabled = FeatureConfiguration.Page.IsPoolingEnabled;
		_previousTimeToLive = PagePool.TimeToLive;

		FeatureConfiguration.Page.IsPoolingEnabled = true;
		PagePool.TimeToLive = TimeSpan.FromMinutes(5);
	}

	[TestCleanup]
	public void Cleanup()
	{
		// Restore mutated process-global configuration and leave no pooled instances behind.
		PagePool.DropAllPooledInstances();
		FeatureConfiguration.Page.IsPoolingEnabled = _previousIsPoolingEnabled;
		PagePool.TimeToLive = _previousTimeToLive;
	}

	[TestMethod]
	public void When_Fresh_Entry_Then_Dequeue_Returns_Pooled_Instance()
	{
		var pool = new PagePool();
		var page = new PoolTestPage();

		pool.EnqueuePage(typeof(PoolTestPage), page);

		Assert.AreSame(
			page,
			pool.DequeuePage(typeof(PoolTestPage)),
			"A pooled page within its TTL must be served back (newest first).");
	}

	[TestMethod]
	public void When_Entries_Stale_Then_Dequeue_Creates_Fresh_Instance()
	{
		var pool = new PagePool();
		var stale = new PoolTestPage();

		pool.EnqueuePage(typeof(PoolTestPage), stale);

		// Make every pooled entry stale: the scavenger may not have run (it is between passes),
		// so DequeuePage itself must never serve a page past its TTL.
		PagePool.TimeToLive = TimeSpan.Zero;
		Thread.Sleep(15);

		var dequeued = pool.DequeuePage(typeof(PoolTestPage));

		Assert.AreNotSame(stale, dequeued, "A page past its TTL must never be served; a fresh instance is created instead.");
		Assert.IsNotNull(dequeued, "The fallback must create a fresh instance.");

		// The stale entry was evicted (not served later either).
		PagePool.TimeToLive = TimeSpan.FromMinutes(5);
		Assert.AreNotSame(stale, pool.DequeuePage(typeof(PoolTestPage)), "Stale entries must be evicted on dequeue, not resurrected once TTL grows again.");
	}

	[TestMethod]
	public void When_EvictStaleEntries_Then_Only_Stale_Instances_Counted()
	{
		var pool = new PagePool();
		pool.EnqueuePage(typeof(PoolTestPage), new PoolTestPage());
		pool.EnqueuePage(typeof(PoolTestPage), new PoolTestPage());

		Assert.AreEqual(0, PagePool.EvictStaleEntries(), "Nothing is stale under a large TTL.");

		PagePool.TimeToLive = TimeSpan.Zero;
		Thread.Sleep(15);

		Assert.AreEqual(2, PagePool.EvictStaleEntries(), "Both stale entries must be evicted (N=many).");
		Assert.AreEqual(0, PagePool.EvictStaleEntries(), "The eviction must be idempotent once the pool is empty (N=0).");
	}

	[TestMethod]
	public void When_Pooling_Disabled_Then_DropAll_Empties_Pools_And_ReEnable_Starts_Clean()
	{
		var pool = new PagePool();
		var pooled = new PoolTestPage();
		pool.EnqueuePage(typeof(PoolTestPage), pooled);

		// The scavenger drops all pooled instances when it observes pooling was disabled, so
		// re-enabling cannot serve stale pages past TTL.
		FeatureConfiguration.Page.IsPoolingEnabled = false;
		Assert.AreEqual(1, PagePool.DropAllPooledInstances(), "Disabling pooling must drop every pooled instance (N=1).");
		Assert.AreEqual(0, PagePool.DropAllPooledInstances(), "The drop must be idempotent (N=0).");

		// While disabled: enqueue is a no-op and dequeue always creates fresh instances.
		pool.EnqueuePage(typeof(PoolTestPage), pooled);
		Assert.AreNotSame(pooled, pool.DequeuePage(typeof(PoolTestPage)), "With pooling disabled, every requested instance must be new.");

		// Re-enabled: pooling resumes from an empty pool.
		FeatureConfiguration.Page.IsPoolingEnabled = true;
		Assert.AreNotSame(pooled, pool.DequeuePage(typeof(PoolTestPage)), "Re-enabling must start from an empty pool (the dropped instance must not reappear).");

		var recycled = new PoolTestPage();
		pool.EnqueuePage(typeof(PoolTestPage), recycled);
		Assert.AreSame(recycled, pool.DequeuePage(typeof(PoolTestPage)), "Pooling must work again after re-enabling.");
	}

	[TestMethod]
	public void When_Two_Pools_Then_Pages_Do_Not_Migrate()
	{
		// Each Frame owns its own pool: a page enqueued by one Frame (e.g. in a closed window)
		// must never be served to another Frame under a different XamlRoot.
		var firstPool = new PagePool();
		var secondPool = new PagePool();
		var pooled = new PoolTestPage();

		firstPool.EnqueuePage(typeof(PoolTestPage), pooled);

		Assert.AreNotSame(
			pooled,
			secondPool.DequeuePage(typeof(PoolTestPage)),
			"A pooled page must not migrate across pools (Frames/XamlRoots) — residual state (parent, cache mode, bindings) would ride along.");
		Assert.AreSame(
			pooled,
			firstPool.DequeuePage(typeof(PoolTestPage)),
			"The owning pool must still serve its own pooled page.");
	}

	[TestMethod]
	public void When_ClearNonDefaultAlcEntries_Then_Default_Alc_Entries_Kept()
	{
		// The collectible-key removal half is covered by the WASM runtime ALC pin guard (a Page
		// type cannot be re-loaded into a collectible ALC from the unit-test host); this guards
		// the "keep" half — the sweep must not evict framework/host page types.
		var pool = new PagePool();
		var pooled = new PoolTestPage();
		pool.EnqueuePage(typeof(PoolTestPage), pooled);

		PagePool.ClearNonDefaultAlcEntries();

		Assert.AreSame(
			pooled,
			pool.DequeuePage(typeof(PoolTestPage)),
			"The ALC sweep must keep default-ALC page types; it only drops collectible-ALC keys.");
	}

	[TestMethod]
	public void When_ClearNonDefaultAlcEntries_Then_Collectible_Key_Dropped_And_Default_Kept()
	{
		// The DROP half of the sweep: a page pooled under a COLLECTIBLE-ALC page type must be
		// removed (it would otherwise pin the previewed app's context after unload), while a page
		// pooled under a default-ALC (framework/host) type must survive. The collectible key stands
		// in for a previewed-app page type by loading this test assembly into a collectible ALC.
		var collectibleAlc = new AssemblyLoadContext("Given_PagePool.collectible", isCollectible: true);
		try
		{
			var collectibleKey = collectibleAlc
				.LoadFromAssemblyPath(typeof(PoolTestPage).Assembly.Location)
				.GetType(typeof(PoolTestPage).FullName!, throwOnError: true)!;

			Assert.IsTrue(collectibleKey.IsCollectible, "Pre-condition: the stand-in key must belong to the collectible ALC.");

			var pool = new PagePool();
			// EnqueuePage keys purely by the supplied Type, so a default-ALC PoolTestPage instance can
			// stand in under the collectible key — the sweep discriminates on the KEY, which is what pins the ALC.
			pool.EnqueuePage(collectibleKey, new PoolTestPage());
			pool.EnqueuePage(typeof(PoolTestPage), new PoolTestPage());

			Assert.AreEqual(1, pool.GetPooledCount(collectibleKey), "Pre-condition: the collectible-keyed entry must be pooled.");
			Assert.AreEqual(1, pool.GetPooledCount(typeof(PoolTestPage)), "Pre-condition: the default-keyed entry must be pooled.");

			PagePool.ClearNonDefaultAlcEntries();

			Assert.AreEqual(
				0,
				pool.GetPooledCount(collectibleKey),
				"The sweep must drop pooled entries keyed by a collectible-ALC page type; otherwise the pooled instance pins the previewed app's context after unload.");
			Assert.AreEqual(
				1,
				pool.GetPooledCount(typeof(PoolTestPage)),
				"The sweep must keep default-ALC (framework/host) page types; it only drops collectible-ALC keys.");
		}
		finally
		{
			PagePool.DropAllPooledInstances();
			collectibleAlc.Unload();
		}
	}

	public class PoolTestPage : Page
	{
	}
}
