using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uno.Extensions;
using Uno.UI;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;

namespace Windows.UI.Xaml
{
	/// <summary>
	/// Provides an instance pool for <see cref="Page"/>s. Pooling is enabled when <see cref="Uno.UI.FeatureConfiguration.Page.IsPoolingEnabled"/> is set to true.
	/// </summary>
	/// <remarks>Enabling page pooling improves performance when using <see cref="Frame"/> navigation.</remarks>
	public class PagePool
	{
		private readonly Stopwatch _watch = new Stopwatch();
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
			_watch.Start();

#if !IS_UNIT_TESTS
			_ = CoreDispatcher.Main.RunIdleAsync(Scavenger);
#endif
		}

		private async void Scavenger(IdleDispatchedHandlerArgs e)
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

			_ = CoreDispatcher.Main.RunIdleAsync(Scavenger);
		}

		internal Page DequeuePage(Type pageType)
		{
			if (!FeatureConfiguration.Page.IsPoolingEnabled)
			{
				return Frame.CreatePageInstance(pageType);
			}

			var list = _pooledInstances.UnoGetValueOrDefault(pageType);

			if (list == null || list.Count == 0)
			{
				return Frame.CreatePageInstance(pageType);
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
			var list = _pooledInstances.FindOrCreate(pageType, () => new List<PagePoolEntry>());

			FrameworkTemplatePool.PropagateOnTemplateReused(pageInstance);

			list.Add(new PagePoolEntry(_watch.Elapsed, pageInstance));
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
