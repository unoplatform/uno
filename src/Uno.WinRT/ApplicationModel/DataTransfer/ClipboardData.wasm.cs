#nullable enable

using System;
using System.Text.Json.Serialization;
using Uno.Storage.Internal;

namespace Windows.ApplicationModel.DataTransfer;

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

	[JsonPropertyName("custom")]
	public bool Custom { get; set; }
}

[JsonSerializable(typeof(ClipboardContentData))]
[JsonSerializable(typeof(ClipboardWriteEntry[]))]
internal partial class ClipboardSerializationContext : JsonSerializerContext
{
}
