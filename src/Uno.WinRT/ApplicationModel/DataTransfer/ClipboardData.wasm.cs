#nullable enable

using System;
using System.Text.Json.Serialization;
using Uno.Storage.Internal;

namespace Windows.ApplicationModel.DataTransfer;

// Values of ClipboardContentData.Status; mirrored by ClipboardContentStatus in Clipboard.ts.
internal static class ClipboardContentStatus
{
	// The content is known and handed over whole: a recent paste, or this application's own write.
	public const string Paste = "paste";
	public const string Own = "own";

	// The content is not known yet and is resolved by the providers.
	public const string Imminent = "imminent";
	public const string Unknown = "unknown";

	// Outcomes of an async read.
	public const string Async = "async";
	public const string Empty = "empty";
	public const string Denied = "denied";
	public const string Failed = "failed";
	public const string Unavailable = "unavailable";
}

internal sealed class ClipboardContentData
{
	[JsonPropertyName("status")]
	public string Status { get; set; } = "";

	[JsonPropertyName("texts")]
	public ClipboardTextEntry[] Texts { get; set; } = Array.Empty<ClipboardTextEntry>();

	[JsonPropertyName("files")]
	public NativeStorageItemInfo[] Files { get; set; } = Array.Empty<NativeStorageItemInfo>();

	[JsonPropertyName("image")]
	public NativeStorageItemInfo? Image { get; set; }

	[JsonPropertyName("handles")]
	public string[] Handles { get; set; } = Array.Empty<string>();

	[JsonPropertyName("pasteShortcutTime")]
	public double PasteShortcutTime { get; set; } = -1;
}

internal sealed class ClipboardTextEntry
{
	[JsonPropertyName("type")]
	public string Type { get; set; } = "";

	[JsonPropertyName("value")]
	public string Value { get; set; } = "";
}

internal sealed class ClipboardWriteEntry
{
	[JsonPropertyName("type")]
	public string Type { get; set; } = "";

	[JsonPropertyName("value")]
	public string Value { get; set; } = "";
}

internal sealed class ClipboardWriteFormat
{
	[JsonPropertyName("type")]
	public string Type { get; set; } = "";

	// Written as a web custom format rather than a standard one.
	[JsonPropertyName("custom")]
	public bool Custom { get; set; }
}

[JsonSerializable(typeof(ClipboardContentData))]
[JsonSerializable(typeof(ClipboardWriteEntry[]))]
[JsonSerializable(typeof(ClipboardWriteFormat[]))]
internal partial class ClipboardSerializationContext : JsonSerializerContext
{
}
