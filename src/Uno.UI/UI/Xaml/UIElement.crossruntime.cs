using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Uno.Collections;
using Uno.Core.Comparison;
using Uno.Extensions;
using Uno.Foundation;
using Uno.Foundation.Logging;
using Uno.UI;
using Uno.UI.Extensions;
using Uno.UI.Xaml;
using Windows.Foundation;
using Windows.System;

namespace Microsoft.UI.Xaml
{
	public partial class UIElement : DependencyObject
	{
		internal bool IsActiveInVisualTree { get; private set; }

		private Microsoft.UI.Composition.Compositor _elementVisualCompositor;
		private protected Microsoft.UI.Composition.Compositor ElementVisualCompositor
			=> _elementVisualCompositor ?? Microsoft.UI.Composition.Compositor.GetSharedCompositor();

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

			// Propagate VisualTree to ContextFlyout (matches WinUI UIElement::EnterImpl
			// which calls Enter on the ContextFlyout when entering the tree).
			if (ContextFlyout is { } contextFlyout && this.GetVisualTree() is { } visualTree)
			{
				contextFlyout.SetVisualTree(visualTree);
			}

			// Re-propagate DataContext to mentored children (e.g., ContextFlyout).
			// Their DataContext may have been cleared during a previous unload cycle.
			((DependencyObject)this).RepropagateMentoredChildrenDataContext();

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

			// Clear inherited DataContext on mentored children (e.g., ContextFlyout)
			// to break the reference chain FlyoutBase → DataContext → ViewModel.
			// This prevents memory leaks when shared flyouts outlive their placement targets.
			((DependencyObject)this).ClearMentoredChildrenDataContext();

			OnFwEltUnloaded();
			UpdateHitTest();
		}

#if __SKIA__
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
			else if (child.IsActiveInVisualTree)
			{
				var context = this.GetContext();
				var eventManager = context.EventManager;
				eventManager.RequestRaiseLoadedEventOnNextTick();
			}
		}

		private void OnChildRemoved(UIElement child)
		{
			child.Shutdown();
			(child as DependencyObject)?.ClearInheritedDataContext();

			var leaveParams = new LeaveParams(IsActiveInVisualTree)
			{
				SkipNameRegistration = SkipNameRegistrationForChildren,
			};

			// The owner is resolved from this parent, which is still in the tree - the child's own
			// parent pointer is already cleared by the time we get here (WinUI's CDOCollection::RemoveAt
			// captures GetStandardNameScopeOwner() before it unparents too).
			child.Leave(GetStandardNameScopeOwner(), leaveParams);
		}
#endif

		internal Point GetPosition(Point position, UIElement relativeTo)
			=> TransformToVisual(relativeTo).TransformPoint(position);

		// MUX Reference: CDOCollection::ChildEnter (DOCollection.cpp:313).
		private void ChildEnter(UIElement child, DependencyObject namescopeOwner, EnterParams @params)
		{
			// TODO Uno: WinUI precedes the live pass with a dead pass that registers names
			// (skipped when params.fSkipNameRegistration). It arrives with registration on Enter,
			// spec 058 step 9 - until then the walk would have nothing to do.
			if (@params.IsLive)
			{
				// Compute from the parent's persisted Depth, never from @params.Depth - @params is
				// threaded through property, resource and flyout walks where its Depth may be stale.
				@params.Depth = this.Depth + 1;

				// The names were registered by the dead pass, so the live one never re-registers.
				@params.SkipNameRegistration = true;
				child.Enter(namescopeOwner, @params);
			}
			else if (@params.IsForKeyboardAccelerator)
			{
				// Dead enter to propagate keyboard accelerator registration through the subtree.
				@params.Depth = int.MinValue;
				child.Enter(namescopeOwner, @params);
			}
		}

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
