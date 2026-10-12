#nullable enable

using System.Runtime.CompilerServices;
using Microsoft.UI.Content;
using Uno.UI.Xaml.Core;
using Windows.Foundation;

namespace Microsoft.UI.Xaml;

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

	/// <summary>
	/// Gets the environment of the island hosting this XAML content, or null if there is none.
	/// </summary>
	public ContentIslandEnvironment? ContentIslandEnvironment
	{
		get
		{
			// Uno specific: there is no ContentIsland per content root, so the environment is derived from the host window.
			if (HostWindow?.AppWindow.Id is not { } appWindowId)
			{
				return null;
			}

			if (_contentIslandEnvironment?.AppWindowId != appWindowId)
			{
				_contentIslandEnvironment = new ContentIslandEnvironment(appWindowId);
			}

			return _contentIslandEnvironment;
		}
	}

	private ContentIslandEnvironment? _contentIslandEnvironment;

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
}
