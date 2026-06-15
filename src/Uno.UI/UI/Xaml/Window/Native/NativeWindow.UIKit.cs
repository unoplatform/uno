#nullable enable
using System;
using System.Linq;
using CoreGraphics;
using Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using UIKit;
using Uno.Collections;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI.Extensions;
using Windows.UI.ViewManagement;
using Windows.UI.Core;
using Windows.Foundation;
using Windows.System;

#if !__TVOS__
using WebKit;
#endif

namespace Uno.UI.Controls;

/// <summary>
/// A UIWindow which handle the focus item.
/// </summary>
public partial class Window : UIWindow
{
	private static readonly WeakAttachedDictionary<UIView, string> _attachedProperties = new WeakAttachedDictionary<UIView, string>();
	private const string NeedsKeyboardAttachedPropertyKey = "NeedsKeyboard";
	private const int KeyboardMargin = 10;

	private UIView? _focusedView; // Not really the "focused", but the last view which was touched.
	private WeakReference<UIScrollView?>? _scrollViewModifiedForKeyboard;
	private InputPane _inputPane;

	internal event Action? FrameChanged;

	/// <summary>
	/// ctor.
	/// </summary>
	public Window()
		: base(UIScreen.MainScreen.Bounds)
	{
		_inputPane = InputPane.GetForCurrentView();
		_inputPane.Window = this;

#if !__TVOS__
		UIKeyboard.Notifications.ObserveWillShow(OnKeyboardWillShow);
		UIKeyboard.Notifications.ObserveWillHide(OnKeyboardWillHide);
#endif
		UIApplication.Notifications.ObserveDidEnterBackground(OnApplicationEnteredBackground);
		UIApplication.Notifications.ObserveContentSizeCategoryChanged(OnContentSizeCategoryChanged);

		//NSNotificationCenter.DefaultCenter.AddObserver(UIKeyboard.WillShowNotification, OnKeyboardWillShow);

		FocusedViewBringIntoViewOnKeyboardOpensMode = BringIntoViewMode.BottomRightOfViewPort;
		FocusedViewBringIntoViewOnKeyboardOpensPadding = 20;
	}

	public override void PressesEnded(NSSet<UIPress> presses, UIPressesEvent evt)
	{
		base.PressesEnded(presses, evt);
	}

	public override void PressesBegan(NSSet<UIPress> presses, UIPressesEvent evt)
	{
		var handled = false;

		var focusInputHandler = Uno.UI.Xaml.Core.CoreServices.Instance.MainRootVisual?.AssociatedVisualTree?.UnoFocusInputHandler;
		if (Uno.WinRTFeatureConfiguration.Focus.EnableExperimentalKeyboardFocus && focusInputHandler != null)
		{
			foreach (UIPress press in presses)
			{
				if (press.Key is null)
				{
					continue;
				}

				if (press.Key.KeyCode == UIKeyboardHidUsage.KeyboardTab)
				{
					var shift =
						press.Key.ModifierFlags.HasFlag(UIKeyModifierFlags.AlphaShift) ||
						press.Key.ModifierFlags.HasFlag(UIKeyModifierFlags.Shift);
					handled |= focusInputHandler.TryHandleTabFocus(shift);
				}
				else if (press.Key.KeyCode == UIKeyboardHidUsage.KeyboardLeftArrow)
				{
					handled |= focusInputHandler.TryHandleDirectionalFocus(Windows.System.VirtualKey.Left);
				}
				else if (press.Key.KeyCode == UIKeyboardHidUsage.KeyboardRightArrow)
				{
					handled |= focusInputHandler.TryHandleDirectionalFocus(Windows.System.VirtualKey.Right);
				}
				else if (press.Key.KeyCode == UIKeyboardHidUsage.KeyboardUpArrow)
				{
					handled |= focusInputHandler.TryHandleDirectionalFocus(Windows.System.VirtualKey.Up);
				}
				else if (press.Key.KeyCode == UIKeyboardHidUsage.KeyboardDownArrow)
				{
					handled |= focusInputHandler.TryHandleDirectionalFocus(Windows.System.VirtualKey.Down);
				}
			}
		}
		if (!handled)
		{
			base.PressesBegan(presses, evt);
		}
	}

	/// <summary>
	/// The behavior to use to bring the focused item into view when opening the keyboard.
	/// Null means that auto bring into view on keyboard opening is disabled.
	/// </summary>
	public BringIntoViewMode? FocusedViewBringIntoViewOnKeyboardOpensMode { get; set; }

	/// <summary>
	/// The padding to add at top or bottom when bringing the focused item in the view when opening the keyboard.
	/// </summary>
	public int FocusedViewBringIntoViewOnKeyboardOpensPadding { get; set; }

	private void OnApplicationEnteredBackground(object? sender, NSNotificationEventArgs e)
	{
		try
		{
			_focusedView?.EndEditing(true);
		}
		catch (Exception ex)
		{
			// The app must not crash if any managed exception happens in the
			// native callback
			Application.Current.RaiseRecoverableUnhandledException(ex);
		}
	}

	private void OnContentSizeCategoryChanged(object? sender, UIContentSizeCategoryChangedEventArgs e)
	{
		try
		{
			var scalableViews = this.FindSubviewsOfType<IFontScalable>(int.MaxValue);
			scalableViews.ForEach(v => v.RefreshFont());

			// Update text scale factor from OS
			Uno.UI.Xaml.Core.CoreServices.Instance.UpdateFontScale(global::Windows.UI.ViewManagement.UISettings.GetTextScaleFactorValue());
		}
		catch (Exception ex)
		{
			// The app must not crash if any managed exception happens in the
			// native callback
			Application.Current.RaiseRecoverableUnhandledException(ex);
		}
	}

	public override UIView? HitTest(CGPoint point, UIEvent? uievent)
	{
		return base.HitTest(point, uievent);
	}

#if !__TVOS__
	private void OnKeyboardWillShow(object? sender, UIKeyboardEventArgs e)
	{
		try
		{
			if (e.Notification.UserInfo is null)
			{
				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().Debug("[OnKeyboardWillShow] Notification UserInfo was null");
				}

				return;
			}

			_inputPane.OccludedRect = ((NSValue?)e.Notification.UserInfo.ObjectForKey(UIKeyboard.FrameEndUserInfoKey))?.CGRectValue ?? default;
		}
		catch (Exception ex)
		{
			// The app must not crash if any managed exception happens in the
			// native callback
			Application.Current.RaiseRecoverableUnhandledException(ex);
		}
	}

	private void OnKeyboardWillHide(object? sender, UIKeyboardEventArgs e)
	{
		try
		{
			_inputPane.OccludedRect = new Rect(0, 0, 0, 0);
		}
		catch (Exception ex)
		{
			// The app must not crash if any managed exception happens in the
			// native callback
			Application.Current.RaiseRecoverableUnhandledException(ex);
		}
	}
#endif

	internal void MakeFocusedViewVisible(bool isOpeningKeyboard = false)
	{
		if (IsWithinAWebView(_focusedView))
		{
			RestoreFocusedViewVisibility(); // Sanity
		}
		else
		{
			// Get the actual focused element in case the element gets its focus programmatically, rather than a hit test
			var xamlRoot = Microsoft.UI.Xaml.Window.InitialWindow?.Content?.XamlRoot;
			UIView? view = null;
			if (xamlRoot is not null)
			{
				view = FocusManager.GetFocusedElement(xamlRoot) as UIView;
			}
			view ??= _focusedView;

			if (view is not null)
			{
				MakeVisible(view, FocusedViewBringIntoViewOnKeyboardOpensMode, useForcedAnimation: isOpeningKeyboard);
			}
		}
	}

	internal void RestoreFocusedViewVisibility()
	{
		// When keyboard disappear, ensure to restore the scroll state to avoid empty space at the bottom of the screen
		try
		{
			if (_scrollViewModifiedForKeyboard != null
				&& _scrollViewModifiedForKeyboard.TryGetTarget(out var scrollView))
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

	internal void MakeVisible(UIView? view, BringIntoViewMode? bringIntoViewMode, bool useForcedAnimation = false)
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

		_scrollViewModifiedForKeyboard = new WeakReference<UIScrollView?>(scrollView);

		if (scrollView == null)
		{
			this.Log().Warn("Keyboard will show, but we cannot find any ScrollView with enough space for the currently focused view, so it's impossible to ensure that it's visible.");
			return;
		}

		var scrollViewRectInWindow = ConvertRectFromView(scrollView.Bounds, scrollView);

		var keyboardTop = (nfloat)_inputPane.OccludedRect.Top;

		// if Keyboard.Top is not greater than zero, then this means there is not visible keyboard on the screen]
		// Doing so will avoid the scrollviewer to animate on platforms allowing hardware keyboard input (Simulator, iPad, Catalyst).
		if (keyboardTop <= 0)
		{
			return;
		}

		var keyboardOverlap = scrollViewRectInWindow.Bottom - keyboardTop;
		if (keyboardOverlap > 0)
		{
			scrollView.ContentInset = new UIEdgeInsets(0f, 0f, keyboardOverlap, 0f);
		}

		var viewRectInScrollView = CGRect.Empty;

		//if the view is a multilineTextBox, we want to based our ScrollRectToVisible logic on caret position not on the bottom of the multilineTextBox view
		var multilineTextBoxView = view as Microsoft.UI.Xaml.Controls.MultilineTextBoxView;
		if (multilineTextBoxView == null)
		{
			multilineTextBoxView = (view as TextBox)?.MultilineTextBox;
		}
		if (multilineTextBoxView != null && multilineTextBoxView.IsFirstResponder)
		{
			using var range = ObjCRuntime.Runtime.GetNSObject<UITextRange>(multilineTextBoxView.SelectedTextRange);
			viewRectInScrollView = multilineTextBoxView.GetCaretRectForPosition(range?.Start);

			// We need to add an additional margins because the caret is too tight to the text. The font is cutoff under the keyboard.
			viewRectInScrollView.Y -= KeyboardMargin;
			viewRectInScrollView.Height += 2 * KeyboardMargin;

			viewRectInScrollView = view.ConvertRectToView(viewRectInScrollView, scrollView);
		}
		else
		{
			if (view is not TextBox)
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

	private static bool GetNeedsKeyboard(UIView? view)
	{
		if (view == null)
		{
			return false;
		}

		var superViews = view.FindSuperviews().Trim().ToList();
		superViews.Insert(0, view);
		return superViews.Any(superView => _attachedProperties.GetValue(superView, NeedsKeyboardAttachedPropertyKey, () => default(bool?)).GetValueOrDefault());
	}

	private static bool NeedsKeyboard(UIView? view)
	{
		return view is UISearchBar
			|| view is UITextView
			|| view is UITextField
			|| GetNeedsKeyboard(view);
	}

	private bool IsWithinAWebView(UIView? view)
	{
		return
#if __TVOS__
			false;
#else
			view?.FindSuperviewOfType<UIWebView>(stopAt: this) != null ||
			view?.FindSuperviewOfType<WKWebView>(stopAt: this) != null;
#endif
	}
}
