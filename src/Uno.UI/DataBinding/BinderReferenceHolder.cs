using System;
using System.Linq;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI.Extensions;
using System.Diagnostics;
using System.ComponentModel;
using Windows.UI.Xaml;

#if __IOS__
using UIKit;
using _NativeReference = global::Foundation.NSObject;
using _NativeView = UIKit.UIView;
#elif __MACOS__
using AppKit;
using _NativeReference = global::Foundation.NSObject;
using _NativeView = AppKit.NSView;
#elif __ANDROID__
using _NativeReference = Android.Views.View;
using _NativeView = Android.Views.View;
#else
using _NativeReference = Windows.UI.Xaml.UIElement;
using _NativeView = Windows.UI.Xaml.UIElement;
#endif

namespace Uno.UI.DataBinding
{
	/// <summary>
	/// A helper class for memory diagnostics.
	/// </summary>
	/// <remarks>
	/// This class functions as a GC marker to detect if a binder has 
	/// been collected, hence detect memory leaks of the visual tree.
	/// </remarks>
	[DebuggerDisplay("{_handle}", Name = "{_type.ToString(),nq}")]
	[DebuggerTypeProxy(typeof(BinderReferenceHolderDebuggerProxy))]
	public class BinderReferenceHolder
	{
		public const string BinderActiveReferencesCounter = "Performance.ActiveBinders";
		public const string BinderCollectedReferencesCounter = "Performance.CollectedBinders";

		private static Dictionary<IntPtr, WeakReference> _nativeHolders = new Dictionary<IntPtr, WeakReference>();
		private static List<WeakReference> _holders = new List<WeakReference>();
		private readonly WeakReference _ref;
		private readonly Type _type;

		private readonly WeakReference _target;
		private readonly Dictionary<IntPtr, System.Tuple<Type, WeakReference>> _newReferences = new Dictionary<IntPtr, System.Tuple<Type, WeakReference>>();
		private readonly IntPtr _handle;

		public static bool IsEnabled { get; set; }

		public BinderReferenceHolder(Type type, object target)
		{
			_type = type;
			_target = new WeakReference(target);

#if !IS_UNO
			Uno.Services.Diagnostics.Performance.Increment(BinderActiveReferencesCounter);
#endif

			lock (_holders)
			{
				_ref = new WeakReference(this);

				_handle = IntPtr.Zero;

				var view = target as _NativeReference;

				if (view != null)
				{
					_handle = view.Handle;
					_nativeHolders[_handle] = _ref;
				}
				else
				{
					_holders.Add(_ref);
				}
			}
		}

		/// <summary>
		/// Adds the specified instance as referenced by the specified parent.
		/// </summary>
		public static void AddNativeReference(_NativeReference instance, _NativeReference parent)
		{
			WeakReference localRef;

			if (_nativeHolders.TryGetValue(instance.Handle, out localRef))
			{
				var holder = localRef.Target as BinderReferenceHolder;

				if (holder != null)
				{
					holder._newReferences[parent.Handle] = Tuple.Create(parent.GetType(), new WeakReference(parent));
				}
			}
		}

		/// <summary>
		/// Removes the specified instance as referenced by the specified parent.
		/// </summary>
		public static void RemoveNativeReference(_NativeReference instance, _NativeReference parent)
		{
			WeakReference localRef;

			if (_nativeHolders.TryGetValue(instance.Handle, out localRef))
			{
				var holder = localRef.Target as BinderReferenceHolder;

				if (holder != null && parent != null)
				{
					holder._newReferences.Remove(parent.Handle);
				}
			}
		}

		/// <summary>
		/// Retreives a list of binders that are native views that
		/// don't have a parent, and are not attached to the window.
		/// An inactive binder may be a memory leak.
		/// </summary>
		public static BinderReferenceHolder[] GetInactiveViewBinders()
		{
			var q = from r in _holders.Concat(_nativeHolders.Values)
					let holder = r.Target as BinderReferenceHolder
					where holder != null && holder.IsInactiveView()
					select holder;

			return q.ToArray();
		}

		/// <summary>
		/// Retrieves a list of binders that are native views that 
		/// aren't attached to the window. These views may be part 
		/// of a leaked control.
		/// </summary>
		public static BinderReferenceHolder[] GetInactiveChildViewBinders()
		{
			var q = from r in _holders.Concat(_nativeHolders.Values)
					let holder = r.Target as BinderReferenceHolder
					let view = holder?._target.Target as _NativeView
					where view != null && !IsInactiveView(view) && IsInactiveView(view.GetTopLevelParent())
					select holder;

			return q.ToArray();
		}

		/// <summary>
		/// Retrieves all inactive views of the specified type.
		/// </summary>
		public static T[] GetInactiveInstancesOfType<T>() => GetInactiveViewBinders().Select(h => h._target.Target).OfType<T>().ToArray();

		/// <summary>
		/// Retrieves all children of the specified type whose top-level parent is detached from the window.
		/// </summary>
		public static T[] GetInactiveChildInstancesOfType<T>() => GetInactiveChildViewBinders().Select(h => h._target.Target).OfType<T>().ToArray();

		/// <summary>
		/// Retreives statistics about the live instances.
		/// </summary>
		public static System.Tuple<Type, int>[] GetReferenceStats()
		{
			lock (_holders)
			{
				var q = from r in _holders.Concat(_nativeHolders.Values)
						let holder = r.Target as BinderReferenceHolder
						where holder != null
						group holder by holder._type into types
						let count = types.Count()
						orderby count descending
						select System.Tuple.Create(types.Key, count);

				return q.ToArray();
			}
		}

		/// <summary>
		/// Retreives statistics about the live instances.
		/// </summary>
		public static void LogReferenceStatsWithDetails()
		{
			lock (_holders)
			{
				var q = from r in _holders.Concat(_nativeHolders.Values)
						let holder = r.Target as BinderReferenceHolder
						where holder != null
						where holder._type == typeof(Windows.UI.Xaml.Controls.Grid)
						group holder by holder._type into types
						let count = types.Count()
						let parents = (
										from type in types
										let parentType = (type._target?.Target as DependencyObject).GetParent()?.GetType()
										where parentType != null
										select parentType
									 ).Distinct()
						orderby count descending
						select System.Tuple.Create(types.Key, count, parents);

				var sb = new StringBuilder();
				sb.Append("Detailed DependencyObject references: \r\n");

				foreach (var activref in q)
				{
					sb.AppendFormatInvariant("\t{0}: {1}, [{2}]\r\n", activref.Item1, activref.Item2, string.Join(", ", activref.Item3));
				}

				if (IsEnabled && typeof(BinderReferenceHolder).Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Information))
				{
					typeof(BinderReferenceHolder).Log().Info(sb.ToString());
				}
			}
		}

		public static void LogActiveViewReferencesStatsDiff(Tuple<Type, int>[] activeStats)
		{
			var newActiveStats = GetReferenceStats();

			LogDiff(activeStats, newActiveStats, "Active");
		}

		public static void LogInactiveViewReferencesStatsDiff(Tuple<Type, int>[] inactiveStats)
		{
			var newInactiveStats = GetInactiveViewReferencesStats();

			LogDiff(inactiveStats, newInactiveStats, "Inactive");
		}

		private static void LogDiff(Tuple<Type, int>[] oldInactiveStats, Tuple<Type, int>[] newInactiveStats, string referenceType)
		{
			var q = from oldInactiveStat in oldInactiveStats
					from newInactiveStat in newInactiveStats
					where oldInactiveStat.Item1 == newInactiveStat.Item1
					let diff = newInactiveStat.Item2 - oldInactiveStat.Item2
					where diff != 0
					select new
					{
						Type = oldInactiveStat.Item1,
						Diff = diff
					};

			var sb = new StringBuilder();
			sb.Append($"Detailed {referenceType} DependencyObject references delta: \r\n");

			foreach (var activref in q)
			{
				sb.AppendFormatInvariant("\t{0}: {1}\r\n", activref.Type, activref.Diff);
			}

			if (IsEnabled && typeof(BinderReferenceHolder).Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Information))
			{
				typeof(BinderReferenceHolder).Log().Info(sb.ToString());
			}
		}

		/// <summary>
		/// Retreives statistics about the live inactive instances.
		/// </summary>
		public static System.Tuple<Type, int>[] GetInactiveViewReferencesStats()
		{
			lock (_holders)
			{
				var q = from r in _holders.Concat(_nativeHolders.Values)
						let holder = r.Target as BinderReferenceHolder
						where holder != null && holder.IsInactiveView()
						group holder by holder._type into types
						let count = types.Count()
						orderby count descending
						select System.Tuple.Create(types.Key, count);

				return q.ToArray();
			}
		}

		public static void LogReport()
		{
			try
			{
				var sb = new StringBuilder();

				sb.Append("Inactive DependencyObject references: \r\n");
				foreach (var rs in GetInactiveViewReferencesStats())
				{
					sb.AppendFormatInvariant("\t{0}: {1}\r\n", rs.Item1, rs.Item2);
				}

				sb.Append("Active DependencyObject references: \r\n");
				foreach (var rs in GetReferenceStats())
				{
					sb.AppendFormatInvariant("\t{0}: {1}\r\n", rs.Item1, rs.Item2);
				}

				if (IsEnabled && typeof(BinderReferenceHolder).Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Information))
				{
					typeof(BinderReferenceHolder).Log().Info(sb.ToString());
				}
			}
			catch (Exception ex)
			{
				if (typeof(BinderReferenceHolder).Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Error))
				{
					typeof(BinderReferenceHolder).Log().Error("Failed to generate binders report", ex);
				}
			}
		}

		private bool IsInactiveView() => IsInactiveView(_target.Target);

		private static bool IsInactiveView(object target)
		{
			try
			{
#if __IOS__
				var uiView = target as UIView;

				if (uiView != null && ObjCRuntime.Runtime.TryGetNSObject(uiView.Handle) != null)
				{
					return uiView.Superview == null && uiView.Window == null;
				}
#elif __ANDROID__
				var uiView = target as Android.Views.View;

				if (uiView != null)
				{
					return uiView.Parent == null && !uiView.IsLoaded();
				}
#endif
			}
			catch
			{

			}

			return false;
		}

		~BinderReferenceHolder()
		{
			if (IsEnabled && this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
			{
				this.Log().Debug($"Collecting [{_type}]");
			}

			lock (_holders)
			{
				if (_handle != IntPtr.Zero)
				{
					_nativeHolders.Remove(_handle);
				}
				else
				{
					_holders.Remove(_ref);
				}
			}

#if !IS_UNO
			Uno.Services.Diagnostics.Performance.Decrement(BinderActiveReferencesCounter);
			Uno.Services.Diagnostics.Performance.Increment(BinderCollectedReferencesCounter);
#endif
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		public object GetDebuggerProxy()
		{
			return new BinderReferenceHolderDebuggerProxy(this);
		}

		private class BinderReferenceHolderDebuggerProxy
		{
			public BinderReferenceHolderDebuggerProxy(BinderReferenceHolder holder)
			{
				IsAlive = holder._target.IsAlive;
				Target = holder._target;
				References = holder
					._newReferences
					.Select(p => new BinderReference(p.Value.Item1, p.Value.Item2.Target))
					.ToArray();
			}

			public bool IsAlive { get; }

			public BinderReference[] References { get; }

			public object Target { get; }

			public GlobalStats GlobalStats => GlobalStats.Default;
		}

		private class BinderReference
		{
			public BinderReference(Type type, object target)
			{
				Type = type;
				Target = target;
			}

			public object Target { get; }

			public Type Type { get; }
		}

		[DebuggerDisplay("BinderDetails global stats (may be slow)")]
		private class GlobalStats
		{
			public readonly static GlobalStats Default = new GlobalStats();

			public object[] InactiveViewBinders
			{
				get
				{
					return GetInactiveViewReferencesStats();
				}
			}
		}
	}
}
