using System;
using Android.OS;
using AndroidX.Activity;
using Windows.UI.Core;

namespace Microsoft.UI.Xaml;

partial class ApplicationActivity
{
	private OnBackPressedCallback? _backPressedCallback;

	private void InitializeBackPressedCallback()
	{
		// On Android 15+ (API 35+), use OnBackPressedCallback for predictive back gesture support.
		// The callback is enabled/disabled based on BackRequested subscription state.
		if ((int)Build.VERSION.SdkInt >= 35)
		{
			_backPressedCallback = new UnoOnBackPressedCallback(
				enabled: SystemNavigationManager.GetForCurrentView().HasBackRequestedSubscribers);
			OnBackPressedDispatcher.AddCallback(this, _backPressedCallback);
			SystemNavigationManager.BackRequestedSubscribersChanged += OnBackRequestedSubscribersChanged;
		}
	}

	private void CleanupBackPressedCallback()
	{
		if (_backPressedCallback is not null)
		{
			SystemNavigationManager.BackRequestedSubscribersChanged -= OnBackRequestedSubscribersChanged;
			_backPressedCallback.Remove();
			_backPressedCallback = null;
		}
	}

	private void OnBackRequestedSubscribersChanged(object? sender, bool hasSubscribers)
	{
		if (_backPressedCallback is not null)
		{
			_backPressedCallback.Enabled = hasSubscribers;
		}
	}

	/// <summary>
	/// Callback for handling back button presses on Android 15+ with predictive back gesture support.
	/// </summary>
	private sealed class UnoOnBackPressedCallback : OnBackPressedCallback
	{
		public UnoOnBackPressedCallback(bool enabled) : base(enabled)
		{
		}

		public override void HandleOnBackPressed()
		{
			// On Android 15+, subscription to BackRequested means the app handles back navigation.
			// The Handled property is ignored - back press is always consumed when callback is enabled.
			SystemNavigationManager.GetForCurrentView().RequestBack();
		}
	}
}
