using System.Collections.Generic;
using System.Dynamic;
using System.Text.Json;

namespace Uno.UI.Markup;

#nullable enable

/// <summary>
/// Converts JSON payloads into dynamic object graphs composed of <see cref="ExpandoObject"/> and <see cref="List{T}"/> instances.
/// </summary>
internal static class JsonToObjectParser
{
	private static readonly JsonDocumentOptions _documentOptions = new()
	{
		AllowTrailingCommas = true,
		CommentHandling = JsonCommentHandling.Skip
	};

	public static object? Parse(string json)
	{
		using var document = JsonDocument.Parse(json, _documentOptions);
		return ConvertElement(document.RootElement);
	}

	private static object? ConvertElement(in JsonElement element) => element.ValueKind switch
	{
		JsonValueKind.Object => ConvertObject(element),
		JsonValueKind.Array => ConvertArray(element),
		JsonValueKind.String => element.GetString(),
		JsonValueKind.Number => ConvertNumber(element),
		JsonValueKind.True => true,
		JsonValueKind.False => false,
		JsonValueKind.Null => null,
		JsonValueKind.Undefined => null,
		_ => null
	};

	private static object ConvertObject(in JsonElement element)
	{
		var expando = new ExpandoObject();
		var dictionary = (IDictionary<string, object?>)expando;

		foreach (var property in element.EnumerateObject())
		{
			dictionary[property.Name] = ConvertElement(property.Value);
		}

		return expando;
	}

	private static object ConvertArray(in JsonElement element)
	{
		var items = new List<object?>();

		foreach (var item in element.EnumerateArray())
		{
			items.Add(ConvertElement(item));
		}

		return items;
	}

	private static object ConvertNumber(in JsonElement element)
	{
		if (element.TryGetInt32(out var intValue))
		{
			return intValue;
		}

		return element.GetDouble();
	}
}
