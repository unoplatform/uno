#nullable enable

using Uno.UI.Xaml.Core;
using Windows.Foundation;

namespace Windows.UI.Xaml;

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

	public event TypedEventHandler<XamlRoot, XamlRootChangedEventArgs>? Changed;

	public UIElement? Content => Window.Current?.Content;

	public Size Size => Content?.RenderSize ?? Size.Empty;

	public double RasterizationScale
		=> global::Windows.Graphics.Display.DisplayInformation.GetForCurrentView().RawPixelsPerViewPixel;

	internal void NotifyChanged()
	{
		Changed?.Invoke(this, new XamlRootChangedEventArgs());
	}
}
