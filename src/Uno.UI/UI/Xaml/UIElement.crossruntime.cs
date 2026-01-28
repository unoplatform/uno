using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using Windows.Foundation;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Uno.Collections;
using Uno.Extensions;
using Uno.Foundation;
using Uno.Foundation.Logging;
using Uno.UI;
using Uno.UI.Extensions;
using Uno.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.System;
using System.Reflection;

using Uno.Core.Comparison;

namespace Microsoft.UI.Xaml
{
	public partial class UIElement : DependencyObject
	{
		internal bool IsActiveInVisualTree { get; private set; }

		private static protected readonly Logger _log = typeof(UIElement).Log();
		private static protected readonly Logger _logDebug = _log.IsEnabled(LogLevel.Debug) ? _log : null;
		private static protected readonly Logger _logTrace = _log.IsEnabled(LogLevel.Trace) ? _log : null;

		private readonly bool _isFrameworkElement;
		internal readonly MaterializableList<UIElement> _children = new MaterializableList<UIElement>();

		// Even if this a concept of FrameworkElement, the loaded state is handled by the UIElement in order to avoid
		// to cast to FrameworkElement each time a child is added or removed.
		internal bool IsLoaded { get; set; }

		/// <summary>
		/// This flag is transiently set while element is 'loading' but not yet 'loaded'.
		/// </summary>
		internal bool IsLoading { get; private protected set; }

		/// <summary>
		/// Gets the element depth in the visual tree.
		/// ** WARNING** This is set before the FrameworkElement loading event and cleared on unload.
		/// </summary>
		internal int Depth { get; private set; } = int.MinValue;

		internal void RaiseLoaded()
		{
			if (IsLoaded)
			{
				return;
			}

			IsLoading = false;
			IsLoaded = true;

			OnFwEltLoaded();
			UpdateHitTest();
		}

		// Overloads for the FrameworkElement to raise the events
		// (Load/Unload is actually a concept of the FwElement, but it's easier to handle it directly from the UIElement)
		private protected virtual void OnFwEltLoaded() { }
		private protected virtual void OnFwEltUnloaded() { }

		internal void OnElementUnloaded()
		{
			IsLoaded = false;

			OnFwEltUnloaded();
			UpdateHitTest();
		}

#if __SKIA__ || __WASM__
		private void OnChildAdded(UIElement child)
		{
			if (!child._isFrameworkElement)
			{
				return;
			}

			if (child.IsLoaded)
			{
				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().Debug($"{this.GetDebugName()}: Inconsistent state: child {child} is already loaded (OnChildAdded). Common cause for this is an exception during Unloaded handling.");
				}
			}
#if UNO_HAS_ENHANCED_LIFECYCLE
			else if (child.IsActiveInVisualTree)
			{
				var context = this.GetContext();
				var eventManager = context.EventManager;
				eventManager.RequestRaiseLoadedEventOnNextTick();
			}
#endif
		}

		private void OnChildRemoved(UIElement child)
		{
			child.Shutdown();
			(child as IDependencyObjectStoreProvider)?.Store.ClearInheritedDataContext();

#if UNO_HAS_ENHANCED_LIFECYCLE
			var leaveParams = new LeaveParams(IsActiveInVisualTree);
			child.Leave(leaveParams);
#endif
		}
#endif

		internal Point GetPosition(Point position, UIElement relativeTo)
			=> TransformToVisual(relativeTo).TransformPoint(position);

#if UNO_HAS_ENHANCED_LIFECYCLE
		private void ChildEnter(UIElement child, EnterParams @params)
		{
			// Uno TODO: WinUI has much more complex logic than this.
			if (@params.IsLive)
			{
				child.EnterImpl(@params, this.Depth + 1);
			}
		}
#endif

#if DEBUG

		/// <summary>
		/// Convenience method to find all views with the given name.
		/// </summary>
		public FrameworkElement[] FindViewsByName(string name) => FindViewsByName(name, searchDescendantsOnly: false);


		/// <summary>
		/// Convenience method to find all views with the given name.
		/// </summary>
		/// <param name="searchDescendantsOnly">If true, only look in descendants of the current view; otherwise search the entire visual tree.</param>
		public FrameworkElement[] FindViewsByName(string name, bool searchDescendantsOnly)
		{

			FrameworkElement topLevel = this as FrameworkElement;

			if (!searchDescendantsOnly)
			{
				while (topLevel?.Parent is FrameworkElement newTopLevel)
				{
					topLevel = newTopLevel;
				}
			}

			return GetMatchesInChildren(topLevel).ToArray();

			IEnumerable<FrameworkElement> GetMatchesInChildren(FrameworkElement parent)
			{
				if (parent == null)
				{
					yield break;
				}

				foreach (var subview in parent._children)
				{
					if (subview is FrameworkElement fe && fe.Name == name)
					{
						yield return fe;
					}

					foreach (var match in GetMatchesInChildren(subview as FrameworkElement))
					{
						yield return match;
					}
				}
			}
		}
#endif
	}
}
