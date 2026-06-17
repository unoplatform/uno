#nullable enable

using System.Collections.Immutable;

namespace Uno.UI.RemoteControl.Tools;

/// <summary>
/// The outcome of a tool invocation or resource read: one or more content items, optionally
/// flagged as an error. A consumer maps this onto its own result/content shape.
/// </summary>
internal sealed record ToolResult(
	ImmutableArray<ToolContent> Content,
	bool IsError = false)
{
	/// <summary>A successful result wrapping a single <see cref="ToolContentKind.Text"/> item.</summary>
	public static ToolResult Text(string text)
		=> new([new ToolContent(ToolContentKind.Text, Text: text)]);

	/// <summary>An error result wrapping a single textual explanation.</summary>
	public static ToolResult Error(string message)
		=> new([new ToolContent(ToolContentKind.Text, Text: message)], IsError: true);
}

/// <summary>A single piece of content produced by a tool or resource.</summary>
internal sealed record ToolContent(
	ToolContentKind Kind,
	string? Text = null,
	string? MimeType = null,
	string? Base64Data = null);

internal enum ToolContentKind
{
	Text,
	Json,
	Image,
	Blob,
}
