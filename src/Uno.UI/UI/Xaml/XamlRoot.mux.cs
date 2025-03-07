#nullable enable

using Uno.UI.Xaml.Core;
using Windows.Foundation;

namespace Windows.UI.Xaml;

public sealed partial class XamlRoot
{
	internal VisualTree VisualTree { get; set; }

	/// <summary>
	/// Gets the width and height of the content area.
	/// </summary>
	public Size Size => VisualTree.Size;

	/// <summary>
	/// Gets a value that represents the number of raw (physical) pixels for each view pixel.
	/// </summary>
	public double RasterizationScale => VisualTree.RasterizationScale;

	/// <summary>
	/// Gets a value that indicates whether the XamlRoot is visible.
	/// </summary>
	public bool IsHostVisible => VisualTree.IsVisible;

	internal void RaiseChangedEvent() => Changed?.Invoke(this, new());

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
}
