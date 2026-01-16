using System;

namespace Microsoft.UI.Content;

/// <summary>
/// Provides a host environment for a ContentIsland.
/// </summary>
public partial class ContentSite
	: IDisposable, IClosableNotifier
{
	private float _parentScale = 1f;

	internal ContentSite() => View = new(this);

	/// <summary>
	/// Gets or sets whether this ContentSite is visible.
	/// </summary>
	public bool IsSiteVisible { get; set; }

	/// <summary>
	/// Gets or sets the parent default scaling factor for this ContentSite.
	/// </summary>
	public float ParentScale
	{
		get => _parentScale;
		set
		{
			_parentScale = value;
			RasterizationScaleInitialized = true;
		}
	}

	/// <summary>
	/// Gets or sets the scaling factor to use for this ContentSite, which overrides the ParentScale.
	/// </summary>
	public float OverrideScale { get; set; } = 1f;

	/// <summary>
	/// Gets the computed local DPI for this ContentSite.
	/// </summary>
	public float RasterizationScale => OverrideScale * ParentScale;

	/// <summary>
	/// Gets the ContentSiteView associated with this ContentSite.
	/// </summary>
	public ContentSiteView View { get; }

	internal bool RasterizationScaleInitialized { get; private set; }
}
