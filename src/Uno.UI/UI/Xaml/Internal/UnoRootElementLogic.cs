using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Uno.Foundation.Logging;
using Uno.UI.Helpers.WinUI;

namespace Uno.UI.Xaml.Core;

/// <summary>
/// Contains Uno-specific root element logic shared across RootVisual and XamlIsland.
/// </summary>
internal class UnoRootElementLogic
{
	private readonly Panel _rootElement;
	private double? _inputPaneViewportHeight;

	public UnoRootElementLogic(Panel rootElement)
	{
		_rootElement = rootElement;

		//Uno specific - flag as VisualTreeRoot for interop with existing logic
		rootElement.IsVisualTreeRoot = true;
	}

	/// <summary>
	/// How far the public root visual is scrolled up to keep the focused element above the input pane.
	/// </summary>
	/// <remarks>
	/// Stands in for WinUI's RootScrollViewer, which Uno does not host: while the input pane is shown, the public
	/// root visual scrolls within a viewport that ends at the top of the pane, as the outermost bring-into-view scroller.
	/// </remarks>
	internal double InputPaneVerticalOffset { get; private set; }

	/// <summary>
	/// Like WinUI's RootScrollViewer::NotifyInputPaneStateChange: sets the area left above the input pane, then places the
	/// open flyouts of this root again.
	/// </summary>
	internal void NotifyInputPaneStateChange(bool isInputPaneVisible, double? viewportHeight)
	{
		SetInputPaneViewportHeight(viewportHeight);

		if (VisualTree.GetForElement(_rootElement) is not { } visualTree)
		{
			return;
		}

		// Ensure Flyout size and position properly with showing/hiding IHM.
		foreach (var flyout in FlyoutBase.OpenFlyouts.ToArray())
		{
			if (flyout.XamlRoot?.VisualTree != visualTree)
			{
				continue;
			}

			try
			{
				flyout.NotifyInputPaneStateChange(isInputPaneVisible);
			}
			catch (Exception e)
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error($"Failed to place {flyout.GetType().Name} again for the input pane.", e);
				}
			}
		}
	}

	/// <summary>
	/// Sets the height of the area left above the input pane, or null once the pane no longer constrains the root.
	/// </summary>
	internal void SetInputPaneViewportHeight(double? viewportHeight)
	{
		_inputPaneViewportHeight = viewportHeight;

		SetInputPaneVerticalOffset(viewportHeight is { } height
			? Math.Min(InputPaneVerticalOffset, GetMaxInputPaneVerticalOffset(height))
			: 0);
	}

	internal void OnBringIntoViewRequested(BringIntoViewRequestedEventArgs args)
	{
		if (args.Handled
			|| _inputPaneViewportHeight is not { } viewportHeight
			|| VisualTree.GetForElement(_rootElement)?.PublicRootVisual is not { } content
			|| args.TargetElement is not { } target
			|| (target != content && !SharedHelpers.IsAncestor(target, content, true /*checkVisibility*/)))
		{
			return;
		}

		// Inner scrollers have already retargeted the request to where the element lands once they scrolled.
		var targetRect = target.TransformToVisual(content).TransformBounds(args.TargetRect);

		var offset = InputPaneVerticalOffset;
		if (targetRect.Bottom > offset + viewportHeight)
		{
			offset = targetRect.Bottom - viewportHeight;
		}

		if (targetRect.Top < offset)
		{
			offset = targetRect.Top;
		}

		SetInputPaneVerticalOffset(Math.Clamp(offset, 0, GetMaxInputPaneVerticalOffset(viewportHeight)));
	}

	private double GetMaxInputPaneVerticalOffset(double viewportHeight) => Math.Max(0, _rootElement.ActualHeight - viewportHeight);

	private void SetInputPaneVerticalOffset(double offset)
	{
		if (offset == InputPaneVerticalOffset)
		{
			return;
		}

		InputPaneVerticalOffset = offset;
		_rootElement.InvalidateArrange();

		// Popups are not moved with the content: re-arrange them so the ones placed against it follow.
		if (VisualTree.GetForElement(_rootElement)?.PopupRoot is { } popupRoot)
		{
			foreach (var child in popupRoot.Children)
			{
				child.InvalidateArrange();
			}
		}
	}
}
