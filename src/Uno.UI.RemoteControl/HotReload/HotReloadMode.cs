namespace Uno.UI.RemoteControl.HotReload;

internal enum HotReloadMode
{
	/// <summary>
	/// Hot reload is not configured
	/// </summary>
	None = 0,

	/// <summary>
	/// Hot reload using Metadata updates
	/// </summary>
	/// <remarks>This can be metadata-updates pushed by either VS or the dev-server.</remarks>
	MetadataUpdates,
}
