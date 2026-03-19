using System.Text;
using System.Text.Json.Nodes;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Uno.UI.DevServer.Cli.Mcp.Setup;

/// <summary>
/// Pure JSON manipulation for merging/removing MCP server entries in config files.
/// No I/O — receives string content, returns string content.
/// Preserves JSONC comments while still normalizing formatting on write.
/// </summary>
internal static class ConfigWriter
{
	/// <summary>
	/// Merges a server definition into existing JSON config content.
	/// Creates the file structure if <paramref name="existingContent"/> is null or empty.
	/// </summary>
	public static string MergeServer(
		string? existingContent,
		string rootKey,
		string serverKey,
		JsonObject definition,
		bool includeType,
		string? transport,
		string? urlKey = null)
	{
		var newEntry = BuildNewEntry(definition, includeType, transport, urlKey);

		if (string.IsNullOrWhiteSpace(existingContent))
		{
			var root = new JObject
			{
				[rootKey] = new JObject
				{
					[serverKey] = newEntry,
				},
			};

			return FormatOutput(root);
		}

		try
		{
			using var reader = CreateReader(existingContent);
			using var writer = CreateWriter();

			if (!reader.Read())
			{
				throw new System.Text.Json.JsonException("The root JSON value must be an object.");
			}

			if (reader.TokenType != JsonToken.StartObject)
			{
				throw new System.Text.Json.JsonException("The root JSON value must be an object.");
			}

			WriteRootObjectForMerge(reader, writer.JsonWriter, rootKey, serverKey, newEntry);
			return writer.GetResult();
		}
		catch (JsonReaderException ex)
		{
			throw new System.Text.Json.JsonException(ex.Message, ex);
		}
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

		try
		{
			using var reader = CreateReader(existingContent);
			using var writer = CreateWriter();

			if (!reader.Read())
			{
				throw new System.Text.Json.JsonException("The root JSON value must be an object.");
			}

			if (reader.TokenType != JsonToken.StartObject)
			{
				throw new System.Text.Json.JsonException("The root JSON value must be an object.");
			}

			var removed = WriteRootObjectForRemove(reader, writer.JsonWriter, rootKey, serverKey);
			return removed ? writer.GetResult() : null;
		}
		catch (JsonReaderException ex)
		{
			throw new System.Text.Json.JsonException(ex.Message, ex);
		}
	}

	private static JObject BuildNewEntry(
		JsonObject definition,
		bool includeType,
		string? transport,
		string? urlKey)
	{
		var newEntry = JObject.Parse(definition.ToJsonString());

		if (urlKey is not null && urlKey != "url" && newEntry.Property("url") is { } urlProperty)
		{
			urlProperty.Remove();
			newEntry[urlKey] = urlProperty.Value;
		}

		if (includeType && transport is not null)
		{
			newEntry["type"] = transport;
		}

		return newEntry;
	}

	private static JsonTextReader CreateReader(string content)
	{
		return new JsonTextReader(new StringReader(content))
		{
			DateParseHandling = DateParseHandling.None,
		};
	}

	private static JsonWriterScope CreateWriter()
	{
		var stringWriter = new StringWriter(new StringBuilder());
		var jsonWriter = new JsonTextWriter(stringWriter)
		{
			Formatting = Formatting.Indented,
			IndentChar = ' ',
			Indentation = 2,
		};

		return new JsonWriterScope(stringWriter, jsonWriter);
	}

	private static void WriteRootObjectForMerge(
		JsonTextReader reader,
		JsonTextWriter writer,
		string rootKey,
		string serverKey,
		JObject newEntry)
	{
		writer.WriteStartObject();
		var foundRootKey = false;

		while (reader.Read())
		{
			switch (reader.TokenType)
			{
				case JsonToken.Comment:
					writer.WriteComment(reader.Value?.ToString());
					break;
				case JsonToken.PropertyName:
					var propertyName = reader.Value!.ToString()!;
					if (!reader.Read())
					{
						throw new System.Text.Json.JsonException("Unexpected end of JSON while reading property value.");
					}

					writer.WritePropertyName(propertyName);
					if (!ReadPastComments(reader, writer))
					{
						throw new System.Text.Json.JsonException("Unexpected end of JSON while reading property value.");
					}

					if (string.Equals(propertyName, rootKey, StringComparison.Ordinal))
					{
						foundRootKey = true;
						WriteServerCollectionForMerge(reader, writer, serverKey, newEntry);
					}
					else
					{
						CopyValue(reader, writer);
					}

					break;
				case JsonToken.EndObject:
					if (!foundRootKey)
					{
						writer.WritePropertyName(rootKey);
						writer.WriteStartObject();
						writer.WritePropertyName(serverKey);
						newEntry.WriteTo(writer);
						writer.WriteEndObject();
					}

					writer.WriteEndObject();
					return;
			}
		}

		throw new System.Text.Json.JsonException("Unexpected end of JSON while reading object.");
	}

	private static bool WriteRootObjectForRemove(
		JsonTextReader reader,
		JsonTextWriter writer,
		string rootKey,
		string serverKey)
	{
		writer.WriteStartObject();
		var removed = false;

		while (reader.Read())
		{
			switch (reader.TokenType)
			{
				case JsonToken.Comment:
					writer.WriteComment(reader.Value?.ToString());
					break;
				case JsonToken.PropertyName:
					var propertyName = reader.Value!.ToString()!;
					if (!reader.Read())
					{
						throw new System.Text.Json.JsonException("Unexpected end of JSON while reading property value.");
					}

					writer.WritePropertyName(propertyName);
					if (!ReadPastComments(reader, writer))
					{
						throw new System.Text.Json.JsonException("Unexpected end of JSON while reading property value.");
					}

					if (string.Equals(propertyName, rootKey, StringComparison.Ordinal))
					{
						removed |= WriteServerCollectionForRemove(reader, writer, serverKey);
					}
					else
					{
						CopyValue(reader, writer);
					}

					break;
				case JsonToken.EndObject:
					writer.WriteEndObject();
					return removed;
			}
		}

		throw new System.Text.Json.JsonException("Unexpected end of JSON while reading object.");
	}

	private static void WriteServerCollectionForMerge(
		JsonTextReader reader,
		JsonTextWriter writer,
		string serverKey,
		JObject newEntry)
	{
		if (reader.TokenType != JsonToken.StartObject)
		{
			writer.WriteStartObject();
			writer.WritePropertyName(serverKey);
			newEntry.WriteTo(writer);
			writer.WriteEndObject();
			SkipValue(reader);
			return;
		}

		writer.WriteStartObject();
		var foundEntry = false;

		while (reader.Read())
		{
			switch (reader.TokenType)
			{
				case JsonToken.Comment:
					writer.WriteComment(reader.Value?.ToString());
					break;
				case JsonToken.PropertyName:
					var propertyName = reader.Value!.ToString()!;
					if (!reader.Read())
					{
						throw new System.Text.Json.JsonException("Unexpected end of JSON while reading property value.");
					}

					writer.WritePropertyName(propertyName);
					if (!ReadPastComments(reader, writer))
					{
						throw new System.Text.Json.JsonException("Unexpected end of JSON while reading property value.");
					}

					if (string.Equals(propertyName, serverKey, StringComparison.Ordinal))
					{
						foundEntry = true;
						WriteEntryForMerge(reader, writer, newEntry);
					}
					else
					{
						CopyValue(reader, writer);
					}

					break;
				case JsonToken.EndObject:
					if (!foundEntry)
					{
						writer.WritePropertyName(serverKey);
						newEntry.WriteTo(writer);
					}

					writer.WriteEndObject();
					return;
			}
		}

		throw new System.Text.Json.JsonException("Unexpected end of JSON while reading object.");
	}

	private static bool WriteServerCollectionForRemove(
		JsonTextReader reader,
		JsonTextWriter writer,
		string serverKey)
	{
		if (reader.TokenType != JsonToken.StartObject)
		{
			CopyValue(reader, writer);
			return false;
		}

		writer.WriteStartObject();
		var removed = false;

		while (reader.Read())
		{
			switch (reader.TokenType)
			{
				case JsonToken.Comment:
					writer.WriteComment(reader.Value?.ToString());
					break;
				case JsonToken.PropertyName:
					var propertyName = reader.Value!.ToString()!;
					if (!reader.Read())
					{
						throw new System.Text.Json.JsonException("Unexpected end of JSON while reading property value.");
					}

					if (string.Equals(propertyName, serverKey, StringComparison.Ordinal))
					{
						removed = true;
						while (reader.TokenType == JsonToken.Comment) { if (!reader.Read()) break; }
						SkipValue(reader);
					}
					else
					{
						writer.WritePropertyName(propertyName);
						if (!ReadPastComments(reader, writer))
						{
							throw new System.Text.Json.JsonException("Unexpected end of JSON while reading property value.");
						}

						CopyValue(reader, writer);
					}

					break;
				case JsonToken.EndObject:
					writer.WriteEndObject();
					return removed;
			}
		}

		throw new System.Text.Json.JsonException("Unexpected end of JSON while reading object.");
	}

	private static void WriteEntryForMerge(
		JsonTextReader reader,
		JsonTextWriter writer,
		JObject newEntry)
	{
		if (reader.TokenType != JsonToken.StartObject)
		{
			newEntry.WriteTo(writer);
			SkipValue(reader);
			return;
		}

		writer.WriteStartObject();
		var remainingProperties = new HashSet<string>(
			newEntry.Properties().Select(static p => p.Name),
			StringComparer.Ordinal);

		while (reader.Read())
		{
			switch (reader.TokenType)
			{
				case JsonToken.Comment:
					writer.WriteComment(reader.Value?.ToString());
					break;
				case JsonToken.PropertyName:
					var propertyName = reader.Value!.ToString()!;
					if (!reader.Read())
					{
						throw new System.Text.Json.JsonException("Unexpected end of JSON while reading property value.");
					}

					if (!ReadPastComments(reader, writer))
					{
						throw new System.Text.Json.JsonException("Unexpected end of JSON while reading property value.");
					}

					if (remainingProperties.Remove(propertyName))
					{
						writer.WritePropertyName(propertyName);
						newEntry[propertyName]!.WriteTo(writer);
						SkipValue(reader);
					}
					else
					{
						writer.WritePropertyName(propertyName);
						CopyValue(reader, writer);
					}

					break;
				case JsonToken.EndObject:
					foreach (var pendingPropertyName in newEntry.Properties().Select(static p => p.Name))
					{
						if (!remainingProperties.Contains(pendingPropertyName))
						{
							continue;
						}

						writer.WritePropertyName(pendingPropertyName);
						newEntry[pendingPropertyName]!.WriteTo(writer);
					}

					writer.WriteEndObject();
					return;
			}
		}

		throw new System.Text.Json.JsonException("Unexpected end of JSON while reading object.");
	}

	private static void CopyValue(JsonTextReader reader, JsonTextWriter writer)
	{
		switch (reader.TokenType)
		{
			case JsonToken.StartObject:
				writer.WriteStartObject();
				while (reader.Read())
				{
					if (reader.TokenType == JsonToken.EndObject)
					{
						writer.WriteEndObject();
						return;
					}

					if (reader.TokenType == JsonToken.Comment)
					{
						writer.WriteComment(reader.Value?.ToString());
						continue;
					}

					if (reader.TokenType != JsonToken.PropertyName)
					{
						throw new System.Text.Json.JsonException("Unexpected token inside object.");
					}

					writer.WritePropertyName(reader.Value!.ToString()!);
					if (!reader.Read())
					{
						throw new System.Text.Json.JsonException("Unexpected end of JSON while reading property value.");
					}

					if (!ReadPastComments(reader, writer))
					{
						throw new System.Text.Json.JsonException("Unexpected end of JSON while reading property value.");
					}

					CopyValue(reader, writer);
				}

				break;
			case JsonToken.StartArray:
				writer.WriteStartArray();
				while (reader.Read())
				{
					if (reader.TokenType == JsonToken.EndArray)
					{
						writer.WriteEndArray();
						return;
					}

					if (reader.TokenType == JsonToken.Comment)
					{
						writer.WriteComment(reader.Value?.ToString());
						continue;
					}

					CopyValue(reader, writer);
				}

				break;
			case JsonToken.Integer:
				writer.WriteValue(Convert.ToInt64(reader.Value, CultureInfo.InvariantCulture));
				break;
			case JsonToken.Float:
				writer.WriteValue(Convert.ToDouble(reader.Value, CultureInfo.InvariantCulture));
				break;
			case JsonToken.String:
				writer.WriteValue(reader.Value?.ToString());
				break;
			case JsonToken.Boolean:
				writer.WriteValue(Convert.ToBoolean(reader.Value, CultureInfo.InvariantCulture));
				break;
			case JsonToken.Null:
			case JsonToken.Undefined:
				writer.WriteNull();
				break;
			case JsonToken.Date:
				writer.WriteValue((DateTime)reader.Value!);
				break;
			case JsonToken.Bytes:
				writer.WriteValue((byte[])reader.Value!);
				break;
			case JsonToken.Raw:
				writer.WriteRawValue(reader.Value?.ToString());
				break;
			case JsonToken.Comment:
				writer.WriteComment(reader.Value?.ToString());
				break;
			default:
				throw new System.Text.Json.JsonException($"Unsupported token type '{reader.TokenType}'.");
		}
	}

	/// <summary>
	/// Advances the reader past any <see cref="JsonToken.Comment"/> tokens,
	/// writing them to the writer. Returns <c>false</c> if the reader is exhausted.
	/// After this call, the reader is positioned on the first non-comment token.
	/// </summary>
	private static bool ReadPastComments(JsonTextReader reader, JsonTextWriter writer)
	{
		while (reader.TokenType == JsonToken.Comment)
		{
			writer.WriteComment(reader.Value?.ToString());
			if (!reader.Read())
			{
				return false;
			}
		}

		return true;
	}

	private static void SkipValue(JsonTextReader reader)
	{
		if (reader.TokenType is not (JsonToken.StartObject or JsonToken.StartArray))
		{
			return;
		}

		var depth = reader.Depth;
		while (reader.Read())
		{
			if (reader.TokenType is JsonToken.EndObject or JsonToken.EndArray && reader.Depth == depth)
			{
				return;
			}
		}
	}

	private static string FormatOutput(JObject root)
	{
		using var writer = CreateWriter();
		root.WriteTo(writer.JsonWriter);
		return writer.GetResult();
	}

	private sealed class JsonWriterScope(StringWriter stringWriter, JsonTextWriter jsonWriter) : IDisposable
	{
		public StringWriter StringWriter { get; } = stringWriter;

		public JsonTextWriter JsonWriter { get; } = jsonWriter;

		public string GetResult()
		{
			JsonWriter.Flush();
			var json = StringWriter.ToString();
			if (!json.EndsWith('\n'))
			{
				json += "\n";
			}

			return json;
		}

		public void Dispose()
		{
			JsonWriter.Close();
			StringWriter.Dispose();
		}
	}
}
