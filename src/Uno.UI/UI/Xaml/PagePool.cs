using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI;
using Windows.UI.Core;
using Microsoft.UI.Xaml.Controls;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.UI.Xaml
{
	/// <summary>
	/// Provides an instance pool for <see cref="Page"/>s. Pooling is enabled when <see cref="Uno.UI.FeatureConfiguration.Page.IsPoolingEnabled"/> is set to true.
	/// </summary>
	/// <remarks>
	/// <para>Enabling page pooling improves performance when using <see cref="Frame"/> navigation.</para>
	/// <para>Each <see cref="Frame"/> owns its own pool, so a pooled page is never served to a
	/// different <see cref="Frame"/> (or a different <c>XamlRoot</c>/window). Pools register into a
	/// process-wide WEAK registry, which lets the shared scavenger and the ALC teardown sweep reach
	/// every live pool without rooting any of them — a pool dies with its Frame.</para>
	/// <para>Threading: pool contents are guarded by a per-pool lock — navigation, the scavenger and
	/// the ALC teardown sweep can each touch a pool from different call paths.</para>
	/// </remarks>
	[UnconditionalSuppressMessage("Trimming", "IL2057", Justification = "Types manipulated here have been marked earlier")]
	[UnconditionalSuppressMessage("Trimming", "IL2067", Justification = "Types manipulated here have been marked earlier")]
	public class PagePool
	{
		/// <summary>
		/// Shared monotonic time base for entry ages, so TTL comparisons are consistent across pools.
		/// </summary>
		private static readonly Stopwatch _watch = Stopwatch.StartNew();

		/// <summary>
		/// Weak registry of every pool (one per <see cref="Frame"/>). Weak so the registry — and the
		/// shared scavenger iterating it — never roots a pool past its Frame's lifetime; dead
		/// registrations are pruned opportunistically. Guarded by <see cref="_poolsGate"/>.
		/// </summary>
		private static readonly List<WeakReference<PagePool>> _pools = new();
		private static readonly object _poolsGate = new();
		private static bool _scavengerStarted; // guarded by _poolsGate

		/// <summary>Guards <see cref="_pooledInstances"/> (see the threading remarks on the class).</summary>
		private readonly object _gate = new();
		private readonly Dictionary<Type, List<PagePoolEntry>> _pooledInstances = new Dictionary<Type, List<PagePoolEntry>>();

		/// <summary>
		/// Determines the duration for which a pooled page stays alive.
		/// </summary>
		public static TimeSpan TimeToLive { get; set; } = TimeSpan.FromMinutes(1);

		/// <summary>
		/// Determines if the pooling is enabled. If false, all requested instances are new.
		/// </summary>
		public static bool IsPoolingEnabled { get; set; } = true;

		internal PagePool()
		{
			lock (_poolsGate)
			{
				// Opportunistic pruning keeps the registry proportional to LIVE pools even if
				// Frames churn heavily between scavenger passes.
				_pools.RemoveAll(static reference => !reference.TryGetTarget(out _));
				_pools.Add(new WeakReference<PagePool>(this));
			}
		}

		/// <summary>
		/// Starts the periodic scavenger that evicts pooled pages older than <see cref="TimeToLive"/>
		/// across every live pool. The scavenger only runs while pooling is enabled — a disabled pool
		/// never enqueues anything, so the eternal idle loop is pure overhead. It iterates the weak
		/// registry, so it never keeps a pool (or its Frame) alive. Idempotent: the loop is scheduled
		/// at most once.
		/// </summary>
		private static void EnsureScavengerStarted()
		{
#if !IS_UNIT_TESTS
			lock (_poolsGate)
			{
				if (_scavengerStarted || !FeatureConfiguration.Page.IsPoolingEnabled)
				{
					return;
				}

				_scavengerStarted = true;
			}

			_ = CoreDispatcher.Main.RunIdleAsync(Scavenger);
#endif
		}

		private static async void Scavenger(IdleDispatchedHandlerArgs e)
		{
			try
			{
				var removedInstancesCount = EvictStaleEntries();

				if (removedInstancesCount > 0)
				{
					CollectOnMobileTargets();
				}

				await Task.Delay(TimeSpan.FromSeconds(30));

#if !IS_UNIT_TESTS
				// Honor the "only runs while pooling is enabled" contract: if pooling was disabled
				// after the loop started, stop rescheduling (an idle loop over a disabled pool is
				// pure overhead) and allow EnsureScavengerStarted to spin it up again if pooling is
				// later re-enabled and something is enqueued.
				if (!FeatureConfiguration.Page.IsPoolingEnabled)
				{
					// Pooling disabled: drop all pooled instances so re-enabling cannot serve stale
					// pages past TTL — with the scavenger stopped, nothing else would ever evict them.
					var droppedInstancesCount = DropAllPooledInstances();
					if (droppedInstancesCount > 0)
					{
						CollectOnMobileTargets();
					}

					lock (_poolsGate)
					{
						_scavengerStarted = false;
					}

					return;
				}

				_ = CoreDispatcher.Main.RunIdleAsync(Scavenger);
#endif
			}
			catch (Exception ex)
			{
				// async void: an unhandled exception here would crash the runtime (fatal on WASM,
				// where this runs in a web worker). Best-effort scavenging must never do that.
				if (typeof(PagePool).Log().IsEnabled(LogLevel.Warning))
				{
					typeof(PagePool).Log().Warn("PagePool scavenger iteration failed", ex);
				}

				// A transient failure interrupted the loop; clear the guard so the next EnqueuePage
				// can restart the scavenger via EnsureScavengerStarted. Leaving it set would
				// permanently disable eviction and let the pools grow unbounded.
				lock (_poolsGate)
				{
					_scavengerStarted = false;
				}
			}
		}

		/// <summary>
		/// Forcing a collection is only needed under iOS and Android for the GC to pick up the
		/// orphan instances that were just released; other targets rely on natural collection.
		/// On WASM in particular a forced blocking collect on the UI thread is a visible stall.
		/// </summary>
		private static void CollectOnMobileTargets()
		{
			if (OperatingSystem.IsAndroid() || OperatingSystem.IsIOS())
			{
				GC.Collect();
			}
		}

		/// <summary>
		/// Evicts, from every live pool, the entries older than <see cref="TimeToLive"/>.
		/// Internal (rather than private) as a test seam: the scavenger loop itself is
		/// dispatcher-scheduled and disabled under IS_UNIT_TESTS.
		/// </summary>
		/// <returns>The number of evicted page instances.</returns>
		internal static int EvictStaleEntries()
		{
			var removed = 0;
			foreach (var pool in GetLivePools())
			{
				removed += pool.EvictStaleEntriesCore();
			}

			return removed;
		}

		private int EvictStaleEntriesCore()
		{
			lock (_gate)
			{
				var now = _watch.Elapsed;
				var removed = 0;
				foreach (var list in _pooledInstances.Values)
				{
					removed += list.RemoveAll(t => now - t.CreationTime > TimeToLive);
				}

				return removed;
			}
		}

		/// <summary>
		/// Drops every pooled instance of every live pool (used when pooling gets disabled).
		/// Internal (rather than private) as a test seam: the scavenger loop itself is
		/// dispatcher-scheduled and disabled under IS_UNIT_TESTS.
		/// </summary>
		/// <returns>The number of dropped page instances.</returns>
		internal static int DropAllPooledInstances()
		{
			var dropped = 0;
			foreach (var pool in GetLivePools())
			{
				lock (pool._gate)
				{
					foreach (var list in pool._pooledInstances.Values)
					{
						dropped += list.Count;
					}

					pool._pooledInstances.Clear();
				}
			}

			return dropped;
		}

		private static List<PagePool> GetLivePools()
		{
			lock (_poolsGate)
			{
				var live = new List<PagePool>(_pools.Count);
				for (var i = _pools.Count - 1; i >= 0; i--)
				{
					if (_pools[i].TryGetTarget(out var pool))
					{
						live.Add(pool);
					}
					else
					{
						_pools.RemoveAt(i);
					}
				}

				return live;
			}
		}

		internal Page DequeuePage(Type pageType)
		{
			if (!FeatureConfiguration.Page.IsPoolingEnabled)
			{
				return Frame.CreatePageInstance(pageType) as Page;
			}

			Page pooled = null;
			lock (_gate)
			{
				if (_pooledInstances.TryGetValue(pageType, out var list) && list.Count > 0)
				{
					// Never serve a page past its TTL (the scavenger may not have run yet — e.g. it
					// is between passes, or pooling was toggled). Entries are age-checked
					// INDIVIDUALLY so only genuinely stale instances are dropped, never a
					// still-fresh one that happens to share the list with a stale entry.
					var now = _watch.Elapsed;
					list.RemoveAll(t => now - t.CreationTime > TimeToLive);

					if (list.Count > 0)
					{
						// Entries are appended in creation order, so serve the newest.
						var position = list.Count - 1;
						pooled = list[position].PageInstance;
						list.RemoveAt(position);
					}
				}
			}

			// The fallback instantiation runs app code (the page constructor) — keep it out of the lock.
			return pooled ?? Frame.CreatePageInstance(pageType) as Page;
		}

		internal void EnqueuePage(Type pageType, Page pageInstance)
		{
			if (!FeatureConfiguration.Page.IsPoolingEnabled)
			{
				return;
			}

			// Only spin up the periodic scavenger once there is actually something to scavenge.
			EnsureScavengerStarted();

			// Template-reuse propagation walks the page's subtree — keep it out of the lock.
			FrameworkTemplatePool.PropagateOnTemplateReused(pageInstance);

			lock (_gate)
			{
				var list = _pooledInstances.FindOrCreate(pageType, () => new List<PagePoolEntry>());
				list.Add(new PagePoolEntry(_watch.Elapsed, pageInstance));
			}
		}

		/// <summary>
		/// Removes, from every live pool, the pooled pages whose page <see cref="Type"/> belongs to a
		/// non-default (collectible) <see cref="System.Runtime.Loader.AssemblyLoadContext"/>. A downstream host that loads
		/// previewed apps into their own collectible AssemblyLoadContexts navigates the app's pages;
		/// pooled instances (and the <see cref="Type"/> keys) then keep the app's context alive after
		/// unload. Called from the ALC cleanup hook.
		/// </summary>
		internal static void ClearNonDefaultAlcEntries()
		{
			var removed = 0;
			foreach (var pool in GetLivePools())
			{
				lock (pool._gate)
				{
					removed += Uno.UI.Helpers.AlcCacheSweep.RemoveNonDefaultAlcEntries(pool._pooledInstances);
				}
			}

			if (removed > 0 && typeof(PagePool).Log().IsEnabled(LogLevel.Debug))
			{
				typeof(PagePool).Log().Debug($"[ALC-CLEANUP] PagePool: removed {removed} non-default-ALC page type(s) from the live pools.");
			}
		}

		/// <summary>
		/// Test seam: the number of pooled entries currently held for <paramref name="pageType"/> in
		/// THIS pool. Lets the ALC sweep's collectible-key drop be asserted without depending on
		/// <see cref="DequeuePage"/> instantiating a (collectible) page type.
		/// </summary>
		internal int GetPooledCount(Type pageType)
		{
			lock (_gate)
			{
				return _pooledInstances.TryGetValue(pageType, out var list) ? list.Count : 0;
			}
		}

		private class PagePoolEntry
		{
			public PagePoolEntry(TimeSpan creationTime, Page pageInstance)
			{
				CreationTime = creationTime;
				PageInstance = pageInstance;
			}

			public TimeSpan CreationTime { get; private set; }

			public Page PageInstance { get; private set; }
		}
	}
}
