using System;
using Windows.Foundation;
using Uno.UI;
using Uno;
using Uno.UI.Controls;

namespace Windows.UI.ViewManagement;

public partial class InputPane
{
	private static InputPane _instance = new();
	private Rect _occludedRect = new Rect(0, 0, 0, 0);

	private InputPane()
	{
		InitializePlatform();
	}

	partial void InitializePlatform();

	public event TypedEventHandler<InputPane, InputPaneVisibilityEventArgs> Hiding;

	public event TypedEventHandler<InputPane, InputPaneVisibilityEventArgs> Showing;

	public Rect OccludedRect
	{
		get => _occludedRect;
		internal set
		{
			if (_occludedRect != value)
			{
				_occludedRect = value;
				OnOccludedRectChanged();
			}
		}
	}

	public bool Visible
	{
		get => OccludedRect.Height > 0;
		set
		{
			if (value)
			{
				TryShow();
			}
			else
			{
				TryHide();
			}
		}
	}

	public static InputPane GetForCurrentView() => _instance;

#if __APPLE_UIKIT__
	[NotImplemented]
#endif
	public bool TryShow()
	{
		if (Visible)
		{
			return false;
		}

		return TryShowPlatform();
	}

	public bool TryHide()
	{
		if (!Visible)
		{
			return false;
		}

		return TryHidePlatform();
	}

	internal void OnOccludedRectChanged()
	{
		var args = new InputPaneVisibilityEventArgs(OccludedRect);

		if (Visible)
		{
			Showing?.Invoke(this, args);
		}
		else
		{
			Hiding?.Invoke(this, args);
		}

		if (!args.EnsuredFocusedElementInView)
		{
			// Wait for proper element to be focused
			_ = UI.Core.CoreDispatcher.Main.RunAsync(
				UI.Core.CoreDispatcherPriority.Normal,
				() => EnsureFocusedElementInViewPartial()
			);
		}
	}

	partial void EnsureFocusedElementInViewPartial();
}
