#nullable enable

using System;
using Windows.Foundation;
using Uno.UI.DataBinding;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Xaml.Islands;
using Windows.UI.ViewManagement;

namespace Uno.UI.Xaml.Core;

internal partial class VisualTree : IWeakReferenceProvider
{
	private Rect _visibleBounds;
	private Rect? _visibleBoundsOverride;

	private const int UnoTopZIndex = int.MaxValue - 100;
	private const int FocusVisualZIndex = UnoTopZIndex + 1;
	internal const int TextBoxTouchKnobPopupZIndex = FocusVisualZIndex + 1;

	private ManagedWeakReference? _selfWeakReference;

	public Canvas? FocusVisualRoot { get; private set; }

	public void EnsureFocusVisualRoot()
	{
		if (FocusVisualRoot == null)
		{
			FocusVisualRoot = new Canvas()
			{
				Background = null,
				IsHitTestVisible = false
			};
			Canvas.SetZIndex(FocusVisualRoot, FocusVisualZIndex);
		}
	}

	ManagedWeakReference IWeakReferenceProvider.WeakReference =>
		_selfWeakReference ??= WeakReferencePool.RentSelfWeakReference(this);

	internal Rect VisibleBounds
	{
		get
		{
			if (VisibleBoundsOverride is not null)
			{
				return VisibleBoundsOverride.Value;
			}
			else if (Windows.ApplicationModel.Core.CoreApplication.IsFullFledgedApp)
			{
				return _visibleBounds;
			}
			else
			{
				// For Uno islands, return the full size.
				return new Rect(0, 0, Size.Width, Size.Height);
			}
		}
		set
		{
			if (_visibleBounds != value)
			{
				_visibleBounds = value;

				OnVisibleBoundsChanged();
			}
		}
	}

	/// <summary>
	/// If set, overrides the 'real' visible bounds. Used for testing visible bounds-related behavior on devices that have no native
	/// 'unsafe area'.
	/// </summary>
	internal Rect? VisibleBoundsOverride
	{
		get => _visibleBoundsOverride;
		set
		{
			if (_visibleBoundsOverride != value)
			{
				_visibleBoundsOverride = value;

				OnVisibleBoundsChanged();
			}
		}
	}

	internal event EventHandler? VisibleBoundsChanged;

	private void OnVisibleBoundsChanged()
	{
		VisibleBoundsChanged?.Invoke(this, EventArgs.Empty);

		if (RootElement is XamlIslandRoot { OwnerWindow: { } ownerWindow } _)
		{
			// Notify the XamlIslandRoot that the visible bounds have changed.
			ApplicationView.GetOrCreateForWindowId(ownerWindow.AppWindow.Id).SetVisibleBounds(VisibleBounds);
		}
		else if (RootElement is RootVisual window)
		{
			// Notify the Window that the visible bounds have changed.
			ApplicationView.GetForCurrentViewSafe().SetVisibleBounds(VisibleBounds);
		}
	}
}
