using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Windows.UI.Input;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Foundation;
using UIKit;
using Uno.UI.Extensions;
using WebKit;

namespace Windows.UI.Xaml
{
	partial class UIElement
	{
		private IEnumerable<TouchesManager> _parentsTouchesManager;
		private bool _isManipulating;

		partial void InitializePointersPartial()
		{
			RegisterLoadActions(PrepareParentTouchesManagers, ReleaseParentTouchesManager);
		}

		#region Native touch handling (i.e. source of the pointer / gesture events)
		public override void TouchesBegan(NSSet touches, UIEvent evt)
		{
			/* Note: Here we have a mismatching behavior with UWP, if the events bubble natively we're going to get
					 (with Ctrl_02 is a child of Ctrl_01):
							Ctrl_02: Entered
									 Pressed
							Ctrl_01: Entered
									 Pressed

					While on UWP we will get:
							Ctrl_02: Entered
							Ctrl_01: Entered
							Ctrl_02: Pressed
							Ctrl_01: Pressed

					However, to fix this is would mean that we handle all events in managed code, but this would
					break lots of control (ScrollViewer) and ability to easily integrate an external component.
			*/

			try
			{
				var isPointerOver = evt.IsTouchInView(this);
				IsPointerOver = isPointerOver;

				var pointerEventIsHandledOrBubblingInManaged = false;
				if (isPointerOver)
				{
					NotifyParentTouchesManagersManipulationStarted();

					pointerEventIsHandledOrBubblingInManaged = RaiseNativelyBubbledDown(new PointerRoutedEventArgs(touches, evt, this));
				}

				if (!pointerEventIsHandledOrBubblingInManaged)
				{
					// Continue native bubbling up of the event
					base.TouchesBegan(touches, evt);
				}
			}
			catch (Exception e)
			{
				Application.Current.RaiseRecoverableUnhandledException(e);
			}
		}

		public override void TouchesMoved(NSSet touches, UIEvent evt)
		{
			try
			{
				var isPointerOver = evt.IsTouchInView(this);
				IsPointerOver = isPointerOver;

				var pointerEventIsHandledInManaged = false;
				if (IsPointerCaptured || isPointerOver)
				{
					pointerEventIsHandledInManaged = RaiseNativelyBubbledMove(new PointerRoutedEventArgs(touches, evt, this));
				}

				if (!pointerEventIsHandledInManaged)
				{
					// Continue native bubbling up of the event
					base.TouchesMoved(touches, evt);
				}
			}
			catch (Exception e)
			{
				Application.Current.RaiseRecoverableUnhandledException(e);
			}
		}

		public override void TouchesEnded(NSSet touches, UIEvent evt)
		{
			try
			{
				var wasPointerOver = IsPointerOver;
				var isPointerOver = false;
				IsPointerOver = isPointerOver;

				var pointerEventIsHandledOrBubblingInManaged = false;
				if (IsPointerCaptured || wasPointerOver)
				{
					pointerEventIsHandledOrBubblingInManaged = RaiseNativelyBubbledUp(new PointerRoutedEventArgs(touches, evt, this));
				}

				if (!pointerEventIsHandledOrBubblingInManaged)
				{
					// Continue native bubbling up of the event
					base.TouchesEnded(touches, evt);
				}

				NotifyParentTouchesManagersManipulationEnded();
			}
			catch (Exception e)
			{
				Application.Current.RaiseRecoverableUnhandledException(e);
			}
		}

		public override void TouchesCancelled(NSSet touches, UIEvent evt)
		{
			try
			{
				var wasPointerOver = IsPointerOver;
				var isPointerOver = false;
				IsPointerOver = isPointerOver;

				var pointerEventIsHandledOrBubblingInManaged = false;
				if (IsPointerCaptured || wasPointerOver)
				{
					pointerEventIsHandledOrBubblingInManaged = RaiseNativelyBubbledLost(new PointerRoutedEventArgs(touches, evt, this));
				}

				if (!pointerEventIsHandledOrBubblingInManaged)
				{
					// Continue native bubbling up of the event
					base.TouchesCancelled(touches, evt);
				}

				NotifyParentTouchesManagersManipulationEnded();
			}
			catch (Exception e)
			{
				Application.Current.RaiseRecoverableUnhandledException(e);
			}
		}
		#endregion

		#region TouchesManager (Alter the parents native scroll view to make sure to receive all touches)
		partial void OnManipulationModeChanged(ManipulationModes mode) => PrepareParentTouchesManagers(mode);

		// Loaded
		private void PrepareParentTouchesManagers() => PrepareParentTouchesManagers(ManipulationMode);
		private void PrepareParentTouchesManagers(ManipulationModes mode)
		{
			// 1. Make sure to end any pending manipulation
			ReleaseParentTouchesManager();

			// 2. If this control can  Walk the tree to detect all ScrollView and register our self as a manipulation listener
			if (mode != ManipulationModes.System)
			{
				_parentsTouchesManager = TouchesManager.GetAllParents(this).ToList();

				foreach (var manager in _parentsTouchesManager)
				{
					manager.RegisterChildListener();
				}
			}
		}

		// Unloaded
		private void ReleaseParentTouchesManager()
		{
			// 1. Make sure to end any pending manipulation
			NotifyParentTouchesManagersManipulationEnded();

			// 2. Un-register our self (so the SV can re-enable the delay)
			if (_parentsTouchesManager != null)
			{
				foreach (var manager in _parentsTouchesManager)
				{
					manager.UnRegisterChildListener();
				}

				_parentsTouchesManager = null; // prevent leak and disable manipulation started/ended reports
			}
		}

		private void NotifyParentTouchesManagersManipulationStarted()
		{
			if (!_isManipulating && (_parentsTouchesManager?.Any() ?? false))
			{
				_isManipulating = true;
				foreach (var manager in _parentsTouchesManager)
				{
					manager.ManipulationStarted();
				}
			}
		}

		private void NotifyParentTouchesManagersManipulationEnded()
		{
			if (_isManipulating && (_parentsTouchesManager?.Any() ?? false))
			{
				_isManipulating = false;
				foreach (var manager in _parentsTouchesManager)
				{
					manager.ManipulationEnded();
				}
			}
		}

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
					case ScrollContentPresenter presenter:
						manager = presenter.TouchesManager;
						return true;

					case UIScrollView scrollView:
						manager = _scrollViews.GetValue(scrollView, sv => new ScrollViewTouchesManager((UIScrollView)sv));
						return true;

					case UIWebView uiWebView:
						manager = _scrollViews.GetValue(uiWebView.ScrollView, sv => new ScrollViewTouchesManager((UIScrollView)sv));
						return true;

					case WKWebView wkWebView:
						manager = _scrollViews.GetValue(wkWebView.ScrollView, sv => new ScrollViewTouchesManager((UIScrollView)sv));
						return true;

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
				foreach (var parent in element.GetParents())
				{
					if (parent is UIView view && TryGet(view, out var manager))
					{
						yield return manager;
					}
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

			protected abstract void SetCanDelay(bool canDelay);

			protected abstract void SetCanCancel(bool canCancel);
		}

		private class ScrollViewTouchesManager : TouchesManager
		{
			private readonly UIScrollView _scrollView;

			public ScrollViewTouchesManager(UIScrollView scrollView)
			{
				_scrollView = scrollView;
			}

			/// <inheritdoc />
			protected override void SetCanDelay(bool canDelay)
				=> _scrollView.DelaysContentTouches = canDelay;

			/// <inheritdoc />
			protected override void SetCanCancel(bool canCancel)
				=> _scrollView.CanCancelContentTouches = canCancel;
		}
		#endregion

		private void ReleasePointerCaptureNative(Pointer value)
		{
		}
	}
}
