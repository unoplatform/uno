namespace Windows.Foundation.Metadata;

/// <summary>
/// Values that indicate if a Windows feature is enabled or disabled.
/// </summary>
public enum FeatureStage 
{
	/// <summary>
	/// The feature is always disabled.
	/// </summary>
	AlwaysDisabled = 0,

	/// <summary>
	/// The feature is diabled by default.
	/// </summary>
	DisabledByDefault = 1,

	/// <summary>
	/// The feature is enabled by default.
	/// </summary>
	EnabledByDefault = 2,

	/// <summary>
	/// The feature is always enabled.
	/// </summary>
	AlwaysEnabled = 3,
}
