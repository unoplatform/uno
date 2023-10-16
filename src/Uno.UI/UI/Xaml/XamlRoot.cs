#nullable enable

using System;
using Uno.UI.Xaml.Core;
using Uno.UI.Xaml.Islands;
using Windows.Foundation;
using Windows.Graphics.Display;
using Uno.UI.Extensions;
using Windows.UI.Composition;
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

	/// <summary>
	/// Gets the width and height of the content area.
	/// </summary>
	public Size Size
	{
		get
		{
			if (VisualTree.ContentRoot.Type == ContentRootType.CoreWindow)
			{
				return Content?.RenderSize ?? Size.Empty;
			}

			var rootElement = VisualTree.RootElement;
			if (rootElement is RootVisual)
			{
				if (Window.CurrentSafe is null)
				{
					throw new InvalidOperationException("Window.Current must be set.");
				}

				return Window.CurrentSafe.Bounds.Size;
			}
			else if (rootElement is XamlIsland xamlIslandRoot)
			{
				var width = !double.IsNaN(xamlIslandRoot.Width) ? xamlIslandRoot.Width : 0;
				var height = !double.IsNaN(xamlIslandRoot.Height) ? xamlIslandRoot.Height : 0;
				return new Size(width, height);
			}

			return default;
		}
	}

	internal Rect Bounds
	{
		get
		{
			var rootElement = VisualTree.RootElement;
			if (rootElement is RootVisual rootVisual)
			{
				if (Window.CurrentSafe is null)
				{
					throw new InvalidOperationException("Window.Current must be set.");
				}

				return Window.CurrentSafe.Bounds;
			}
			else if (rootElement is XamlIsland xamlIsland)
			{
				var width = !double.IsNaN(xamlIsland.Width) ? xamlIsland.Width : 0;
				var height = !double.IsNaN(xamlIsland.Height) ? xamlIsland.Height : 0;
				return new Rect(0, 0, width, height);
			}

			return default;
		}
	}

	internal Compositor Compositor => Compositor.GetSharedCompositor();

	/// <summary>
	/// Gets a value that represents the number of raw (physical) pixels for each view pixel.
	/// </summary>
	public double RasterizationScale => DisplayInformation.GetForCurrentView().RawPixelsPerViewPixel;

	/// <summary>
	/// Gets a value that indicates whether the XamlRoot is visible.
	/// </summary>
	public bool IsHostVisible => VisualTree.IsVisible;

#if !HAS_UNO_WINUI // This is a UWP-only property
	/// <summary>
	/// Gets the context identifier for the view.
	/// </summary>
	public UIContext UIContext { get; } = new UIContext();
#endif

	internal Window? HostWindow => VisualTree.ContentRoot.GetOwnerWindow();

	internal void NotifyChanged() => Changed?.Invoke(this, new XamlRootChangedEventArgs());

	internal static XamlRoot? GetForElement(DependencyObject element)
	{
		XamlRoot? result = null;

		var visualTree = VisualTree.GetForElement(element);
		if (visualTree is not null)
		{
			result = visualTree.GetOrCreateXamlRoot();
		}

		return result;
	}

	internal static void SetForElement(DependencyObject element, XamlRoot? currentRoot, XamlRoot? newRoot)
	{
		if (currentRoot == newRoot)
		{
			return;
		}

		if (currentRoot is not null)
		{
			throw new InvalidOperationException("Cannot change XamlRoot for existing element");
		}

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
