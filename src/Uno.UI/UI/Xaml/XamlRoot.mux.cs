#nullable enable

using System.Runtime.CompilerServices;
using Uno.UI.Xaml.Controls;
using Uno.UI.Xaml.Core;
using Windows.Foundation;

namespace Microsoft.UI.Xaml;

public sealed partial class XamlRoot
{
	private ApplicationBarService? m_applicationBarService;

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

	internal static XamlRoot? GetForElement(DependencyObject element, bool createIfNotExist = true)
	{
		XamlRoot? result = null;

		var visualTree = VisualTree.GetForElement(element);
		if (visualTree is not null)
		{
			result = createIfNotExist ? visualTree.GetOrCreateXamlRoot() : visualTree.XamlRoot;
		}

		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static XamlRoot? GetImplementationForElement(DependencyObject element, bool createIfNotExist = true) =>
		GetForElement(element, createIfNotExist);

	internal ApplicationBarService GetApplicationBarService()
	{
		if (m_applicationBarService is null)
		{
			m_applicationBarService = new ApplicationBarService();
			m_applicationBarService.SetXamlRoot(this);
		}

		return m_applicationBarService;
	}

	internal ApplicationBarService? TryGetApplicationBarService()
	{
		return m_applicationBarService;
	}
}
