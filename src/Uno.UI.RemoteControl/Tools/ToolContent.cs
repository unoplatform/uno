#nullable enable

namespace Uno.UI.RemoteControl.Tools;

/// <summary>A single piece of content produced by a tool or resource.</summary>
internal sealed record ToolContent(
	ToolContentKind Kind,
	string? Text = null,
	string? MimeType = null,
	string? Base64Data = null);
