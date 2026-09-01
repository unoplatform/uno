using System;
using System.Linq;
using Uno.Disposables;
using static Uno.UI.FeatureConfiguration;

namespace Microsoft.UI.Xaml.Controls.Primitives;

partial class ScrollBar
{
	private bool _isTouchThumbDragEnabled;

	/// <summary>
	/// Keeps the bar in its interactive state, so that its thumb can be dragged with a finger.
	/// </summary>
	/// <remarks>
	/// Opted into through <c>Uno.UI.Toolkit.ScrollBarExtensions.IsTouchThumbDragEnabled</c>.
	/// WinUI has the bar parts ignore touch and shows the non-interactive touch indicator instead, which
	/// stays the default here. A bar which opted in is treated as not conscious, which is the same path a
	/// system with auto-hiding scroll bars turned off takes.
	/// </remarks>
	internal bool IsTouchThumbDragEnabled
	{
		get => _isTouchThumbDragEnabled;
		set
		{
			if (_isTouchThumbDragEnabled != value)
			{
				_isTouchThumbDragEnabled = value;

				// AttachEvents is what applies IgnoreTouchInput to the parts, and it is safe to re-run: its
				// subscriptions are held by SerialDisposables, which is how OnLoaded already re-attaches them.
				AttachEvents();

				// The indicator states are picked in ChangeVisualState, which this re-runs.
				RefreshTrackLayout();
			}
		}
	}

	private static void DetachEvents(object snd, RoutedEventArgs args) // OnUnloaded
		=> (snd as ScrollBar)?.DetachEvents();

#if !UNO_HAS_ENHANCED_LIFECYCLE
	private static void OnLayoutUpdated(
		object pSender,
		object pArgs)
	{
		(pSender as ScrollBar)?.UpdateTrackLayout();
	}
#endif
}
