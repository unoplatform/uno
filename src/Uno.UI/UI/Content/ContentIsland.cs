using System;
using Microsoft.UI;
using Windows.UI.Composition;
using Uno;
using Uno.UI.Content;
using Uno.UI.Hosting;
using Uno.UI.Xaml.Controls;
using Windows.Foundation;

namespace Microsoft.UI.Content;

/// <summary>
/// A self-contained piece of content with independent input, output, layout, and accessibility that is connected to a parent ContentSite.
/// </summary>
public partial class ContentIsland
#if HAS_UNO_WINUI // These interfaces are not currently implemented and the Generated partial does not exist in UWP build.
	: IDisposable, IClosableNotifier, ICompositionSupportsSystemBackdrop
#endif
{
	private readonly ContentSiteView _contentSiteView;

	internal ContentIsland(ContentSiteView contentSiteView)
	{
		_contentSiteView = contentSiteView ?? throw new ArgumentNullException(nameof(contentSiteView));
	}

	/// <summary>
	/// Gets the local dots per inch (dpi) of a Windows.UI.Composition.ICompositionSurface.
	/// </summary>
	public float RasterizationScale => _contentSiteView.RasterizationScale;

	/// <summary>
	/// Gets whether the associated ContentSite is visible.
	/// </summary>
	public bool IsSiteVisible => _contentSiteView.IsSiteVisible;

	internal bool RasterizationScaleInitialized => _contentSiteView.RasterizationScaleInitialized;

	/// <summary>
	/// Occurs when a state property for this ContentIsland changes.
	/// </summary>
	public event TypedEventHandler<ContentIsland, ContentIslandStateChangedEventArgs> StateChanged;

#pragma warning disable CS0067
	/// <summary>
	/// Occurs when an automation provider is requested for this ContentIsland.
	/// </summary>
	[NotImplemented("__ANDROID__", "__IOS__", "IS_UNIT_TESTS", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
	public event TypedEventHandler<ContentIsland, ContentIslandAutomationProviderRequestedEventArgs> AutomationProviderRequested;
#pragma warning restore CS0067

	internal void RaiseStateChanged(ContentIslandStateChangedEventArgs stateChangedEventArgs) => StateChanged?.Invoke(this, stateChangedEventArgs);
}
