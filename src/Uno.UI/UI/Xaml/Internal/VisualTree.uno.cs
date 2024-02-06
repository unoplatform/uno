#nullable enable

using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.DataBinding;
using Uno.UI.Xaml.Input;
using Uno.UI.Xaml.Islands;
using Windows.Foundation;
using Windows.UI.ViewManagement;

namespace Uno.UI.Xaml.Core;

partial class VisualTree : IWeakReferenceProvider
{
	private const int UnoTopZIndex = int.MaxValue - 100;
	private const int FocusVisualZIndex = UnoTopZIndex + 1;

	private readonly UnoFocusInputHandler? _focusInputHandler;

	private ManagedWeakReference? _selfWeakReference;
	private Windows.UI.ViewManagement.ApplicationView? _applicationView;

	internal UnoFocusInputHandler? UnoFocusInputHandler => _focusInputHandler;

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

	// TODO Uno: This is a custom implementation of IsVisible, Size, VisibleBounds, and TrueVisibleBounds.
	// We should align as much as possible with WinUI later.
	internal bool IsVisible
	{
		get
		{
			if (RootElement is XamlIsland xamlIsland)
			{
				if (xamlIsland.OwnerWindow is Window) // For full window XamlIsland we don't need to check visibility
				{
					return true;
				}

				return xamlIsland.IsSiteVisible;
			}
			else if (RootElement is RootVisual)
			{
				return true;
			}

			return false;
		}
	}

	internal Size Size
	{
		get
		{
			if (RootElement is XamlIsland xamlIsland)
			{
				// If the size is set explicitly, prefer this, as ActualSize property values may be
				// a frame behind.
				if (!double.IsNaN(xamlIsland.Width) && !double.IsNaN(xamlIsland.Height))
				{
					return new(xamlIsland.Width, xamlIsland.Height);
				}

				var actualSize = xamlIsland.ActualSize;
				return new Size(actualSize.X, actualSize.Y);
			}
			else if (RootElement is RootVisual rootVisual)
			{
				if (Window.CurrentSafe is null)
				{
					throw new InvalidOperationException("Window.Current must be set.");
				}

				return Window.CurrentSafe.Bounds.Size;
			}
			else
			{
				throw new InvalidOperationException("Invalid VisualTree root type");
			}
		}
	}

	internal Rect VisibleBounds
	{
		get
		{
			if (GetApplicationViewForOwnerWindow() is ApplicationView applicationView)
			{
				return applicationView.VisibleBounds;
			}
			else
			{
				var size = Size;
				return new Rect(0, 0, size.Width, size.Height);
			}
		}
	}

	internal Rect TrueVisibleBounds
	{
		get
		{
			if (GetApplicationViewForOwnerWindow() is ApplicationView applicationView)
			{
				return applicationView.TrueVisibleBounds;
			}
			else
			{
				var size = Size;
				return new Rect(0, 0, size.Width, size.Height);
			}
		}
	}

	private ApplicationView? GetApplicationViewForOwnerWindow()
	{
		if (_applicationView is not null)
		{
			return _applicationView;
		}

		if (RootElement is XamlIsland xamlIsland)
		{
			if (xamlIsland.OwnerWindow is Window ownerWindow)
			{
				_applicationView = ApplicationView.GetForWindowId(ownerWindow.AppWindow.Id);
				return _applicationView;
			}
		}
		else if (RootElement is RootVisual)
		{
			if (Window.CurrentSafe is null)
			{
				throw new InvalidOperationException("Window.Current must be set.");
			}

			_applicationView = ApplicationView.GetForCurrentViewSafe();
			return _applicationView;
		}
		else
		{
			throw new InvalidOperationException("Invalid VisualTree root type");
		}

		return null;
	}
}
