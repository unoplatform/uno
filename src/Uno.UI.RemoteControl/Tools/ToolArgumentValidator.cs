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
			var present = arguments.TryGetPropertyValue(parameter.Name, out var node) && node is not null;

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
			_ => true,
		};

	// The node is already a JSON Number; an integer literal carries no fractional or exponent part.
	// GetValue<long> is backing-sensitive (fails on an int-backed node), so scan the raw number text.
	private static bool IsIntegral(JsonNode node)
		=> node.ToJsonString().IndexOfAny(NonIntegralChars) < 0;

	private static readonly char[] NonIntegralChars = ['.', 'e', 'E'];

	private static bool IsAllowedString(JsonNode node, ImmutableArray<string> allowedValues)
		=> node is JsonValue value && value.TryGetValue<string>(out var s) && allowedValues.Contains(s);
}
