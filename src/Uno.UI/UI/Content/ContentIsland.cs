using System;
using Microsoft.UI;
using Microsoft.UI.Composition;
using Uno;
using Uno.UI.Hosting;
using Windows.Foundation;

namespace Microsoft.UI.Content;

/// <summary>
/// A self-contained piece of content with independent input, output, layout, and accessibility that is connected to a parent ContentSite.
/// </summary>
public partial class ContentIsland : IDisposable, IClosableNotifier, ICompositionSupportsSystemBackdrop
{
	/// <summary>
	/// Gets the local dots per inch (dpi) of a Microsoft.UI.Composition.ICompositionSurface.
	/// </summary>
	public float RasterizationScale
	{
		get
		{
			throw new global::System.NotImplementedException("The member float ContentIsland.RasterizationScale is not implemented. For more information, visit https://aka.platform.uno/notimplemented#m=float%20ContentIsland.RasterizationScale");
		}
	}

	/// <summary>
	/// Occurs when a state property for this ContentIsland changes.
	/// </summary>
	public event TypedEventHandler<ContentIsland, ContentIslandStateChangedEventArgs> StateChanged;

	/// <summary>
	/// Occurs when an automation provider is requested for this ContentIsland.
	/// </summary>
	[NotImplemented("__ANDROID__", "__IOS__", "IS_UNIT_TESTS", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
	public event TypedEventHandler<ContentIsland, ContentIslandAutomationProviderRequestedEventArgs> AutomationProviderRequested;
}
