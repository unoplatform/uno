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
	MetadataUpdates,

	/// <summary>
	/// Hot Reload using partial updated types discovery
	/// </summary>
	Partial,

	/// <summary>
	/// Hot Reload using XAML reader
	/// </summary>
	XamlReader,
}
