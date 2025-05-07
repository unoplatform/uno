using System;
using Windows.System;

namespace Microsoft.UI.Xaml.Controls;

partial class NavigationViewItem
{
	// For perf considerations, we defer the pressed and over visual state on Uno.
	// This highly improves scrolling experience by avoiding freeze of UI thread (due to measure/arrange)
	// at the begging of the scroll, or when flicking during scroll.
	// Note: This is enabled only if flag UNO_USE_DEFERRED_VISUAL_STATES is set in NavigationViewItem.cs

	private bool _uno_isDefferingOverState = false;
	private bool _uno_isDefferingPressedState = false;

#pragma warning disable CS0649 // Field 'NavigationViewItem._uno_pointerDeferring' is never assigned to, and will always have its default value null
	private DispatcherQueueTimer _uno_pointerDeferring;
#pragma warning restore CS0649 // Field 'NavigationViewItem._uno_pointerDeferring' is never assigned to, and will always have its default value null

#if __ANDROID__
	private void DeferUpdateVisualStateForPointer()
	{
		// Note: As we use only one timer for both pressed and over state, we stop this timer only if cancelled / capture lost
		//		 Other cases will be handle the "normal" way using the m_isPointerOver and m_isPressed flags.

		if (_uno_isDefferingOverState || _uno_isDefferingPressedState)
		{
			if (_uno_pointerDeferring is null)
			{
				_uno_pointerDeferring = global::Windows.System.DispatcherQueue.GetForCurrentThread().CreateTimer();
				_uno_pointerDeferring.Interval = TimeSpan.FromMilliseconds(200);
				_uno_pointerDeferring.IsRepeating = false;
				_uno_pointerDeferring.Tick += (snd, e) =>
				{
					if (_uno_isDefferingOverState || _uno_isDefferingPressedState)
					{
						_uno_isDefferingOverState = false;
						_uno_isDefferingPressedState = false;
						UpdateVisualStateForPointer();
					}
				};
			}

			if (!_uno_pointerDeferring.IsRunning)
			{
				_uno_pointerDeferring.Start();
			}
		}
	}
#endif
}
