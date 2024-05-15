namespace Windows.Storage;

/// <summary>
/// Describes a known folder's access to a single capability.
/// </summary>
public enum KnownFoldersAccessStatus
{
	/// <summary>
	/// System admin disabled access for all users.
	/// </summary>
	DeniedBySystem = 0,

	/// <summary>
	/// App doesn't have the capability declared in the manifest.
	/// </summary>
	NotDeclaredByApp = 1,

	/// <summary>
	/// User has denied access and there is no fallback for this location.
	/// </summary>
	DeniedByUser = 2,

	/// <summary>
	/// User consent is required, but not yet completed.
	/// </summary>
	UserPromptRequired = 3,

	/// <summary>
	/// Access is allowed.
	/// </summary>
	Allowed = 4,

	/// <summary>
	/// Access is allowed but limited to a Per App Subfolder.
	/// </summary>
	AllowedPerAppFolder = 5,
}
