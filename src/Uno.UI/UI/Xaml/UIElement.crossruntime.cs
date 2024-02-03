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
		private static protected readonly Logger _log = typeof(UIElement).Log();
		private static protected readonly Logger _logDebug = _log.IsEnabled(LogLevel.Debug) ? _log : null;
		private static protected readonly Logger _logTrace = _log.IsEnabled(LogLevel.Trace) ? _log : null;

		private readonly bool _isFrameworkElement;
		internal readonly MaterializableList<UIElement> _children = new MaterializableList<UIElement>();

		// Even if this a concept of FrameworkElement, the loaded state is handled by the UIElement in order to avoid
		// to cast to FrameworkElement each time a child is added or removed.
#if __WASM__
		internal bool IsLoaded { get; private protected set; } // protected for the native loading support
#else
		internal bool IsLoaded { get; private set; }
#endif

		/// <summary>
		/// This flag is transiently set while element is 'loading' but not yet 'loaded'.
		/// </summary>
#if __WASM__
		internal bool IsLoading { get; private protected set; } // protected for the native loading support
#else
		internal bool IsLoading { get; private set; }
#endif

		/// <summary>
		/// Gets the element depth in the visual tree.
		/// ** WARNING** This is set before the FrameworkElement loading event and cleared on unload.
		/// </summary>
		internal int Depth { get; private set; } = int.MinValue;

		internal static void LoadingRootElement(UIElement visualTreeRoot)
			=> visualTreeRoot.OnElementLoading(1);

		internal static void RootElementLoaded(UIElement visualTreeRoot)
		{
			visualTreeRoot.SetHitTestVisibilityForRoot();
			visualTreeRoot.OnElementLoaded();
		}

		internal static void RootElementUnloaded(UIElement visualTreeRoot)
		{
			visualTreeRoot.ClearHitTestVisibilityForRoot();
			visualTreeRoot.OnElementUnloaded();
		}

		partial void OnLoading();

		// Overloads for the FrameworkElement to raise the events
		// (Load/Unload is actually a concept of the FwElement, but it's easier to handle it directly from the UIElement)
		private protected virtual void OnFwEltLoading() { }
		private protected virtual void OnFwEltLoaded() { }
		private protected virtual void OnFwEltUnloaded() { }

		private void OnElementLoading(int depth)
		{
			if (IsLoading || IsLoaded)
			{
				// Note: If child is added while in parent's Loading handler, we might get a double Loading!
				return;
			}

			IsLoading = true;
			Depth = depth;

			OnLoading();
			OnFwEltLoading();

			// Explicit propagation of the loading even must be performed
			// after the compiled bindings are applied (cf. OnLoading), as there may be altered
			// properties that affect the visual tree.

			// Get a materialized copy for Wasm to avoid the use of iterators
			// where try/finally has a high cost.
			var children = _children.Materialized;
			for (int i = 0; i < children.Count; i++)
			{
				children[i].OnElementLoading(depth + 1);
			}
		}

		private void OnElementLoaded()
		{
			if (IsLoaded)
			{
				return;
			}

			if (!IsLoading && _log.IsEnabled(LogLevel.Error))
			{
				_log.Error($"Element {this} is being loaded while not in loading state");
			}

			IsLoading = false;
			IsLoaded = true;

			OnFwEltLoaded();
			UpdateHitTest();

			// Get a materialized copy for Wasm to avoid the use of iterators
			// where try/finally has a high cost.
			var children = _children.Materialized;
			for (int i = 0; i < children.Count; i++)
			{
				children[i].OnElementLoaded();
			}
		}

		private void OnElementUnloaded()
		{
			if (!IsLoaded)
			{
				return;
			}

			IsLoaded = false;
			Depth = int.MinValue;

			// Get a materialized copy for Wasm to avoid the use of iterators
			// where try/finally has a high cost.
			var children = _children.Materialized;
			for (int i = 0; i < children.Count; i++)
			{
				children[i].OnElementUnloaded();
			}

			OnFwEltUnloaded();
			UpdateHitTest();
		}

		private void OnAddingChild(UIElement child)
		{
			if (IsLoading || IsLoaded)
			{
				child.OnElementLoading(Depth + 1);
			}
		}

		private void OnChildAdded(UIElement child)
		{
			if (
#if __WASM__
				!FeatureConfiguration.FrameworkElement.WasmUseManagedLoadedUnloaded ||
#endif
				!IsLoaded
				|| !child._isFrameworkElement)
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
			else
			{
				child.OnElementLoaded();
			}
		}

		private void OnChildRemoved(UIElement child)
		{
			if (
#if __WASM__
				!FeatureConfiguration.FrameworkElement.WasmUseManagedLoadedUnloaded ||
#endif
				!IsLoaded
				|| !child._isFrameworkElement)
			{
				return;
			}

			if (child.IsLoaded)
			{
				child.OnElementUnloaded();
			}
			else
			{
				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().Debug($"{this.GetDebugName()}: Inconsistent state: child {child} is not loaded (OnChildRemoved). Common cause for this is an exception during Loaded handling.");
				}
			}
		}

		internal Point GetPosition(Point position, UIElement relativeTo)
			=> TransformToVisual(relativeTo).TransformPoint(position);

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
