#nullable enable

using System;
using System.Buffers;
using System.Text;
using System.Text.Json;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// Strips Skottie's "SkSL Shader" / "SkSL Color Filter" effects from Lottie JSON. Skottie compiles their source with
/// Skia's SkSL compiler, which crafted input can exploit (CVE-2026-119261), and After Effects exports never contain them.
/// </summary>
internal static class SkottieSkslEffectFilter
{
	private static readonly JsonDocumentOptions _options = new() { AllowTrailingCommas = true, MaxDepth = 256 };

	/// <returns>The JSON without SkSL effects (the same instance when there were none), or <c>null</c> when it may hold
	/// one but cannot be parsed.</returns>
	public static string? Filter(string json)
	{
		// Only a \u escape can spell "SkSL" without the literal text.
		if (!json.Contains("SkSL", StringComparison.Ordinal) && !json.Contains("\\u", StringComparison.Ordinal))
		{
			return json;
		}

		try
		{
			using var document = JsonDocument.Parse(json, _options);
			if (!ContainsSkslEffect(document.RootElement))
			{
				return json;
			}

			ArrayBufferWriter<byte> buffer = new(json.Length);
			using (Utf8JsonWriter writer = new(buffer))
			{
				Write(writer, document.RootElement);
			}

			return Encoding.UTF8.GetString(buffer.WrittenSpan);
		}
		catch (Exception e) when (e is JsonException or ArgumentException or InvalidOperationException)
		{
			return null;
		}
	}

	private static bool ContainsSkslEffect(JsonElement element)
	{
		switch (element.ValueKind)
		{
			case JsonValueKind.Object:
				if (IsSkslEffect(element))
				{
					return true;
				}

				foreach (var property in element.EnumerateObject())
				{
					if (ContainsSkslEffect(property.Value))
					{
						return true;
					}
				}

				return false;

			case JsonValueKind.Array:
				foreach (var item in element.EnumerateArray())
				{
					if (ContainsSkslEffect(item))
					{
						return true;
					}
				}

				return false;

			default:
				return false;
		}
	}

	// Replaces each SkSL effect with null, which Skottie skips.
	private static void Write(Utf8JsonWriter writer, JsonElement element)
	{
		switch (element.ValueKind)
		{
			case JsonValueKind.Object when IsSkslEffect(element):
				writer.WriteNullValue();
				break;

			case JsonValueKind.Object:
				writer.WriteStartObject();
				foreach (var property in element.EnumerateObject())
				{
					writer.WritePropertyName(property.Name);
					Write(writer, property.Value);
				}

				writer.WriteEndObject();
				break;

			case JsonValueKind.Array:
				writer.WriteStartArray();
				foreach (var item in element.EnumerateArray())
				{
					Write(writer, item);
				}

				writer.WriteEndArray();
				break;

			default:
				element.WriteTo(writer);
				break;
		}
	}

	// Checks every "mn", as duplicate keys may resolve differently in Skottie's parser.
	private static bool IsSkslEffect(JsonElement element)
	{
		foreach (var property in element.EnumerateObject())
		{
			if (property.NameEquals("mn") && property.Value.ValueKind == JsonValueKind.String && IsSkslMatchName(property.Value.GetString()!))
			{
				return true;
			}
		}

		return false;
	}

	// Skottie compares "mn" with strcmp, so anything after an embedded NUL is ignored.
	private static bool IsSkslMatchName(string name)
	{
		var end = name.IndexOf('\0');
		var span = end < 0 ? name.AsSpan() : name.AsSpan(0, end);
		return span.SequenceEqual("SkSL Shader") || span.SequenceEqual("SkSL Color Filter");
	}
}
