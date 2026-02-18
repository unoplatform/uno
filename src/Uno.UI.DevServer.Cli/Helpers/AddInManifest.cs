using System.Text.Json.Serialization;

namespace Uno.UI.DevServer.Cli.Helpers;

/// <summary>
/// Represents the deserialized content of a <c>devserver-addin.json</c> manifest file.
/// Unknown properties are ignored for forward compatibility.
/// </summary>
/// <seealso href="../addin-discovery.md"/>
internal sealed class AddInManifest
{
	[JsonPropertyName("version")]
	public int Version { get; set; }

	[JsonPropertyName("addins")]
	public List<AddInManifestEntry>? Addins { get; set; }
}

/// <seealso href="../addin-discovery.md"/>
internal sealed class AddInManifestEntry
{
	[JsonPropertyName("entryPoint")]
	public string? EntryPoint { get; set; }

	[JsonPropertyName("minHostVersion")]
	public string? MinHostVersion { get; set; }
}
