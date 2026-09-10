#nullable enable

using System;
using System.Text.Json.Serialization;
using Uno.Storage.Internal;

namespace Windows.ApplicationModel.DataTransfer;

internal sealed class ClipboardSnapshotFormats
{
	[JsonPropertyName("pasteFormats")]
	public string[]? PasteFormats { get; set; }

	[JsonPropertyName("pasteHasFiles")]
	public bool PasteHasFiles { get; set; }

	[JsonPropertyName("pasteHasImage")]
	public bool PasteHasImage { get; set; }

	[JsonPropertyName("pasteImminent")]
	public bool PasteImminent { get; set; }

	[JsonPropertyName("ownFormats")]
	public string[]? OwnFormats { get; set; }
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

	[JsonPropertyName("custom")]
	public bool Custom { get; set; }
}

[JsonSerializable(typeof(ClipboardSnapshotFormats))]
[JsonSerializable(typeof(ClipboardContentData))]
[JsonSerializable(typeof(ClipboardWriteEntry[]))]
internal partial class ClipboardSerializationContext : JsonSerializerContext
{
}
