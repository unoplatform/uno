using System;
using System.Drawing;
using Uno.Disposables;
using Uno.Extensions;
using Uno.UI.Extensions;
using System.Linq;
using Windows.UI.Core;
using Uno.Diagnostics.Eventing;
using Foundation;
using UIKit;
using CoreGraphics;
using Uno.Collections;
using Windows.UI.Xaml.Input;
using WebKit;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml;
using Uno.UI.Controls;
using Uno.Logging;
using Windows.Foundation;
using Windows.UI.ViewManagement;

namespace Uno.UI.Controls
{
	/// <summary>
	/// A UIWindow which handle the focus item.
	/// </summary>
	public partial class Window : UIWindow
	{
		//Impediment # 19821 A way the bypass the processing done in the HitTest override when third party controls are used.
		public static bool BypassCheckToCloseKeyboard { get; set; }

		private readonly static IEventProvider _trace = Tracing.Get(TraceProvider.Id);

		private static readonly WeakAttachedDictionary<UIView, string> _attachedProperties = new WeakAttachedDictionary<UIView, string>();
		private const string NeedsKeyboardAttachedPropertyKey = "NeedsKeyboard";
		private const int KeyboardMargin = 10;

		public static class TraceProvider
		{
			public readonly static Guid Id = Guid.Parse("{64C590CE-CC02-4B48-BFC9-754A47571AC1}");

			public const int Window_TouchStart = 1;
			public const int Window_TouchStop = 2;
		}

		private UIView _focusedView; // Not really the "focused", but the last view which was touched.
		private WeakReference<UIScrollView> _scrollViewModifiedForKeyboard;
		private InputPane _inputPane;
		private EventProviderExtensions.DisposableEventActivity _touchTrace;

		internal event Action FrameChanged;

		/// <summary>
		/// ctor.
		/// </summary>
		public Window()
			: base(UIScreen.MainScreen.Bounds)
		{
			_inputPane = InputPane.GetForCurrentView();
			_inputPane.Window = this;

			UIKeyboard.Notifications.ObserveWillShow(OnKeyboardWillShow);
			UIKeyboard.Notifications.ObserveWillHide(OnKeyboardWillHide);
			UIApplication.Notifications.ObserveDidEnterBackground(OnApplicationEnteredBackground);
			UIApplication.Notifications.ObserveContentSizeCategoryChanged(OnContentSizeCategoryChanged);

			//NSNotificationCenter.DefaultCenter.AddObserver(UIKeyboard.WillShowNotification, OnKeyboardWillShow);

			FocusedViewBringIntoViewOnKeyboardOpensMode = BringIntoViewMode.BottomRightOfViewPort;
			FocusedViewBringIntoViewOnKeyboardOpensPadding = 20;
		}


		/// <summary>
		/// The behavior to use to bring the focused item into view when opening the keyboard.
		/// Null means that auto bring into view on keayboard opening is disabled.
		/// </summary>
		public BringIntoViewMode? FocusedViewBringIntoViewOnKeyboardOpensMode { get; set; }

		/// <summary>
		/// The padding to add at top or bottom when bringing the focused item in the view when opening the keyboard.
		/// </summary>
		public int FocusedViewBringIntoViewOnKeyboardOpensPadding { get; set; }

		private void OnApplicationEnteredBackground(object sender, NSNotificationEventArgs e)
		{
			_focusedView?.EndEditing(true);
		}

		private void OnContentSizeCategoryChanged(object sender, UIContentSizeCategoryChangedEventArgs e)
		{
			var scalableViews = this.FindSubviewsOfType<IFontScalable>(int.MaxValue);
			scalableViews.ForEach(v => v.RefreshFont());
		}

		public override UIView HitTest(CGPoint point, UIEvent uievent)
		{
			if (!BypassCheckToCloseKeyboard &&
				uievent != null && uievent.Type == UIEventType.Touches)
			{
				_touchTrace = _trace.WriteEventActivity(TraceProvider.Window_TouchStart, TraceProvider.Window_TouchStop);

				if (UIDevice.CurrentDevice.CheckSystemVersion(8, 0) && _inputPane.Visible && point.Y > _inputPane.OccludedRect.Y)
				{
					// For some strange reasons, on iOS 8, when the app is sent in background (while the keyboard is open ?) and then restored,
					// when user re-opens the keyboard we will get some HitTest also for touches which are made upon the keyboard.
					// In this case we have to send back the currently focused view in order to prevent EndEditing which will cause 
					// the keyboard to disappear and the feeling that touches are going "through the keyboard".

					return _focusedView;
				}

				var previouslyFocusedView = _focusedView;
				var newlyFocusedView = base.HitTest(point, uievent);
				_focusedView = newlyFocusedView;

				if (_inputPane.Visible
					&& !NeedsKeyboard(_focusedView))
				{
					// Note: prefer call the IsWithinAWebView after the NeedsKeyboard since it is more power consuming.
					if (IsWithinAWebView(_focusedView))
					{
						// As soon as we are in a WebView the NeedsKeyboard will always returns false. So here we will complete
						// edition as soon as the user touch anywhere on the screen. This is acceptable since there are dedicated
						// buttons on the keyboard to focus the next/previous fields of a form.
						previouslyFocusedView?.EndEditing(true);
					}
					else if (previouslyFocusedView != null && IsFocusable(_focusedView))
					{
						// If not in a WebView or focusable button, stop editing on the whole window in order to close the keyboard even it the focused view
						// is no more the one which requires the keyboard (e.g. swipe on another TextBox will not close the keyboard
						// neither begin edit, but will "focus" it)
						this.EndEditing(true); // Propagated to all children, will close the keyboard
					}
				}

				return _focusedView ?? base.HitTest(point, uievent);
			}
			else
			{
				if(_touchTrace != null)
				{
					_touchTrace.Dispose();
					_touchTrace = null;
				}

				return base.HitTest(point, uievent);
			}
		}

		private void OnKeyboardWillShow(object sender, UIKeyboardEventArgs e)
		{
			var keyboardRect = ((NSValue)e.Notification.UserInfo.ObjectForKey(UIKeyboard.BoundsUserInfoKey)).RectangleFValue;
			var windowRect = Windows.UI.Xaml.Window.Current.Bounds;
			_inputPane.OccludedRect = new Rect(0, windowRect.Height - keyboardRect.Height, keyboardRect.Width, keyboardRect.Height);
		}

		private void OnKeyboardWillHide(object sender, UIKeyboardEventArgs e)
		{
			_inputPane.OccludedRect = new Rect(0, 0, 0, 0);
		}

		internal void MakeFocusedViewVisible(bool isOpeningKeyboard = false)
		{
			if (IsWithinAWebView(_focusedView))
			{
				RestoreFocusedViewVisibility(); // Sanity
			}
			else
			{
				// Get the actual focused element in case the element gets its focus programmatically, rather than a hit test
				var view = FocusManager.GetFocusedElement() as UIView ?? _focusedView;
				MakeVisible(view, FocusedViewBringIntoViewOnKeyboardOpensMode, useForcedAnimation: isOpeningKeyboard);
			}
		}

		internal void RestoreFocusedViewVisibility()
		{
			// When keyboard disappear, ensure to restore the scroll state to avoid empty space at the bottom of the screen
			try
			{
				UIScrollView scrollView;
				if (_scrollViewModifiedForKeyboard != null
					&& _scrollViewModifiedForKeyboard.TryGetTarget(out scrollView))
				{
					scrollView.ContentInset = UIEdgeInsets.Zero;
					scrollView.ClearCustomScrollOffset(animationMode: ScrollViewExtensions.ScrollingMode.Forced);
				}
			}
			catch (ObjectDisposedException)
			{
			}
			catch (Exception)
			{
				this.Log().Warn("Failed to restore scrolling state");
			}
		}

		internal void MakeVisible(UIView view, BringIntoViewMode? bringIntoViewMode, bool useForcedAnimation = false)
		{
			if (view == null)
			{
				return;
			}

			if (bringIntoViewMode == null)
			{
				return;
			}

			var scrollView = view.FindSuperviewsOfType<UIScrollView>().LastOrDefault();
			_scrollViewModifiedForKeyboard = new WeakReference<UIScrollView>(scrollView);

			if (scrollView == null)
			{
				this.Log().Warn("Keyboard will show, but we cannot find any ScrollView with enough space for the currently focused view, so it's impossible to ensure that it's visible.");
				return;
			}

			var scrollViewRectInWindow = ConvertRectFromView(scrollView.Bounds, scrollView);

			var keyboardTop = (nfloat)_inputPane.OccludedRect.Top;

			var keyboardOverlap = scrollViewRectInWindow.Bottom - keyboardTop;
			if (keyboardOverlap > 0)
			{
				scrollView.ContentInset = new UIEdgeInsets(0, 0, keyboardOverlap, 0);
			}

			var viewRectInScrollView = CGRect.Empty;

			//if the view is a multilineTextBox, we want to based our ScrollRectToVisible logic on caret position not on the bottom of the multilineTextBox view 
			var multilineTextBoxView = view as Windows.UI.Xaml.Controls.MultilineTextBoxView;
			if (multilineTextBoxView == null)
			{
				multilineTextBoxView = (view as TextBox)?.MultilineTextBox;
			}
			if (multilineTextBoxView != null && multilineTextBoxView.IsFirstResponder)
			{
				viewRectInScrollView = multilineTextBoxView.GetCaretRectForPosition(multilineTextBoxView.SelectedTextRange.Start);

				// We need to add an additional margins because the caret is too tight to the text. The font is cutoff under the keyboard.
				viewRectInScrollView.Y -= KeyboardMargin;
				viewRectInScrollView.Height += 2 * KeyboardMargin;

				viewRectInScrollView = view.ConvertRectToView(viewRectInScrollView, scrollView);
			}
			else
			{
				if (!(view is TextBox))
				{
					//We want to scroll to the textbox and not the inner textview.
					view = view.FindFirstParent<TextBox>() ?? view;
				}

				viewRectInScrollView = scrollView.ConvertRectFromView(view.Bounds, view);
			}

			scrollView.ScrollRectToVisible(viewRectInScrollView, !useForcedAnimation);
		}

		public override CGRect Frame
		{
			get
			{
				return base.Frame;
			}

			set
			{
				var frameChanged = base.Frame != value;

				base.Frame = value;

				if (frameChanged)
				{
					// Trigger a measure pass when the size of the Window changes (e.g., multi-app, orientation change).
					// This ensures that all elements are measured properly (as SetNeedsLayout isn't called).
					this.FindSubviewsOfType<FrameworkElement>()
						.FirstOrDefault()?
						.SizeThatFits(Frame.Size);

					FrameChanged?.Invoke();
				}
			}
		}

		public static void SetNeedsKeyboard(UIView view, bool needsKeyboard)
		{
			if (view != null)
			{
				_attachedProperties.SetValue(view, NeedsKeyboardAttachedPropertyKey, (bool?)needsKeyboard);
			}
		}

		private static bool GetNeedsKeyboard(UIView view)
		{
			var superViews = view.FindSuperviews().ToList();
			superViews.Insert(0, view);

			return superViews.Any(superView => _attachedProperties.GetValue(superView, NeedsKeyboardAttachedPropertyKey, () => default(bool?)).GetValueOrDefault());
		}

		private static bool NeedsKeyboard(UIView view)
		{
			return view is UISearchBar
				|| view is UITextView
				|| view is UITextField
				|| GetNeedsKeyboard(view);
		}

		private bool IsWithinAWebView(UIView view)
		{
			return view?.FindSuperviewOfType<UIWebView>(stopAt: this) != null
				|| view?.FindSuperviewOfType<WKWebView>(stopAt: this) != null;
		}

		private bool IsFocusable(UIView view)
		{
			// Basic IsFocusable support that only works with buttons.
			// This prevent the keyboard from being dismissed when tapping on a button that doesn't want focus.
			return ((_focusedView as ButtonBase) ?? _focusedView?.FindFirstParent<ButtonBase>())?.IsFocusable
				?? true; // if it's not a button, we fallback to the default behavior which is to dismiss the keyboard
		}
	}
}
