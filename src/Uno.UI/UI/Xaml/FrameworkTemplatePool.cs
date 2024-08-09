#nullable enable

#if UNO_REFERENCE_API
#define USE_HARD_REFERENCES
#endif

using System;
using System.Collections.Generic;
using Uno.Diagnostics.Eventing;
using Windows.UI.Xaml;
using Uno.Buffers;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI;
using Windows.UI.Xaml.Controls;
using Uno.UI.Dispatching;
using Windows.Foundation.Metadata;
using Windows.System;

#if __ANDROID__
using View = Android.Views.View;
using ViewGroup = Android.Views.ViewGroup;
using Font = Android.Graphics.Typeface;
using Android.Graphics;
using DependencyObject = System.Object;
using Uno.UI.Controls;
#elif __IOS__
using View = UIKit.UIView;
using ViewGroup = UIKit.UIView;
using Color = UIKit.UIColor;
using Font = UIKit.UIFont;
using DependencyObject = System.Object;
using UIKit;
#elif __MACOS__
using View = AppKit.NSView;
using ViewGroup = AppKit.NSView;
using Color = AppKit.NSColor;
using Font = AppKit.NSFont;
using DependencyObject = System.Object;
using AppKit;
#elif METRO
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Markup;
#else
using View = Windows.UI.Xaml.UIElement;
using ViewGroup = Windows.UI.Xaml.UIElement;
using System.Text;
using System.Runtime.CompilerServices;
#endif


namespace Windows.UI.Xaml
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
		internal static FrameworkTemplatePool Instance { get; } = new FrameworkTemplatePool();
		public static class TraceProvider
		{
			public readonly static Guid Id = Guid.Parse("{266B850B-674C-4D3E-9B58-F680BE653E18}");

			public const int CreateTemplate = 1;
			public const int RecycleTemplate = 2;
			public const int ReuseTemplate = 3;
			public const int ReleaseTemplate = 4;
		}

		private readonly static IEventProvider _trace = Tracing.Get(TraceProvider.Id);

		private readonly Dictionary<FrameworkTemplate, List<TemplateEntry>> _pooledInstances = new Dictionary<FrameworkTemplate, List<TemplateEntry>>(FrameworkTemplate.FrameworkTemplateEqualityComparer.Default);
		private IFrameworkTemplatePoolPlatformProvider _platformProvider = new FrameworkTemplatePoolDefaultPlatformProvider();
		private static bool _isPoolingEnabled;

#if USE_HARD_REFERENCES
		/// <summary>
		/// List of instances managed by the pool
		/// </summary>
		/// <remarks>
		/// This list is required to avoid the GC to collect the instances. Othewise, the pooled instance
		/// may never get its Parent property set to null, and the pool will never get notified that an instance
		/// can be reused.
		///
		/// The root of the behavior is linked to WeakReferences to objects pending for finalizers are considered
		/// null, something that does not happen on Xamarin.iOS/Android.
		/// </remarks>
		private readonly HashSet<View> _activeInstances = new();
#endif

		/// <summary>
		/// Determines the duration for which a pooled template stays alive.
		/// </summary>
		public static TimeSpan TimeToLive { get; set; } = TimeSpan.FromMinutes(1);

		/// <summary>
		/// Determines if the pooling is enabled. If false, all requested instances are new.
		/// </summary>
		/// <remarks>
		/// This feature is currently disabled and has no effect. See: https://github.com/unoplatform/uno/issues/13969
		/// </remarks>
		public static bool IsPoolingEnabled
		{
			// Pooling is forced disabled, see InternalIsPoolingEnabled.
			get => false;
			set
			{
				if (typeof(FrameworkTemplatePool).Log().IsEnabled(LogLevel.Warning))
				{
					typeof(FrameworkTemplatePool).Log().LogWarn($"Template pooling is disabled in this build of Uno Platform. See https://github.com/unoplatform/uno/issues/13969");
				}
			}
		}

		// Pooling is disabled until https://github.com/unoplatform/uno/issues/13969 is fixed, but we
		// allow some runtime tests to use it.
		internal static bool InternalIsPoolingEnabled
		{
			get => _isPoolingEnabled;
			set => _isPoolingEnabled = value;
		}

		/// <summary>
		/// Gets a value indicating whether the pool is currently recycling a template.
		/// </summary>
		internal static bool IsRecycling { get; private set; }

		/// <summary>
		/// Defines the ratio of memory usage at which the pools starts to stop pooling elligible views.
		/// </summary>
		internal static float HighMemoryThreshold { get; set; } = .8f;

		/// <summary>
		/// Registers a custom <see cref="IFrameworkTemplatePoolPlatformProvider"/>
		/// </summary>
		/// <param name="provider"></param>
		internal void SetPlatformProvider(IFrameworkTemplatePoolPlatformProvider provider)
		{
			if (provider is not null)
			{
				_platformProvider = provider;
			}
			else
			{
				_platformProvider = new FrameworkTemplatePoolDefaultPlatformProvider();
			}
		}

		private FrameworkTemplatePool()
		{
#if !IS_UNIT_TESTS
			_platformProvider.Schedule(Scavenger);
#endif
		}

		private async void Scavenger()
		{
			Scavenge(false);

			await _platformProvider.Delay(TimeSpan.FromSeconds(30));

			_platformProvider.Schedule(Scavenger);
		}

		internal void Scavenge(bool isManual)
		{
			var now = _platformProvider.Now;
			var removedInstancesCount = 0;

			foreach (var list in _pooledInstances.Values)
			{
				removedInstancesCount += list.RemoveAll(t =>
				{
					var remove = isManual || now - t.CreationTime > TimeToLive;

#if USE_HARD_REFERENCES
					if (remove)
					{
						_activeInstances.Remove(t.Control);
					}
#endif

					return remove;
				});
			}

			if (removedInstancesCount > 0)
			{
				if (_trace.IsEnabled)
				{
					for (int i = 0; i < removedInstancesCount; i++)
					{
						_trace.WriteEvent(TraceProvider.ReleaseTemplate);
					}

					if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
					{
						this.Log().Debug($"Released {removedInstancesCount} template instances");
					}
				}

				// Under iOS and Android, we need to force the collection for the GC
				// to pick up the orphan instances that we've just released.

				GC.Collect();
			}
		}

		/// <summary>
		/// Release all templates that are currently held by the pool.
		/// </summary>
		/// <remarks>The pool will periodically release templates that haven't been reused within the span of <see cref="TimeToLive"/>, so
		/// normally you shouldn't need to call this method. It may be useful in advanced memory management scenarios.</remarks>
		public static void Scavenge() => Instance.Scavenge(true);


		internal int GetPooledTemplatesCount()
		{
			int count = 0;

			foreach (var list in _pooledInstances.Values)
			{
				count += list.Count;
			}

			return count;
		}

		internal View? DequeueTemplate(FrameworkTemplate template)
		{
			var list = GetTemplatePool(template);

			View? instance;

			if (list.Count == 0)
			{
				if (_trace.IsEnabled)
				{
					_trace.WriteEventActivity(TraceProvider.CreateTemplate, EventOpcode.Send, new[] { ((Func<View>)template).Method.DeclaringType?.ToString() });
				}

				if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
				{
					this.Log().Debug($"Creating new template, id={GetTemplateDebugId(template)} IsPoolingEnabled:{_isPoolingEnabled}");
				}

				instance = ((IFrameworkTemplateInternal)template).LoadContent();

				if (_isPoolingEnabled && instance is IFrameworkElement)
				{
					DependencyObjectExtensions.RegisterParentChangedCallback((DependencyObject)instance, template, OnParentChanged);
				}
			}
			else
			{
				int position = list.Count - 1;
				instance = list[position].Control;
				list.RemoveAt(position);

				if (_trace.IsEnabled)
				{
					_trace.WriteEventActivity(TraceProvider.ReuseTemplate, EventOpcode.Send, new[] { instance.GetType().ToString() });
				}

				if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
				{
					this.Log().Debug($"Recycling template,    id={GetTemplateDebugId(template)}, {list.Count} items remaining in cache");
				}
			}

#if USE_HARD_REFERENCES
			if (_isPoolingEnabled && instance is { })
			{
				_activeInstances.Add(instance);
			}
#endif
			return instance;
		}

		private List<TemplateEntry> GetTemplatePool(FrameworkTemplate template)
		{
			List<TemplateEntry>? instances;

			if (!_pooledInstances.TryGetValue(template, out instances))
			{
				_pooledInstances[template] = instances = new List<TemplateEntry>();
			}

			return instances;
		}

		private Stack<object> _instancesToRecycle = new();

		private void RaiseOnParentCollected(object instance)
		{
			var shouldEnqueue = false;

			lock (_instancesToRecycle)
			{
				_instancesToRecycle.Push(instance);

				shouldEnqueue = _instancesToRecycle.Count == 1;
			}

			if (shouldEnqueue)
			{
				NativeDispatcher.Main.Enqueue(Recycle);
			}
		}

		private const int RecycleBatchSize = 32;

		private void Recycle()
		{
			var array = ArrayPool<object>.Shared.Rent(RecycleBatchSize);

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
#if __ANDROID__
					if (((View)array[x]).Parent is BindableView bindableView)
					{
						bindableView.RemoveView((View)array[x]);
					}
#endif

					array[x].SetParent(null);
				}
			}
			finally
			{
				ArrayPool<object>.Shared.Return(array, clearArray: true);
			}

			if (shouldRequeue)
			{
				NativeDispatcher.Main.Enqueue(Recycle);
			}
		}

		/// <summary>
		/// Manually return an unused template root to the pool.
		/// </summary>
		/// <remarks>
		/// We disable cleaning the elements inside the template root because it may cause problems. It's safe to assume the template root
		/// is still 'clean' because it was never made available to application code.
		/// </remarks>
		internal void ReleaseTemplateRoot(View root, FrameworkTemplate template) => TryReuseTemplateRoot(root, template, null, shouldCleanUpTemplateRoot: false);

		private void OnParentChanged(object instance, object? key, DependencyObjectParentChangedEventArgs? args)
			=> TryReuseTemplateRoot(instance, key, args?.NewParent, shouldCleanUpTemplateRoot: true);

		private void TryReuseTemplateRoot(object instance, object? key, object? newParent, bool shouldCleanUpTemplateRoot)
		{
			if (!_isPoolingEnabled)
			{
				return;
			}

			if (!CanUsePool())
			{
				if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
				{
					(this).Log().Debug($"Not caching template, memory threshold is reached");
				}

				return;
			}

			var list = GetTemplatePool(key as FrameworkTemplate ?? throw new InvalidOperationException($"Received {key} but expecting {typeof(FrameworkElement)}"));

			if (newParent == null)
			{
				if (_trace.IsEnabled)
				{
					_trace.WriteEventActivity(TraceProvider.RecycleTemplate, EventOpcode.Send, new[] { instance.GetType().ToString() });
				}

				if (instance is IDependencyObjectStoreProvider provider)
				{
					// Make sure the TemplatedParent is disconnected
					provider.Store.Parent = null;
					provider.Store.ClearValue(provider.Store.TemplatedParentProperty, DependencyPropertyValuePrecedences.Local);
				}
				if (shouldCleanUpTemplateRoot)
				{
					PropagateOnTemplateReused(instance);
				}

				var item = instance as View;

				if (item != null)
				{
					list.Add(new TemplateEntry(_platformProvider.Now, item));
#if USE_HARD_REFERENCES
					_activeInstances.Remove(item);
#endif
				}
				else if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Warning))
				{
					this.Log().Warn($"Enqueued template root was not a view");
				}


				if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
				{
					(this).Log().Debug($"Caching template,      id={GetTemplateDebugId(key as FrameworkTemplate)}, {list.Count} items now in cache");
				}
			}
			else
			{
				InstanceTracker.Add(newParent, instance);

				var index = list.FindIndex(e => ReferenceEquals(e.Control, instance));

				if (index != -1)
				{
					list.RemoveAt(index);
				}
			}
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

		internal static void PropagateOnTemplateReused(object instance)
		{
			// If DataContext is not null, it means it has been explicitly set (not inherited). Resetting the view could push an invalid value through 2-way binding in this case.
			if (instance is IFrameworkTemplatePoolAware templateAwareElement && (instance as IFrameworkElement)!.DataContext == null)
			{
				IsRecycling = true;
				templateAwareElement.OnTemplateRecycled();
				IsRecycling = false;
			}

			//Try Panel.Children before ViewGroup.GetChildren - this results in fewer allocations
			if (instance is Controls.Panel p)
			{
				for (var i = 0; i < p.Children.Count; i++)
				{
					var o = (object)p.Children[i];

					PropagateOnTemplateReused(o);
				}
			}
			else if (instance is ViewGroup g)
			{
				// This block is a manual enumeration to avoid the foreach pattern
				// See https://github.com/dotnet/runtime/issues/56309 for details
				var childrenEnumerator = g.GetChildren().GetEnumerator();
				while (childrenEnumerator.MoveNext())
				{
					PropagateOnTemplateReused(childrenEnumerator.Current);
				}
			}
		}

		private string GetTemplateDebugId(FrameworkTemplate? template)
		{
			//Grossly inefficient, should only be used for debug logging
			int i = -1;
			foreach (var kvp in _pooledInstances)
			{
				i++;
				var pooledTemplate = kvp.Key;
				if ((template?.Equals(pooledTemplate) ?? false) && template._viewFactory is { } func)
				{
					return $"{i}({func.Method.DeclaringType}.{func.Method.Name})";
				}
			}

			return "Unknown";
		}

		private class TemplateEntry
		{
			public TemplateEntry(TimeSpan creationTime, View dependencyObject)
			{
				CreationTime = creationTime;
				Control = dependencyObject;
			}

			public TimeSpan CreationTime { get; private set; }

			public View Control { get; private set; }
		}
	}
}
