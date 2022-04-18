#nullable enable

using Uno.UI.Xaml.Core;
using Uno.UI.Xaml.Islands;
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

	/// <summary>
	/// Occurs when a property of XamlRoot has changed.
	/// </summary>
	public event TypedEventHandler<XamlRoot, XamlRootChangedEventArgs>? Changed;

	public UIElement? Content => VisualTree.PublicRootVisual;

	//TODO Uno specific: This is most likely not implemented here in MUX:
	public Size Size
	{
		get
		{
			var rootElement = VisualTree.RootElement;
			if (rootElement is RootVisual rootVisual)
			{
				//TODO:MZ: Support multiple windows!
				return Window.Current.Bounds.Size;
			}
			else if (rootElement is XamlIslandRoot xamlIslandRoot)
			{
				return new Size(xamlIslandRoot.ActualSize.X, xamlIslandRoot.ActualSize.Y);
			}

			return default;
		}
	}

	//TODO Uno specific: This is most likely not implemented here in MUX:
	internal Rect Bounds
	{
		get
		{
			var rootElement = VisualTree.RootElement;
			if (rootElement is RootVisual rootVisual)
			{
				//TODO:MZ: Support multiple windows!
				return Window.Current.Bounds;
			}
			else if (rootElement is XamlIslandRoot xamlIslandRoot)
			{
				return new Rect(0, 0, xamlIslandRoot.ActualSize.X, xamlIslandRoot.ActualSize.Y);
			}

			return default;
		}
	}

	public double RasterizationScale
		=> global::Windows.Graphics.Display.DisplayInformation.GetForCurrentView().RawPixelsPerViewPixel;

	internal void NotifyChanged()
	{
		Changed?.Invoke(this, new XamlRootChangedEventArgs());
	}
}
