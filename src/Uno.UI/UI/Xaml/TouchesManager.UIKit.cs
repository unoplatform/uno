using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml.Controls;
using UIKit;
#if !__TVOS__
using WebKit;
#endif

using Microsoft.UI.Input;

namespace Microsoft.UI.Xaml
{
	/// <summary>
	/// By default the UIScrollView will delay the touches to the content until it detects
	/// if the manipulation is a drag.And even there, if it detects that the manipulation
	///	* is a Drag, it will cancel the touches on content and handle them internally
	/// (i.e.Touches[Began|Moved|Ended] will no longer be invoked on SubViews).
	/// cf.https://developer.apple.com/documentation/uikit/uiscrollview
	///
	/// The "TouchesManager" give the ability to any child UIElement to alter this behavior
	///	if it needs to handle the gestures itself (e.g.the Thumb of a Slider / ToggleSwitch).
	/// 
	/// On the UIElement this is defined by the ManipulationMode
	/// </summary>
	internal abstract class TouchesManager
	{
		private static readonly ConditionalWeakTable<UIView, ScrollViewTouchesManager> _scrollViews = new ConditionalWeakTable<UIView, ScrollViewTouchesManager>();

		/// <summary>
		/// Tries to get the current <see cref="TouchesManager"/> for the given view
		/// </summary>
		public static bool TryGet(UIView view, out TouchesManager manager)
		{
			switch (view)
			{
				// Custom managers
				case NativeScrollContentPresenter presenter:
					manager = presenter.TouchesManager;
					return true;

				case NativeListViewBase listView:
					manager = listView.TouchesManager;
					return true;

				// Generic manager
				case UIScrollView scrollView:
					manager = _scrollViews.GetValue(scrollView, sv => new ScrollViewTouchesManager((UIScrollView)sv));
					return true;

				// Redirections to nested native scrollable contents
				case ListViewBase listView:
					manager = listView.NativePanel?.TouchesManager; // We only propagates the touches manager of the nested native ListView/UICollectionView
					return manager is not null;
#if !__TVOS__
#if !__MACCATALYST__
				case UIWebView uiWebView:
					manager = _scrollViews.GetValue(uiWebView.ScrollView, sv => new ScrollViewTouchesManager((UIScrollView)sv));
					return true;
#endif

				case WKWebView wkWebView:
					manager = _scrollViews.GetValue(wkWebView.ScrollView, sv => new ScrollViewTouchesManager((UIScrollView)sv));
					return true;
#endif
				default:
					manager = default;
					return false;
			}
		}

		/// <summary>
		/// Gets all the <see cref="TouchesManager"/> of the parents hierarchy
		/// </summary>
		public static IEnumerable<TouchesManager> GetAllParents(UIElement element)
		{
			foreach (var parent in GetAllParentViews(element))
			{
				if (TryGet(parent, out var manager))
				{
					yield return manager;
				}
			}
		}

		private static IEnumerable<UIView> GetAllParentViews(UIView current)
		{
			while (current != null)
			{
				// Navigate upward using the managed shadowed visual tree
				using (var parents = current.GetParents().GetEnumerator())
				{
					while (parents.MoveNext())
					{
						if (parents.Current is UIView view)
						{
							yield return current = view;
						}
					}
				}

				// When reaching a UIView, fallback to the native visual tree until the next DependencyObject
				do
				{
					yield return current = current.Superview;
				} while (current != null && !(current is DependencyObject));
			}
		}

		/// <summary>
		/// The number of children that are listening to touches events for manipulations
		/// </summary>
		public int Listeners { get; private set; }

		/// <summary>
		/// The number of children that are currently handling a manipulation
		/// </summary>
		public int ActiveListeners { get; private set; }

		/// <summary>
		/// Notify the owner of this touches manager that a child is listening to touches events for manipulations
		/// (so the owner should disable any delay for touches propagation)
		/// </summary>
		/// <remarks>The caller MUST also call <see cref="UnRegisterChildListener"/> once completed.</remarks>
		public void RegisterChildListener()
		{
			if (Listeners++ == 0)
			{
				SetCanDelay(false);
			}
		}

		/// <summary>
		/// Un-register a child listener
		/// </summary>
		public void UnRegisterChildListener()
		{
			if (--Listeners == 0)
			{
				SetCanDelay(true);
			}
		}

		/// <summary>
		/// Indicates that a child listener is starting to track a manipulation
		/// (so the owner should try to not cancel the touches propagation for interactions that are supported by the given manipulation object)
		/// </summary>
		/// <remarks>If this method returns true, the caller MUST also call <see cref="ManipulationEnded"/> once completed (or cancelled).</remarks>
		public bool ManipulationStarting(GestureRecognizer.Manipulation manipulation)
		{
			if (CanConflict(manipulation))
			{
				ManipulationStarted();
				return true;
			}
			else
			{
				return false;
			}
		}

		/// <summary>
		/// Indicates that a child listener has started to track a manipulation
		/// (so the owner should not cancel the touches propagation)
		/// </summary>
		/// <remarks>The caller MUST also call <see cref="ManipulationEnded"/> once completed (or cancelled).</remarks>
		public void ManipulationStarted()
		{
			if (ActiveListeners++ == 0)
			{
				SetCanCancel(false);
			}
		}

		/// <summary>
		/// Indicates the end (success or failure) of a manipulation tracking
		/// </summary>
		public void ManipulationEnded()
		{
			if (--ActiveListeners == 0)
			{
				SetCanCancel(true);
			}
		}

		protected abstract bool CanConflict(GestureRecognizer.Manipulation manipulation);

		protected abstract void SetCanDelay(bool canDelay);

		protected abstract void SetCanCancel(bool canCancel);

		// Touches manager for generic scrollable content
		private class ScrollViewTouchesManager : TouchesManager
		{
			private readonly UIScrollView _scrollView;

			public ScrollViewTouchesManager(UIScrollView scrollView)
			{
				_scrollView = scrollView;
			}

			/// <inheritdoc />
			protected override bool CanConflict(GestureRecognizer.Manipulation manipulation)
				=> manipulation.IsTranslateXEnabled
					|| manipulation.IsTranslateYEnabled
					|| manipulation.IsDragManipulation; // This will actually always be false when CanConflict is being invoked in current setup.

			/// <inheritdoc />
			protected override void SetCanDelay(bool canDelay)
				=> _scrollView.DelaysContentTouches = canDelay;

			/// <inheritdoc />
			protected override void SetCanCancel(bool canCancel)
				=> _scrollView.CanCancelContentTouches = canCancel;
		}
	}
}
