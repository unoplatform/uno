#nullable enable

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Microsoft.UI.Xaml.Controls;

using Uno.Buffers;
using Uno.Diagnostics.Eventing;
using Uno.Extensions;
using Uno.Extensions.Specialized;
using Uno.Foundation.Logging;
using Uno.UI;
using Uno.UI.Dispatching;

#if __ANDROID__
using Uno.UI.Controls;
using View = Android.Views.View;
using ViewGroup = Android.Views.ViewGroup;
#elif __IOS__
using UIKit;
using View = UIKit.UIView;
using ViewGroup = UIKit.UIView;
#elif __MACOS__
using AppKit;
using View = AppKit.NSView;
using ViewGroup = AppKit.NSView;
#else
using View = Microsoft.UI.Xaml.UIElement;
using ViewGroup = Microsoft.UI.Xaml.UIElement;
#endif

namespace Microsoft.UI.Xaml
{
	/// <summary>
	/// Provides an instance pool for FrameworkTemplates, when <see cref="FrameworkTemplate.LoadContentCached"/> is called.
	/// </summary>
	/// <remarks>
	/// The pooling is particularly important on iOS and Android where the memory management is less than ideal because of the
	/// pinning of native instances, and the inability of the GC to determine that two instances refer to each other via a
	/// native storage (Subviews for instance), and reclaim their memory properly.
	///
	/// The pooling is also important because creating a control is particularly expensive on Android and iOS.
	///
	/// This Xaml implementation relies on unloaded controls to release their templated parent by setting it to null, which always resets
	/// content template of a ContentControl, as well as the template of a Control. This forces recycled controls to re-create their
	/// templates even if it was previously the same. This can make lists particularly jittery.
	/// This class allows for templates to be reused, based on the fact that controls created via a FrameworkTemplate that lose their
	/// DependencyObject.Parent value are considered orhpans. Those instances can then later on be reused.
	///
	///	This behavior is not following windows' implementation, as this requires a control to be stateless. This is pretty easy to do when controls
	///	are strictly databound, but not if the control is using stateful code-behind. This is why this behavior can be disabled via <see cref="IsPoolingEnabled"/>
	///	if the pooling interferes with the normal behavior of a control.
	/// </remarks>
	public partial class FrameworkTemplatePool
	{
		private readonly Dictionary<FrameworkTemplate, List<TemplateEntry>> _availableInstances =
			new(FrameworkTemplate.FrameworkTemplateEqualityComparer.Default);

		private readonly Stack<(FrameworkTemplate, View)> _instancesToRecycle = new();

		private IFrameworkTemplatePoolPlatformProvider _platformProvider = new FrameworkTemplatePoolDefaultPlatformProvider();

		private const int RecycleBatchSize = 32;

		private FrameworkTemplatePool()
		{
#if !IS_UNIT_TESTS
			_platformProvider.Schedule(Scavenger);
#endif
		}

		private bool CanUsePool()
		{
			if (_platformProvider.CanUseMemoryManager)
			{
				return ((float)_platformProvider.AppMemoryUsage / _platformProvider.AppMemoryUsageLimit) < HighMemoryThreshold;
			}
			else
			{
				return true;
			}
		}

		internal View? DequeueTemplate(FrameworkTemplate template)
		{
			var instance = DequeueTemplateOrDefault(template);

			if (instance == null)
			{
				if (_trace.IsEnabled)
				{
					_trace.WriteEventActivity(TraceProvider.CreateTemplate, EventOpcode.Send, new[] { ((Func<View>)template).Method.DeclaringType?.ToString() });
				}

				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().Debug($"Creating new template, id={GetTemplateDebugId(template)} IsPoolingEnabled:{IsPoolingEnabled}");
				}

				instance = Unsafe.As<IFrameworkTemplateInternal>(template).LoadContent();

				if (IsPoolingEnabled && instance is IFrameworkElement)
				{
					DependencyObjectExtensions.RegisterParentChangedCallback((DependencyObject)instance, template, OnParentChanged);
				}
			}
			else
			{
				if (_trace.IsEnabled)
				{
					_trace.WriteEventActivity(TraceProvider.ReuseTemplate, EventOpcode.Send, new[] { instance.GetType().ToString() });
				}

				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().Debug($"Reuse template, id={GetTemplateDebugId(template)}");
				}
			}

			// We don't track empty templates.
			if (IsPoolingEnabled && instance != null)
			{
				InstanceTracker.Add(instance);
			}

			return instance;
		}

		private View? DequeueTemplateOrDefault(FrameworkTemplate template)
		{
			View? instance = null;

			if (_availableInstances.TryGetValue(template, out var pool))
			{
				var index = pool.Count - 1;

				if (index >= 0)
				{
					instance = pool[index].View;

					pool.RemoveAt(index);
				}

				if (pool.Count == 0)
				{
					_availableInstances.Remove(template);
				}
			}

			return instance;
		}

		internal void EnqueueTemplate(FrameworkTemplate template, View view, bool cleanup = false)
		{
			if (IsPoolingEnabled && CanUsePool() && view != null)
			{
				InstanceTracker.TryRemove(view);

				RecycleTemplate(template, view, cleanup);
			}
			else
			{
				this.Log().Debug("Not caching template, pooling is disabled or memory threshold is reached.");
			}
		}

		private void EnqueueTemplateToPool(FrameworkTemplate template, View view)
		{
			ref var pool = ref CollectionsMarshal.GetValueRefOrAddDefault(_availableInstances, template, out var exists);

			if (exists)
			{
				pool!.Add(new(_platformProvider.Now, view));
			}
			else
			{
				pool = new() { new(_platformProvider.Now, view) };
			}
		}

		internal int GetPooledTemplateCount()
		{
			var count = 0;

			foreach (var kvp in _availableInstances)
			{
				count += kvp.Value.Count;
			}

			return count;
		}

		private string GetTemplateDebugId(FrameworkTemplate template)
		{
			if (template._viewFactory is { } func)
			{
				return $"{func.Method.DeclaringType}.{func.Method.Name}";
			}

			return "Unknown";
		}

		private void OnParentChanged(object instance, object? template, DependencyObjectParentChangedEventArgs? args)
		{
			var newParent = args?.NewParent;
			var oldParent = args?.PreviousParent;

			if (newParent != null)
			{
				InstanceTracker.TryRegisterForRecycling(Unsafe.As<FrameworkTemplate>(template)!, Unsafe.As<View>(instance), newParent, oldParent);
			}
			else
			{
				InstanceTracker.TryCancelRecycling(Unsafe.As<View>(instance), oldParent);
			}
		}

		internal static void PropagateOnTemplateReused(object instance)
		{
			// If DataContext is not null, it means it has been explicitly set (not inherited). Resetting the view could push an invalid value through 2-way bindings in this case.
			if (instance is IFrameworkTemplatePoolAware templateAwareElement && Unsafe.As<IFrameworkElement>(instance)!.DataContext == null)
			{
				IsRecycling = true;
				templateAwareElement.OnTemplateRecycled();
				IsRecycling = false;
			}

			//Try Panel.Children before ViewGroup.GetChildren - this results in fewer allocations
			if (instance is Panel p)
			{
				for (var i = 0; i < p.Children.Count; i++)
				{
					PropagateOnTemplateReused(p.Children[i]);
				}
			}
			else if (instance is ViewGroup g)
			{
				var enumerator = g.GetChildren().GetEnumerator();

				while (enumerator.MoveNext())
				{
					PropagateOnTemplateReused(enumerator.Current);
				}
			}
		}

		private void RaiseOnParentCollected(FrameworkTemplate template, View instance)
		{
			var shouldEnqueue = false;

			lock (_instancesToRecycle)
			{
				_instancesToRecycle.Push((template, instance));

				shouldEnqueue = _instancesToRecycle.Count == 1;
			}

			if (shouldEnqueue)
			{
				NativeDispatcher.Main.Enqueue(Recycle);
			}
		}

		private void Recycle()
		{
			var array = ArrayPool<(FrameworkTemplate, View)>.Shared.Rent(RecycleBatchSize);

			var count = 0;

			var shouldRequeue = false;

			lock (_instancesToRecycle)
			{
				while (_instancesToRecycle.TryPop(out var instance) && count < RecycleBatchSize)
				{
					array[count++] = instance;
				}

				shouldRequeue = _instancesToRecycle.Count > 0;
			}

			try
			{
				for (var x = 0; x < count; x++)
				{
					var (template, instance) = array[x];

					if (InstanceTracker.TryRemove(instance, returnCookie: false))
					{
						RecycleTemplate(template, instance, cleanup: true);
					}
					else
					{
						if (this.Log().IsEnabled(LogLevel.Debug))
						{
							this.Log().Debug($"Failed to remove instance tracked for {instance.GetHashCode():X8}");
						}
					}
				}
			}
			finally
			{
				ArrayPool<(FrameworkTemplate, View)>.Shared.Return(array, clearArray: true);
			}

			if (shouldRequeue)
			{
				NativeDispatcher.Main.Enqueue(Recycle);
			}
		}

		private void RecycleTemplate(FrameworkTemplate template, View view, bool cleanup)
		{
			if (_trace.IsEnabled)
			{
				_trace.WriteEventActivity(TraceProvider.RecycleTemplate, EventOpcode.Send, new[] { view.GetType().ToString() });
			}

#if __ANDROID__
			if (view.Parent is BindableView bindableView)
			{
				bindableView.RemoveView(view);
			}
#endif

			// Make sure the TemplatedParent is disconnected
			if (view is IDependencyObjectStoreProvider provider)
			{
				var store = provider.Store;

				store.Parent = null;
				store.ClearValue(store.TemplatedParentProperty, DependencyPropertyValuePrecedences.Local);
			}

			if (cleanup)
			{
				PropagateOnTemplateReused(view);
			}

			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"Caching template, id={GetTemplateDebugId(template)}");
			}

			EnqueueTemplateToPool(template, view);
		}

		/// <summary>
		/// Manually release all available templates currently held by the pool. Normally you shouldn't need to call this method.
		/// </summary>
		public static void Scavenge() => Instance.Scavenge(true);

		internal void Scavenge(bool force)
		{
			var now = _platformProvider.Now;

			var removeCount = 0;

			List<FrameworkTemplate> cleanupList = new();

			foreach (var kvp in _availableInstances)
			{
				removeCount += kvp.Value.RemoveAll(t => force || now - t.CreationTime > TimeToLive);

				if (kvp.Value.Count == 0)
				{
					cleanupList.Add(kvp.Key);
				}
			}

			if (removeCount > 0)
			{
				for (int i = 0; i < cleanupList.Count; i++)
				{
					_availableInstances.Remove(cleanupList[i]);
				}

				if (_trace.IsEnabled)
				{
					for (var i = 0; i < removeCount; i++)
					{
						_trace.WriteEvent(TraceProvider.ReleaseTemplate);
					}
				}

				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().Debug($"Released {removeCount} template instances");
				}

				// Under iOS and Android, we need to trigger a GC to pick up
				// the orphan instances that we've just released.
				GC.Collect();
			}
		}

		private async void Scavenger()
		{
			Scavenge(false);

			await _platformProvider.Delay(TimeSpan.FromSeconds(30));

			_platformProvider.Schedule(Scavenger);
		}

		internal void SetPlatformProvider(IFrameworkTemplatePoolPlatformProvider provider)
			=> _platformProvider = provider ?? new FrameworkTemplatePoolDefaultPlatformProvider();

		/// <summary>
		/// Defines the ratio of memory usage at which the pool stops caching eligible templates.
		/// </summary>
		internal static float HighMemoryThreshold { get; set; } = .8f;

		internal static FrameworkTemplatePool Instance { get; } = new FrameworkTemplatePool();

		/// <summary>
		/// Determines if the pooling is enabled. If false, all requested instances are new.
		/// </summary>
		public static bool IsPoolingEnabled { get; set; } = true;

		/// <summary>
		/// Gets a value indicating whether the pool is currently recycling a template.
		/// </summary>
		internal static bool IsRecycling { get; private set; }

		/// <summary>
		/// Determines the duration for which a pooled template stays alive.
		/// </summary>
		public static TimeSpan TimeToLive { get; set; } = TimeSpan.FromMinutes(1);

		private class TemplateEntry
		{
			public TemplateEntry(TimeSpan creationTime, View view)
			{
				CreationTime = creationTime;
				View = view;
			}

			public TimeSpan CreationTime { get; private set; }

			public View View { get; private set; }
		}
	}
}
