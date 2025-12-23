using System;
using AndroidX.Activity;

using AndroidXOnBackPressedCallback = AndroidX.Activity.OnBackPressedCallback;

namespace Uno.UI.Helpers;

/// <summary>
/// Custom OnBackPressedCallback that integrates with SystemNavigationManager.
/// This is required for Android 33+ where OnBackPressed is deprecated and
/// Android 36+ where OnBackPressed is no longer called at all.
/// </summary>
internal sealed class SystemNavigationManagerBackPressedCallback : AndroidXOnBackPressedCallback
{
	private readonly ComponentActivity _activity;

	/// <summary>
	/// Creates a new instance of the callback.
	/// </summary>
	/// <param name="activity">The activity to associate with this callback.</param>
	public SystemNavigationManagerBackPressedCallback(ComponentActivity activity)
		: base(enabled: true)
	{
		_activity = activity ?? throw new ArgumentNullException(nameof(activity));
	}

	public override void HandleOnBackPressed()
	{
		var handled = global::Windows.UI.Core.SystemNavigationManager.GetForCurrentView().RequestBack();
		if (!handled)
		{
			// The back was not handled by the app, so we need to allow the default behavior.
			// Temporarily disable this callback and re-invoke the dispatcher to trigger the default behavior.
			Enabled = false;
			try
			{
				_activity.OnBackPressedDispatcher.OnBackPressed();
			}
			finally
			{
				Enabled = true;
			}
		}
	}
}
