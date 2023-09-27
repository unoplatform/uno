#nullable enable

using System;
using Uno.UI.DataBinding;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.Xaml.Core;

internal partial class VisualTree : IWeakReferenceProvider
{
	private const int UnoTopZIndex = int.MaxValue - 100;
	private const int FocusVisualZIndex = UnoTopZIndex + 1;

	private ManagedWeakReference? _selfWeakReference;
	private ApplicationView? _applicationView;

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
