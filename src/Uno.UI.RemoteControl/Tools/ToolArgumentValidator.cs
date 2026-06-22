#nullable enable

using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Uno.UI.RemoteControl.Tools;

/// <summary>
/// Validates tool-invocation arguments against a <see cref="ToolDescriptor"/> before dispatch.
/// Pure and transport-agnostic: returns a human-readable error message, or <c>null</c> when valid.
/// </summary>
internal static class ToolArgumentValidator
{
	public static string? Validate(ToolDescriptor descriptor, JsonObject arguments)
	{
		foreach (var parameter in descriptor.Parameters)
		{
			// A JSON null is treated as absent (consistent with ToolInvocation.TryGet*), whether it
			// surfaces as a null node or a JsonValueKind.Null node: an optional null is ignored, a
			// required null yields the clearer "Missing required argument" error.
			var present = arguments.TryGetPropertyValue(parameter.Name, out var node)
				&& node is not null
				&& node.GetValueKind() != JsonValueKind.Null;

			if (!present)
			{
				if (parameter.IsRequired)
				{
					return $"Missing required argument '{parameter.Name}'.";
				}

				continue;
			}

			// The escape hatch owns its own shape; defer flat validation to the consumer's schema mapping.
			if (parameter.JsonSchema is not null)
			{
				continue;
			}

			if (!MatchesKind(node!, parameter.Kind))
			{
				return $"Argument '{parameter.Name}' must be of type {parameter.Kind}.";
			}

			// AllowedValues is an enum constraint on string values only — for non-string kinds the
			// comparison has no well-defined textual form, so it is not enforced.
			if (parameter.Kind == ToolParameterKind.String
				&& !parameter.AllowedValues.IsDefaultOrEmpty
				&& !IsAllowedString(node!, parameter.AllowedValues))
			{
				return $"Argument '{parameter.Name}' must be one of: {string.Join(", ", parameter.AllowedValues)}.";
			}
		}

		return null;
	}

	// GetValueKind reads the JSON type regardless of the node's CLR backing — unlike TryGetValue<T>,
	// which on a value-backed node (e.g. an int literal) only matches the exact CLR type.
	private static bool MatchesKind(JsonNode node, ToolParameterKind kind)
		=> kind switch
		{
			ToolParameterKind.String => node.GetValueKind() == JsonValueKind.String,
			// Integer keeps the typed accessor (GetInt32) honest: a fractional Number must fail here too.
			ToolParameterKind.Integer => node.GetValueKind() == JsonValueKind.Number && IsIntegral(node),
			ToolParameterKind.Number => node.GetValueKind() == JsonValueKind.Number,
			ToolParameterKind.Boolean => node.GetValueKind() is JsonValueKind.True or JsonValueKind.False,
			ToolParameterKind.Array => node.GetValueKind() == JsonValueKind.Array,
			ToolParameterKind.Object => node.GetValueKind() == JsonValueKind.Object,
			// Fail closed: a kind not handled above (a future enum value) surfaces as a validation
			// error rather than silently passing an unvalidated argument through.
			_ => false,
		};

	// The node is already a JSON Number; accept it as an integer only if ToolInvocation.GetInt32 could
	// read it — i.e. it fits Int32 — so validation never passes a value the accessor would then reject.
	private static bool IsIntegral(JsonNode node)
	{
		if (node is not JsonValue value)
		{
			return false;
		}

		// Element-backed (parsed from JSON text): the number token must be an Int32 integer
		// (ToJsonString can normalize 3.0 to "3", so the raw element is checked instead).
		if (value.TryGetValue<JsonElement>(out var element))
		{
			return element.TryGetInt32(out _);
		}

		// Value-backed (constructed in-process): accept only an Int32-typed CLR value, matching GetInt32.
		return value.TryGetValue<int>(out _);
	}

	private static bool IsAllowedString(JsonNode node, ImmutableArray<string> allowedValues)
		=> node is JsonValue value && value.TryGetValue<string>(out var s) && allowedValues.Contains(s);
}
