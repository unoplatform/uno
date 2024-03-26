#nullable enable

using Uno.UI.Xaml.Core;
using Windows.Foundation;

namespace Microsoft.UI.Xaml;

public sealed partial class XamlRoot
{
	internal VisualTree VisualTree { get; set; }

	internal Size Size => VisualTree.Size;

	internal double RasterizationScale => VisualTree.RasterizationScale;

	internal bool IsHostVisible => VisualTree.IsVisible;

	internal void RaiseChangedEvent() => Changed?.Invoke(this, new XamlRootChangedEventArgs());

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
