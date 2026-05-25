#nullable enable

using System;
using System.Diagnostics;
using Windows.ApplicationModel.DataTransfer.DragDrop.Core;
using Microsoft.UI.Xaml.Controls.Primitives;
using Uno.UI.Xaml.Core;
using Uno.UI.Xaml.Islands;
using Windows.Foundation;
using Windows.Graphics.Display;
using Uno.UI.Extensions;
using Uno.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml;

/// <summary>
/// Represents a tree of XAML content and information about the context in which it is hosted.
/// </summary>
/// <remarks>
/// Effectively a public API wrapper around VisualTree.
/// </remarks>
public sealed partial class XamlRoot
{
	internal XamlRoot(VisualTree visualTree)
	{
		VisualTree = visualTree;
	}

	/// <summary>
	/// Occurs when a property of XamlRoot has changed.
	/// </summary>
	public event TypedEventHandler<XamlRoot, XamlRootChangedEventArgs>? Changed;

	/// <summary>
	/// Gets the root element of the XAML element tree.
	/// </summary>
	public UIElement? Content
	{
		get
		{
			var publicRoot = VisualTree.PublicRootVisual;
			if (publicRoot is WindowChrome chrome)
			{
				return chrome.Content as UIElement;
			}

			return publicRoot;
		}
	}

	internal Rect Bounds
	{
		get
		{
			var rootElement = VisualTree.RootElement;
			if (rootElement is RootVisual)
			{
				if (Window.CurrentSafe is not { } window)
				{
					throw new InvalidOperationException("Window.Current must be set.");
				}

				return window.Bounds;
			}
			else if (rootElement is XamlIslandRoot xamlIsland)
			{
				var width = !double.IsNaN(xamlIsland.Width) ? xamlIsland.Width : 0;
				var height = !double.IsNaN(xamlIsland.Height) ? xamlIsland.Height : 0;
				return new Rect(0, 0, width, height);
			}

			return default;
		}
	}

	internal Composition.Compositor Compositor => Composition.Compositor.GetSharedCompositor();

	internal Window? HostWindow => VisualTree.ContentRoot.GetOwnerWindow();

	internal static DisplayInformation GetDisplayInformation(XamlRoot? root)
		=> root?.HostWindow?.AppWindow.Id is { } id ? DisplayInformation.GetOrCreateForWindowId(id) : DisplayInformation.GetForCurrentViewSafe();

	internal static CoreDragDropManager GetCoreDragDropManager(XamlRoot? root)
		=> root?.HostWindow?.AppWindow.Id is { } id ? CoreDragDropManager.GetOrCreateForWindowId(id) : CoreDragDropManager.GetForCurrentViewSafe();

	internal static void SetForElement(DependencyObject element, XamlRoot? currentRoot, XamlRoot? newRoot)
	{
		if (currentRoot == newRoot)
		{
			return;
		}

		// WinUI uses a debug-only ASSERT in CDependencyObject::SetVisualTree guarded
		// by IsActive(): only elements currently in the live tree are checked, and
		// FlyoutBase is explicitly exempted. After Hide(), a Popup is no longer active
		// so the assert is naturally skipped for it. We mirror that with IsInLiveTree.
		Debug.Assert(
			currentRoot is null ||
			element is FlyoutBase ||
			(element is UIElement uiElement && !uiElement.IsInLiveTree),
			"Unexpected XamlRoot change for active non-FlyoutBase element");

		if (newRoot is not null)
		{
			element.SetVisualTree(newRoot.VisualTree);
		}
	}

	internal IDisposable OpenPopup(Microsoft.UI.Xaml.Controls.Primitives.Popup popup)
	{
		if (VisualTree.PopupRoot == null)
		{
			throw new InvalidOperationException("PopupRoot is not initialized yet.");
		}

		return VisualTree.PopupRoot.OpenPopup(popup);
	}
}
