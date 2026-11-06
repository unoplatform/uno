#nullable enable

using System;
using System.Collections.Generic;
using Uno.Foundation.Logging;
using Uno.UI.Dispatching;

namespace Microsoft.UI.Xaml.Controls.Primitives;

// What Uno adds to WinUI for the windows of windowed popups.
partial class Popup
{
	// TODO Uno: WinUI has no such case. A window that never presents would leave an open popup invisible.
	internal void OnWindowedPopupNeverShown()
	{
		m_windowNeverShown = true;

		if (m_windowedPopupWindow is { } window)
		{
			XamlRoot?.VisualTree.ContentRoot.CompositionTarget.RemoveWindowedPopupSurface(window.Surface);
			window.Surface.DropFrame();
		}

		EnsureBridgeClosed();
		m_isWindowed = false;

		if (m_fIsOpen && m_pChild is { } child)
		{
			MoveVisualInsideWindow(child);

			if (GetAdjustedPopupRoot() is { } popupRoot)
			{
				foreach (var openPopup in popupRoot.GetOpenPopups())
				{
					if (!openPopup.IsWindowed && openPopup.m_pChild is { } nestedChild && openPopup.GetFirstAncestorPopup(windowedPopupOnly: true) is null)
					{
						MoveVisualInsideWindow(nestedChild);
					}
				}
			}

			// It was placed against the monitor.
			AssociatedFlyout?.PlaceAfterLosingWindow();
		}

		static void MoveVisualInsideWindow(UIElement element)
		{
			element.Visual.HasOwnSurface = false;
			element.Visual.InvalidatePaint();
			element.Visual.CompositionTarget?.RequestNewFrame();
		}
	}

	private const int MaxHiddenWindows = 8;
	private static readonly List<WeakReference<Popup>> s_popupsWithHiddenWindow = new();

	private void TrackHiddenWindow()
	{
		StopTrackingHiddenWindow();
		s_popupsWithHiddenWindow.Add(new(this));

		if (s_popupsWithHiddenWindow.Count > MaxHiddenWindows)
		{
			// Closing a window can take a while, and this popup is still closing.
			NativeDispatcher.Main.Enqueue(CloseOldestHiddenWindows, NativeDispatcherPriority.Low);
		}
	}

	private static void CloseOldestHiddenWindows()
	{
		while (s_popupsWithHiddenWindow.Count > MaxHiddenWindows)
		{
			var oldest = s_popupsWithHiddenWindow[0];
			s_popupsWithHiddenWindow.RemoveAt(0);
			if (oldest.TryGetTarget(out var popup))
			{
				try
				{
					popup.EnsureBridgeClosed();
				}
				catch (Exception e)
				{
					typeof(Popup).LogWarn()?.Warn($"Closing a hidden popup window failed: {e.Message}");
				}
			}
		}
	}

	private void StopTrackingHiddenWindow()
		=> s_popupsWithHiddenWindow.RemoveAll(reference => !reference.TryGetTarget(out var popup) || popup == this);
}
