using System;

namespace Microsoft.UI.Content;

/// <summary>
/// Provides access to a read-only view of ContentSite properties.
/// </summary>
/// <remarks>
/// This object exposes the most recent values from a ContentSite,
/// it is not a snapshot in time.
/// </remarks>
public partial class ContentSiteView
{
	private readonly ContentSite _contentSite;

	internal ContentSiteView(ContentSite contentSite)
	{
		_contentSite = contentSite ?? throw new ArgumentNullException(nameof(contentSite));
	}

	/// <summary>
	/// Gets the IsSiteVisible state reported by the ContentSite.
	/// </summary>
	public bool IsSiteVisible => _contentSite.IsSiteVisible;

	/// <summary>
	/// Gets the default scaling factor of the parent for a single ContentSite.
	/// </summary>
	public float ParentScale => _contentSite.ParentScale;

	/// <summary>
	/// Gets the override scaling factor for a single ContentSite, ignoring the default scaling factor of the parent.
	/// </summary>
	public float OverrideScale => _contentSite.OverrideScale;

	/// <summary>
	/// Gets the computed local DPI for the associated ContentSite,
	/// which is computed from the OverrideScale and ParentScale.
	/// </summary>
	public float RasterizationScale => _contentSite.RasterizationScale;
}
