#if NETSTANDARD
#define USE_HARD_REFERENCES
#endif

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Linq;
using Uno.Diagnostics.Eventing;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Uno.Extensions;
using Uno.Logging;
using Uno.UI;

#if XAMARIN_ANDROID
using View = Android.Views.View;
using ViewGroup = Android.Views.ViewGroup;
using Font = Android.Graphics.Typeface;
using Android.Graphics;
using DependencyObject = System.Object;
#elif XAMARIN_IOS
using View = UIKit.UIView;
using ViewGroup = UIKit.UIView;
using Color = UIKit.UIColor;
using Font = UIKit.UIFont;
using Windows.UI.Xaml.Controls;
using DependencyObject = System.Object;
using UIKit;
#elif __MACOS__
using View = AppKit.NSView;
using ViewGroup = AppKit.NSView;
using Color = AppKit.NSColor;
using Font = AppKit.NSFont;
using Windows.UI.Xaml.Controls;
using DependencyObject = System.Object;
using AppKit;
#elif METRO
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Markup;
#else
using View = Windows.UI.Xaml.UIElement;
using ViewGroup = Windows.UI.Xaml.UIElement;
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
	public class FrameworkTemplatePool
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

		private readonly Stopwatch _watch = new Stopwatch();
		private readonly Dictionary<FrameworkTemplate, List<TemplateEntry>> _pooledInstances = new Dictionary<FrameworkTemplate, List<TemplateEntry>>(FrameworkTemplate.FrameworkTemplateEqualityComparer.Default);

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
		private readonly HashSet<UIElement> _activeInstances = new HashSet<View>();
#endif

		/// <summary>
		/// Determines the duration for which a pooled template stays alive.
		/// </summary>
		public static TimeSpan TimeToLive { get; set; } = TimeSpan.FromMinutes(1);

		/// <summary>
		/// Determines if the pooling is enabled. If false, all requested instances are new.
		/// </summary>
		public static bool IsPoolingEnabled { get; set; } = true;

		private FrameworkTemplatePool()
		{
			_watch.Start();

#if !NET461
			CoreDispatcher.Main.RunIdleAsync(Scavenger);
#endif
		}

		private async void Scavenger(IdleDispatchedHandlerArgs e)
		{
			Scavenge(false);

			await Task.Delay(TimeSpan.FromSeconds(30));

			CoreDispatcher.Main.RunIdleAsync(Scavenger);
		}

		private void Scavenge(bool isManual)
		{
			var now = _watch.Elapsed;
			var removedInstancesCount = 0;

			foreach (var list in _pooledInstances.Values)
			{
				removedInstancesCount += list.RemoveAll(t => isManual || now - t.CreationTime > TimeToLive);
			}

			if (removedInstancesCount > 0)
			{
				if (_trace.IsEnabled)
				{
					for (int i = 0; i < removedInstancesCount; i++)
					{
						_trace.WriteEvent(TraceProvider.ReleaseTemplate);
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

		internal View DequeueTemplate(FrameworkTemplate template)
		{
			var list = GetTemplatePool(template);

			View instance;

			if (list?.Count == 0)
			{
				if (_trace.IsEnabled)
				{
					_trace.WriteEventActivity(TraceProvider.CreateTemplate, EventOpcode.Send, new[] { ((Func<View>)template).Method.DeclaringType.ToString() });
				}

				if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
				{
					this.Log().Debug($"Creating new template, id={GetTemplateDebugId(template)} IsPoolingEnabled:{IsPoolingEnabled}");
				}

				instance = template.LoadContent();

				if (IsPoolingEnabled && instance is IFrameworkElement)
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

				if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
				{
					this.Log().Debug($"Recycling template,    id={GetTemplateDebugId(template)}, {list.Count} items remaining in cache");
				}
			}

#if USE_HARD_REFERENCES
			_activeInstances.Add(instance);
#endif
			return instance;
		}

		private List<TemplateEntry> GetTemplatePool(FrameworkTemplate template)
		{
			List<TemplateEntry> instances;

			if (!_pooledInstances.TryGetValue(template, out instances))
			{
				_pooledInstances[template] = instances = new List<TemplateEntry>();
			}

			return instances;
		}

		private void OnParentChanged(object instance, object key, DependencyObjectParentChangedEventArgs args)
		{
			var list = GetTemplatePool(key as FrameworkTemplate);

			if (args.NewParent == null)
			{
				if (list == null)
				{
					list = GetTemplatePool(key as FrameworkTemplate);
				}

				if (_trace.IsEnabled)
				{
					_trace.WriteEventActivity(TraceProvider.RecycleTemplate, EventOpcode.Send, new[] { instance.GetType().ToString() });
				}

				PropagateOnTemplateReused(instance);

				var item = instance as View;

				list.Add(new TemplateEntry(_watch.Elapsed, item));

#if USE_HARD_REFERENCES
				_activeInstances.Remove(item);
#endif

				if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
				{
					(this).Log().Debug($"Caching template,      id={GetTemplateDebugId(key as FrameworkTemplate)}, {list.Count} items now in cache");
				}
			}
			else
			{
				var index = list.FindIndex(e => ReferenceEquals(e.Control, instance));

				if (index != -1)
				{
					list.RemoveAt(index);
				}
			}
		}

		internal static void PropagateOnTemplateReused(object instance)
		{
			// If DataContext is not null, it means it has been explicitly set (not inherited). Resetting the view could push an invalid value through 2-way binding in this case.
			if (instance is IFrameworkTemplatePoolAware templateAwareElement && (instance as IFrameworkElement).DataContext == null)
			{
				templateAwareElement.OnTemplateRecycled();
			}

			//Try Panel.Children before ViewGroup.GetChildren - this results in fewer allocations
			if (instance is Controls.Panel p)
			{
				foreach (object o in p.Children)
				{
					PropagateOnTemplateReused(o);
				}
			}
			else if (instance is ViewGroup g)
			{
				foreach (object o in g.GetChildren())
				{
					PropagateOnTemplateReused(o);
				}
			}
		}

		private string GetTemplateDebugId(FrameworkTemplate template)
		{
			//Grossly inefficient, should only be used for debug logging
			int i = -1;
			foreach (var kvp in _pooledInstances)
			{
				i++;
				var pooledTemplate = kvp.Key;
				if (template?.Equals(pooledTemplate) ?? false)
				{
					var func = ((Func<View>)template);

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
