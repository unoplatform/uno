#nullable enable

using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Uno.Foundation.Extensibility;
using Uno.Foundation.Logging;
using Uno.UI.Xaml.Core;
using Windows.Foundation;

namespace Windows.UI.ViewManagement;

partial class InputPane
{
	// WinUI's ExtraPixelsForBringIntoView: the margin kept between the focused element and the input pane.
	private const double ExtraPixelsForBringIntoView = 20;

	private Lazy<IInputPaneExtension?>? _inputPaneExtension;

	// WinUI's m_bHandledFocusElementInView: whether the framework adjusted for a Showing, which Hiding then undoes.
	private bool _handledFocusedElementInView;

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

	// Like WinUI's RootScrollViewer, the root viewport ends at the top of the input pane and the open flyouts are placed
	// again, unless the app handled the occlusion itself. Like WinUI's InputPaneHandler, Hiding only undoes a Showing
	// the framework handled.
	partial void UpdateRootViewportPartial(bool ensureFocusedElementInView)
	{
		if (GetXamlRoot()?.VisualTree.RootElement is not { } rootElement
			|| rootElement is not IRootElement { RootElementLogic: { } rootElementLogic })
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().LogDebug($"Input pane {(Visible ? "shown" : "hidden")} ({OccludedRect}): no root to adjust.");
			}

			return;
		}

		if (ensureFocusedElementInView && (Visible || _handledFocusedElementInView))
		{
			_handledFocusedElementInView |= Visible;
			rootElementLogic.NotifyInputPaneStateChange(Visible, Visible ? Math.Clamp(OccludedRect.Y, 0, rootElement.ActualSize.Y) : null);
		}
		else
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().LogDebug($"Input pane {(Visible ? "shown" : "hidden")} ({OccludedRect}): the app handles it, so the open flyouts are not placed again.");
			}

			rootElementLogic.SetInputPaneViewportHeight(null);
		}
	}

	partial void EnsureFocusedElementInViewPartial()
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
}