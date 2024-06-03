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

	/// <summary>
	/// Hot Reload using partial updated types discovery.
	/// </summary>
	/// <remarks>
	/// In some cases application's MetadataUpdateHandlers are not invoked by the IDE.
	/// When this mode is active, application listen for FileReload (a.k.a. FileUpdated) messages, enumerates (after a small delay) all types loaded in the application to detect changes
	/// and invokes the MetadataUpdateHandlers **for types flags with the CreateNewOnMetadataUpdateAttribute**.
	/// </remarks>
	Partial,

	/// <summary>
	/// Hot Reload using XAML reader
	/// </summary>
	XamlReader,
}
