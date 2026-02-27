using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Uno.UI.DevServer.Cli.Mcp.Setup;

/// <summary>
/// Pure JSON manipulation for merging/removing MCP server entries in config files.
/// No I/O — receives string content, returns string content.
/// </summary>
internal static class ConfigWriter
{
	private static readonly JsonDocumentOptions _readOptions = new()
	{
		CommentHandling = JsonCommentHandling.Skip,
		AllowTrailingCommas = true,
	};

	private static readonly JsonSerializerOptions _writeOptions = new()
	{
		WriteIndented = true,
	};

	/// <summary>
	/// Merges a server definition into existing JSON config content.
	/// Creates the file structure if <paramref name="existingContent"/> is null or empty.
	/// </summary>
	/// <param name="existingContent">Existing file content (may be null/empty for new file).</param>
	/// <param name="rootKey">JSON root key (<c>"servers"</c> or <c>"mcpServers"</c>).</param>
	/// <param name="serverKey">Key name for the server entry (e.g. <c>"UnoApp"</c>).</param>
	/// <param name="definition">Server definition to write.</param>
	/// <param name="includeType">Whether to include a <c>"type"</c> field (VS Code format).</param>
	/// <param name="transport">Transport type for the <c>"type"</c> field (e.g. <c>"stdio"</c>).</param>
	/// <returns>Updated JSON content string (2-space indent, trailing newline, UTF-8 no BOM).</returns>
	/// <exception cref="JsonException">Thrown when <paramref name="existingContent"/> is malformed JSON.</exception>
	public static string MergeServer(
		string? existingContent,
		string rootKey,
		string serverKey,
		JsonObject definition,
		bool includeType,
		string? transport)
	{
		var root = ParseOrCreateRoot(existingContent);

		// Ensure root key object exists
		if (root[rootKey] is not JsonObject serversObj)
		{
			serversObj = new JsonObject();
			root[rootKey] = serversObj;
		}

		// Build the new entry with shallow merge of existing unknown keys
		var newEntry = CloneJsonObject(definition);

		if (includeType && transport is not null)
		{
			newEntry["type"] = transport;
		}

		// Shallow merge: preserve unknown keys from existing entry
		if (serversObj[serverKey] is JsonObject existingEntry)
		{
			foreach (var prop in existingEntry)
			{
				if (!newEntry.ContainsKey(prop.Key))
				{
					newEntry[prop.Key] = prop.Value is not null ? JsonNode.Parse(prop.Value.ToJsonString()) : null;
				}
			}
		}

		serversObj[serverKey] = newEntry;

		return FormatOutput(root);
	}

	/// <summary>
	/// Removes a server entry from existing JSON config content.
	/// </summary>
	/// <returns>Updated JSON content, or <c>null</c> if the server was not found.</returns>
	public static string? RemoveServer(
		string? existingContent,
		string rootKey,
		string serverKey)
	{
		if (string.IsNullOrWhiteSpace(existingContent))
		{
			return null;
		}

		var root = ParseOrCreateRoot(existingContent);

		if (root[rootKey] is not JsonObject serversObj)
		{
			return null;
		}

		if (!serversObj.Remove(serverKey))
		{
			return null;
		}

		return FormatOutput(root);
	}

	private static JsonObject ParseOrCreateRoot(string? content)
	{
		if (string.IsNullOrWhiteSpace(content))
		{
			return new JsonObject();
		}

		// Parse with JSONC support (comments + trailing commas), then serialize
		// back to clean JSON and re-parse into a mutable JsonNode tree.
		using var doc = JsonDocument.Parse(content, _readOptions);
		var cleanJson = JsonSerializer.Serialize(doc.RootElement);
		var root = JsonNode.Parse(cleanJson);

		if (root is JsonObject objRoot)
		{
			return objRoot;
		}

		// Non-object root: wrap in _original to avoid data loss
		return new JsonObject { ["_original"] = root };
	}

	private static JsonObject CloneJsonObject(JsonObject source)
	{
		var json = source.ToJsonString();
		return JsonNode.Parse(json)!.AsObject();
	}

	private static string FormatOutput(JsonObject root)
	{
		var json = root.ToJsonString(_writeOptions);

		// Ensure trailing newline
		if (!json.EndsWith('\n'))
		{
			json += "\n";
		}

		return json;
	}
}
