#nullable enable

namespace Uno.UI.RemoteControl.Tools;

/// <summary>
/// Describes a resource published into the <see cref="ToolRegistry"/>: readable, addressable
/// content identified by an opaque <see cref="Uri"/>.
/// </summary>
internal sealed record ResourceDescriptor(
	string Uri,
	string Name,
	string Description,
	string? MimeType);
