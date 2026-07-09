using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Loader;
using System.Text;
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
	/// <remarks>Enabling page pooling improves performance when using <see cref="Frame"/> navigation.</remarks>
	[UnconditionalSuppressMessage("Trimming", "IL2057", Justification = "Types manipulated here have been marked earlier")]
	[UnconditionalSuppressMessage("Trimming", "IL2067", Justification = "Types manipulated here have been marked earlier")]
	public class PagePool
	{
		private readonly Stopwatch _watch = new Stopwatch();
		private readonly Dictionary<Type, List<PagePoolEntry>> _pooledInstances = new Dictionary<Type, List<PagePoolEntry>>();
		private bool _scavengerStarted;

		/// <summary>
		/// Process-wide pool. Previously every <see cref="Frame"/> allocated its own <see cref="PagePool"/>
		/// into a shared static field: the last Frame won, and every earlier pool was orphaned yet kept
		/// alive forever by the 30s scavenger loop it had scheduled on the dispatcher. A single lazily
		/// created instance removes that leak; the scavenger is scheduled at most once, and only when
		/// pooling is enabled.
		/// </summary>
		internal static PagePool Instance { get; } = new PagePool();

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
			_watch.Start();
		}

		/// <summary>
		/// Starts the periodic scavenger that evicts pooled pages older than <see cref="TimeToLive"/>.
		/// The scavenger only runs while pooling is enabled — a disabled pool never enqueues anything,
		/// so the eternal idle loop is pure overhead (and, historically, a per-orphaned-pool leak).
		/// Idempotent: the loop is scheduled at most once.
		/// </summary>
		private void EnsureScavengerStarted()
		{
#if !IS_UNIT_TESTS
			if (_scavengerStarted || !FeatureConfiguration.Page.IsPoolingEnabled)
			{
				return;
			}

			_scavengerStarted = true;
			_ = CoreDispatcher.Main.RunIdleAsync(Scavenger);
#endif
		}

		private async void Scavenger(IdleDispatchedHandlerArgs e)
		{
			try
			{
				var now = _watch.Elapsed;
				var removedInstancesCount = 0;

				foreach (var list in _pooledInstances.Values)
				{
					removedInstancesCount += list.RemoveAll(t => now - t.CreationTime > TimeToLive);
				}

				if (removedInstancesCount > 0)
				{
					// Under iOS and Android, we need to force the collection for the GC
					// to pick up the orphan instances that we've just released.

					GC.Collect();
				}

				await Task.Delay(TimeSpan.FromSeconds(30));

#if !IS_UNIT_TESTS
				_ = CoreDispatcher.Main.RunIdleAsync(Scavenger);
#endif
			}
			catch (Exception ex)
			{
				// async void: an unhandled exception here would crash the runtime (fatal on WASM,
				// where this runs in a web worker). Best-effort scavenging must never do that.
				if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Warning))
				{
					this.Log().Warn("PagePool scavenger iteration failed", ex);
				}
			}
		}

		internal Page DequeuePage(Type pageType)
		{
			if (!FeatureConfiguration.Page.IsPoolingEnabled)
			{
				return Frame.CreatePageInstance(pageType) as Page;
			}

			var list = _pooledInstances.UnoGetValueOrDefault(pageType);

			if (list == null || list.Count == 0)
			{
				return Frame.CreatePageInstance(pageType) as Page;
			}
			else
			{
				var position = list.Count - 1;
				var instance = list[position].PageInstance;
				list.RemoveAt(position);
				return instance;
			}
		}

		internal void EnqueuePage(Type pageType, Page pageInstance)
		{
			if (!FeatureConfiguration.Page.IsPoolingEnabled)
			{
				return;
			}

			// Only spin up the periodic scavenger once there is actually something to scavenge.
			EnsureScavengerStarted();

			var list = _pooledInstances.FindOrCreate(pageType, () => new List<PagePoolEntry>());

			FrameworkTemplatePool.PropagateOnTemplateReused(pageInstance);

			list.Add(new PagePoolEntry(_watch.Elapsed, pageInstance));
		}

		/// <summary>
		/// Removes pooled pages whose page <see cref="Type"/> belongs to a non-default (collectible)
		/// <see cref="AssemblyLoadContext"/>. A downstream host that loads previewed apps into their own
		/// collectible AssemblyLoadContexts navigates the app's pages; pooled instances (and the
		/// <see cref="Type"/> keys) then keep the app's context alive after unload. Called from the ALC
		/// cleanup hook.
		/// </summary>
		internal void ClearNonDefaultAlcEntries()
		{
			var defaultAlc = AssemblyLoadContext.Default;
			List<Type> keysToRemove = null;

			foreach (var key in _pooledInstances.Keys)
			{
				if (key.IsCollectible)
				{
					(keysToRemove ??= new List<Type>()).Add(key);
					continue;
				}

				var alc = AssemblyLoadContext.GetLoadContext(key.Assembly);
				if (alc is not null && alc != defaultAlc)
				{
					(keysToRemove ??= new List<Type>()).Add(key);
				}
			}

			if (keysToRemove is null)
			{
				return;
			}

			foreach (var key in keysToRemove)
			{
				_pooledInstances.Remove(key);
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
