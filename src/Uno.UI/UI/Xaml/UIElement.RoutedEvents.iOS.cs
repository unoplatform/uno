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
		#region ManipulationMode (DP)
		public static readonly DependencyProperty ManipulationModeProperty = DependencyProperty.Register(
			"ManipulationMode",
			typeof(ManipulationModes),
			typeof(UIElement),
			new FrameworkPropertyMetadata(ManipulationModes.System, FrameworkPropertyMetadataOptions.None, OnManipulationModeChanged));

		private static void OnManipulationModeChanged(DependencyObject snd, DependencyPropertyChangedEventArgs args)
		{
			if (snd is UIElement elt)
			{
				elt.PrepareParentTouchesManagers((ManipulationModes)args.NewValue);
			}
		}

		public ManipulationModes ManipulationMode
		{
			get => (ManipulationModes)this.GetValue(ManipulationModeProperty);
			set => this.SetValue(ManipulationModeProperty, value);
		}
		#endregion

		private /* readonly */ Lazy<GestureRecognizer> _gestures;
		private IEnumerable<TouchesManager> _parentsTouchesManager;
		private bool _isManipulating;

		// ctor
		private void InitializePointers()
		{
			_gestures = new Lazy<GestureRecognizer>(CreateGestureRecognizer);
			RegisterLoadActions(PrepareParentTouchesManagers, ReleaseParentTouchesManager);
		}

		private GestureRecognizer CreateGestureRecognizer()
		{
			var recognizer = new GestureRecognizer();

			recognizer.Tapped += OnTapRecognized;

			return recognizer;

			void OnTapRecognized(GestureRecognizer sender, TappedEventArgs args)
			{
				if (args.TapCount == 1)
				{
					RaiseEvent(TappedEvent, new TappedRoutedEventArgs(args.PointerDeviceType, args.Position));
				}
				else // i.e. args.TapCount == 2
				{
					RaiseEvent(DoubleTappedEvent, new DoubleTappedRoutedEventArgs(args.PointerDeviceType, args.Position));
				}
			}
		}

		#region Add/Remove handler (This should be moved in the shared file once all platform use the GestureRecognizer)
		partial void AddHandlerPartial(RoutedEvent routedEvent, int handlersCount, object handler, bool handledEventsToo)
		{
			if (handlersCount == 1)
			{
				// If greater than 1, it means that we already enabled the setting (and if lower than 0 ... it's weird !)
				ToggleGesture(routedEvent);
			}
		}

		partial void RemoveHandlerPartial(RoutedEvent routedEvent, int remainingHandlersCount, object handler)
		{
			if (remainingHandlersCount == 0)
			{
				ToggleGesture(routedEvent);
			}
		}

		private void ToggleGesture(RoutedEvent routedEvent)
		{
			if (routedEvent == TappedEvent)
			{
				_gestures.Value.GestureSettings |= GestureSettings.Tap;
			}
			else if (routedEvent == DoubleTappedEvent)
			{
				_gestures.Value.GestureSettings |= GestureSettings.DoubleTap;
			}
		}
		#endregion

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
					pointerEventIsHandledOrBubblingInManaged = RaiseNativelyBubbledLost();
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

		#region Raise pointer events and gesture recognition (This should be moved in the shared file once all platform use the GestureRecognizer)
		private bool RaiseNativelyBubbledDown(PointerRoutedEventArgs args)
		{
			IsPointerPressed = true;

			args.Handled = false; // reset event
			var handledInManaged = RaiseEvent(PointerEnteredEvent, args);

			args.Handled = false; // reset event
			handledInManaged |= RaiseEvent(PointerPressedEvent, args);

			// Note: We process the DownEvent *after* the Raise(Pressed), so in case of DoubleTapped
			//		 the event is fired after
			if (_gestures.IsValueCreated)
			{
				// We need to process only events that are bubbling natively to this control,
				// if they are bubbling in managed it means that tey where handled a child control,
				// so we should not use them for gesture recognition.
				_gestures.Value.ProcessDownEvent(args.GetCurrentPoint(this));
			}

			return handledInManaged;
		}

		private bool RaiseNativelyBubbledMove(PointerRoutedEventArgs args)
		{
			args.Handled = false; // reset event
			var handledInManaged = RaiseEvent(PointerMovedEvent, args);

			if (_gestures.IsValueCreated)
			{
				// We need to process only events that are bubbling natively to this control,
				// if they are bubbling in managed it means that tey where handled a child control,
				// so we should not use them for gesture recognition.
				_gestures.Value.ProcessMoveEvents(args.GetIntermediatePoints(this));
			}

			return handledInManaged;
		}

		private bool RaiseNativelyBubbledUp(PointerRoutedEventArgs args)
		{
			IsPointerPressed = false;

			args.Handled = false; // reset event
			var handledInManaged = RaiseEvent(PointerReleasedEvent, args);

			// Note: We process the UpEvent between Release and Exited as the gestures like "Tap"
			//		 are fired between those events.
			if (_gestures.IsValueCreated)
			{
				// We need to process only events that are bubbling natively to this control,
				// if they are bubbling in managed it means that they where handled a child control,
				// so we should not use them for gesture recognition.
				_gestures.Value.ProcessUpEvent(args.GetCurrentPoint(this));
			}

			args.Handled = false; // reset event
			handledInManaged |= RaiseEvent(PointerExitedEvent, args);

			// On pointer up (and *after* the exited) we request to release an remaining captures
			ReleasePointerCaptures();

			return handledInManaged;
		}

		/// <summary>
		/// This occurs when the pointer is lost (e.g. when captured by a native control like the ScrollViewer)
		/// which prevents us to continue the touches handling.
		/// </summary>
		private bool RaiseNativelyBubbledLost()
		{
			// When a pointer is captured, we don't even receive "Released" nor "Exited"

			IsPointerPressed = false;

			if (_gestures.IsValueCreated)
			{
				_gestures.Value.CompleteGesture();
			}

			// On pointer up (and *after* the exited) we request to release an remaining captures
			ReleasePointerCaptures();

			return false;
		}
		#endregion

		#region Pointer capture handling
		/*
		 * About pointer capture
		 *
		 * - When a pointer is captured, it will still bubble up, but it will bubble up from the element
		 *   that captured the touch (so the a inner control won't receive it, even if under the pointer !)
		 *   !!! BUT !!! The OriginalSource will still be the inner control!
		 * - Captured are exclusive : first come, first served!
		 * - A control can capture a pointer, even if not under the pointer
		 *
		 */


		private void ReleasePointerCaptureNative(Pointer value)
		{
		}
		#endregion

		#region TouchesManager (Alter the parents native scroll view to make sure to receive all touches)
		// Loaded
		private void PrepareParentTouchesManagers() => PrepareParentTouchesManagers(ManipulationMode);
		private void PrepareParentTouchesManagers(ManipulationModes mode)
		{
			// 1. Make sure to end any pending manipulation
			ReleaseParentTouchesManager();

			// 2. If this control can  Walk the tree to detect all ScrollView and register our self as a manipulation listener
			if (mode == ManipulationModes.System)
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
	}
}
