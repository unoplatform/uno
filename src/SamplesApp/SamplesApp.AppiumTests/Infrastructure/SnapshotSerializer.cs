#nullable enable

using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SamplesApp.AppiumTests.Infrastructure;

public static class SnapshotSerializer
{
	public const int SchemaVersion = 2;

	private static readonly JsonSerializerOptions s_options = new()
	{
		WriteIndented = true,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
		Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
	};

	public static string Serialize(AccessibilitySnapshot snapshot)
	{
		snapshot.Schema = SchemaVersion;
		return JsonSerializer.Serialize(snapshot, s_options).Replace("\r\n", "\n");
	}

	public static void Write(string path, AccessibilitySnapshot snapshot)
	{
		Directory.CreateDirectory(Path.GetDirectoryName(path)!);
		File.WriteAllText(path, Serialize(snapshot));
	}

	public static AccessibilitySnapshot? Read(string path)
	{
		if (!File.Exists(path))
		{
			return null;
		}

		var json = File.ReadAllText(path);
		var snapshot = JsonSerializer.Deserialize<AccessibilitySnapshot>(json, s_options);
		if (snapshot is null)
		{
			return null;
		}

		if (snapshot.Schema != SchemaVersion)
		{
			throw new InvalidDataException(
				$"Snapshot schema version mismatch for '{path}'. Expected {SchemaVersion}, actual {snapshot.Schema}.");
		}

		snapshot.Elements ??= new List<AccessibilityElementSnapshot>();
		return snapshot;
	}
}
