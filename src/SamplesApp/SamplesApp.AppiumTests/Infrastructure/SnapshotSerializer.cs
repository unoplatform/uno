#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SamplesApp.AppiumTests.Infrastructure;

/// <summary>
/// Reads and writes <see cref="AccessibilityNode"/> trees as deterministic
/// JSON files. The output format is stable: indented two-space, LF line
/// endings, ordered properties, no escaping of non-ASCII so diffs render
/// human-readably.
/// </summary>
public static class SnapshotSerializer
{
	private const int SchemaVersion = 1;

	private static readonly JsonSerializerOptions s_options = new()
	{
		WriteIndented = true,
		DefaultIgnoreCondition = JsonIgnoreCondition.Never,
		Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
	};

	public static string Serialize(AccessibilityNode root, string sample, string flavor)
	{
		var snapshot = new SnapshotFile
		{
			Schema = SchemaVersion,
			Sample = sample,
			Flavor = flavor,
			Root = root,
		};
		return JsonSerializer.Serialize(snapshot, s_options).Replace("\r\n", "\n");
	}

	public static void Write(string path, AccessibilityNode root, string sample, string flavor)
	{
		Directory.CreateDirectory(Path.GetDirectoryName(path)!);
		File.WriteAllText(path, Serialize(root, sample, flavor));
	}

	public static AccessibilityNode? Read(string path)
	{
		if (!File.Exists(path))
		{
			return null;
		}

		var json = File.ReadAllText(path);
		var snapshot = JsonSerializer.Deserialize<SnapshotFile>(json, s_options);
		return snapshot?.Root;
	}

	private sealed class SnapshotFile
	{
		[JsonPropertyName("schema")]
		public int Schema { get; set; }

		[JsonPropertyName("sample")]
		public string Sample { get; set; } = string.Empty;

		[JsonPropertyName("flavor")]
		public string Flavor { get; set; } = string.Empty;

		[JsonPropertyName("root")]
		public AccessibilityNode Root { get; set; } = new();
	}
}
