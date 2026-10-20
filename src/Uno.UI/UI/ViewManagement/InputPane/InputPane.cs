using System;
using Windows.Foundation;
using Uno.UI;
using Uno;
using Uno.UI.Controls;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Uno.Extensions;
using Uno.Foundation.Extensibility;
using Uno.UI.Extensions;

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

		var ensureFocusedElementInView = !args.EnsuredFocusedElementInView;

		UpdateRootViewport(ensureFocusedElementInView);

		if (ensureFocusedElementInView && Visible)
		{
			// Wait for proper element to be focused
			_ = UI.Core.CoreDispatcher.Main.RunAsync(
				UI.Core.CoreDispatcherPriority.Normal,
				EnsureFocusedElementInView
			);
		}
	}

#nullable enable
	// WinUI's ExtraPixelsForBringIntoView: the margin kept between the focused element and the input pane.
	private const double ExtraPixelsForBringIntoView = 20;

	private Lazy<IInputPaneExtension?>? _inputPaneExtension;

	partial void InitializePlatform()
	{
		_inputPaneExtension = new(() =>
		{
			ApiExtensibility.CreateInstance<IInputPaneExtension>(this, out var extension);
			return extension;
		});
	}

	private bool TryShowPlatform() => _inputPaneExtension?.Value?.TryShow() ?? false;

	private bool TryHidePlatform() => _inputPaneExtension?.Value?.TryHide() ?? false;

	private static XamlRoot? GetXamlRoot() => Window.InitialWindow?.Content?.XamlRoot;

	// Like WinUI's RootScrollViewer, the root viewport ends at the top of the input pane, unless the app
	// handled the occlusion itself.
	private void UpdateRootViewport(bool ensureFocusedElementInView)
	{
		if (GetXamlRoot()?.VisualTree.RootElement is not { } rootElement
			|| rootElement is not Uno.UI.Xaml.Core.IRootElement { RootElementLogic: { } rootElementLogic })
		{
			return;
		}

		rootElementLogic.SetInputPaneViewportHeight(Visible && ensureFocusedElementInView
			? Math.Clamp(OccludedRect.Y, 0, rootElement.ActualSize.Y)
			: null);
	}

	private void EnsureFocusedElementInView()
	{
		if (!Visible
			|| GetXamlRoot() is not { } xamlRoot
			|| FocusManager.GetFocusedElement(xamlRoot) is not UIElement focusedElement)
		{
			return;
		}

		var size = focusedElement.RenderSize;
		var targetRect = new Rect(0, 0, size.Width, size.Height);
		if (size.Height + (2 * ExtraPixelsForBringIntoView) <= OccludedRect.Y)
		{
			targetRect = new Rect(0, -ExtraPixelsForBringIntoView, size.Width, size.Height + (2 * ExtraPixelsForBringIntoView));
		}

		focusedElement.StartBringIntoView(new BringIntoViewOptions
		{
			AnimationDesired = false,
			TargetRect = targetRect,
		});
	}
#nullable disable
}
