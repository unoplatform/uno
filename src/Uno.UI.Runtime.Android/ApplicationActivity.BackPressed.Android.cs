#nullable enable

using System;
using System.Linq;
using Android.OS;
using Android.Views;
using AndroidX.Activity;
using Microsoft.UI.Xaml.Media;
using Windows.UI.Core;

namespace Uno.UI.Runtime.Android;

partial class ApplicationActivity
{
	// A back swipe's touch-down reaches the app before the system takes the pointer over (ACTION_CANCEL), so it
	// light-dismisses an open popup first. The Back that ends the swipe has then already done its job.
	private const long GestureBackTimeoutMs = 2000;

	private OnBackPressedCallback? _backPressedCallback;
	private int _lightDismissPopupsBeforeTouchDown;
	private bool _gestureTouchDownClosedPopup;
	private long _gestureClosedPopupUptimeMs = -1;
	private bool _gestureBackStarted;
	private Handler? _gestureBackExpiryHandler;

	private void InitializeBackPressedCallback()
	{
		// Guard against multiple calls
		if (_backPressedCallback is not null)
		{
			return;
		}

		// On Android 16+ (API 36+), use OnBackPressedCallback for predictive back gesture support.
		// The callback is enabled/disabled based on combined back handler state
		// (both public BackRequested subscribers and internal BackButtonIntegration listeners).
		if ((int)Build.VERSION.SdkInt >= 36)
		{
			SystemNavigationManager.BackHandlerStateChanged += OnBackHandlerStateChanged;
			_backPressedCallback = new UnoOnBackPressedCallback(this);
			OnBackPressedDispatcher.AddCallback(this, _backPressedCallback);
			UpdateBackPressedCallback();
		}
	}

	private void CleanupBackPressedCallback()
	{
		_gestureBackExpiryHandler?.RemoveCallbacksAndMessages(null);

		if (_backPressedCallback is not null)
		{
			SystemNavigationManager.BackHandlerStateChanged -= OnBackHandlerStateChanged;
			_backPressedCallback.Remove();
			_backPressedCallback = null;
		}
	}

	private void OnBackHandlerStateChanged(object? sender, bool hasHandlers) => UpdateBackPressedCallback();

	private void UpdateBackPressedCallback()
	{
		if (_backPressedCallback is not null)
		{
			// The popup a back swipe closed may have been the last back handler: its Back must still come to the app,
			// or the system would finish the activity.
			_backPressedCallback.Enabled = SystemNavigationManager.GetForCurrentView().HasAnyBackHandlers || IsGestureBackPending;
		}
	}

	private bool IsGestureBackPending
		=> _gestureTouchDownClosedPopup || IsGestureBackOfClosedPopupDue;

	// The swipe's Back is still to come: with predictive back, from the moment the system starts the back gesture until it
	// ends; before that (and below API 36, where nothing says when the swipe ends) for 2 s after the system took the pointer.
	private bool IsGestureBackOfClosedPopupDue
		=> _gestureBackStarted
			|| (_gestureClosedPopupUptimeMs >= 0 && SystemClock.UptimeMillis() - _gestureClosedPopupUptimeMs < GestureBackTimeoutMs);

	private void OnTouchEventDispatching(MotionEvent ev)
	{
		if (ev.ActionMasked == MotionEventActions.Down)
		{
			ClearGestureBack();

			// Counted first: with nothing open, the gesture area isn't asked for.
			var popups = CountOpenLightDismissPopups();
			_lightDismissPopupsBeforeTouchDown = popups > 0 && IsInBackGestureArea(ev.GetX()) ? popups : 0;

			// Set before the touch-down closes the popup, so the API 36+ callback doesn't go off and on again meanwhile.
			_gestureTouchDownClosedPopup = _lightDismissPopupsBeforeTouchDown > 0;
		}
	}

	private void OnTouchEventDispatched(MotionEvent ev)
	{
		switch (ev.ActionMasked)
		{
			case MotionEventActions.Down when _gestureTouchDownClosedPopup && CountOpenLightDismissPopups() >= _lightDismissPopupsBeforeTouchDown:
				// The touch-down closed nothing.
				ClearGestureBack();
				break;

			case MotionEventActions.Cancel when _gestureTouchDownClosedPopup:
				// The system took the pointer over: its Back comes when the finger lifts.
				_gestureTouchDownClosedPopup = false;
				_gestureClosedPopupUptimeMs = SystemClock.UptimeMillis();
				if (!_gestureBackStarted && _backPressedCallback is not null)
				{
					_gestureBackExpiryHandler ??= new Handler(Looper.MainLooper!);
					_gestureBackExpiryHandler.PostDelayed(UpdateBackPressedCallback, GestureBackTimeoutMs);
				}
				break;

			case MotionEventActions.Up when _gestureTouchDownClosedPopup:
				ClearGestureBack();
				break;
		}
	}

	private void OnGestureBackStarted()
	{
		if (IsGestureBackPending)
		{
			// The system started the back swipe: its Back is due until the swipe ends, however long the user holds it.
			_gestureBackStarted = true;
			_gestureBackExpiryHandler?.RemoveCallbacksAndMessages(null);
		}
	}

	private void ClearGestureBack()
	{
		_gestureBackExpiryHandler?.RemoveCallbacksAndMessages(null);
		_gestureTouchDownClosedPopup = false;
		_gestureClosedPopupUptimeMs = -1;
		_gestureBackStarted = false;
		UpdateBackPressedCallback();
	}

	// The left and right edges where the system recognizes a back swipe, with gesture navigation (Android 10+).
	private bool IsInBackGestureArea(float x)
	{
		if (!OperatingSystem.IsAndroidVersionAtLeast(29) || Window?.DecorView is not { } decor || decor.RootWindowInsets?.SystemGestureInsets is not { } gestureInsets)
		{
			return false;
		}

		return x < gestureInsets.Left || x >= decor.Width - gestureInsets.Right;
	}

	private int CountOpenLightDismissPopups()
		=> Wrapper.XamlRoot is { } xamlRoot
			? VisualTreeHelper.GetOpenPopupsForXamlRoot(xamlRoot).Count(popup => popup.IsLightDismissEnabled)
			: 0;

	private bool RequestBack()
	{
		var swipeClosedPopup = IsGestureBackOfClosedPopupDue;
		ClearGestureBack();

		return swipeClosedPopup || SystemNavigationManager.GetForCurrentView().RequestBack();
	}

	/// <summary>
	/// Callback for handling back button presses on Android 16+ (API 36+) with predictive back gesture support.
	/// </summary>
	private sealed class UnoOnBackPressedCallback : OnBackPressedCallback
	{
		private readonly ApplicationActivity _activity;

		public UnoOnBackPressedCallback(ApplicationActivity activity) : base(false)
		{
			_activity = activity;
		}

		public override void HandleOnBackStarted(BackEventCompat backEvent) => _activity.OnGestureBackStarted();

		public override void HandleOnBackCancelled() => _activity.ClearGestureBack();

		public override void HandleOnBackPressed()
		{
			// With back handlers, the app takes this Back whatever BackRequested's Handled says: on Android 16+, the system
			// asks before the gesture whether the app handles Back. Without any, the callback was enabled only for a back
			// swipe that closed the last one.
			var hadBackHandlers = SystemNavigationManager.GetForCurrentView().HasAnyBackHandlers;
			if (!_activity.RequestBack() && !hadBackHandlers)
			{
				// That swipe's Back came too late to count: go back as the system would have.
				Enabled = false;
				_activity.OnBackPressedDispatcher.OnBackPressed();
				_activity.UpdateBackPressedCallback();
			}
		}
	}
}
