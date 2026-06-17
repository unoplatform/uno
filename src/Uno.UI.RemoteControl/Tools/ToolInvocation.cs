#nullable enable

using System;
using System.Text.Json.Nodes;

namespace Uno.UI.RemoteControl.Tools;

/// <summary>
/// The parsed arguments passed to a <see cref="ToolHandler"/>. Typed accessors throw
/// <see cref="ArgumentException"/> on a missing or wrong-typed argument; a throwing accessor is
/// caught by the registry and surfaced as an error <see cref="ToolResult"/> (see
/// <see cref="IToolCatalog.InvokeAsync"/>). Use the <c>TryGet*</c> overloads for optional values.
/// </summary>
internal sealed class ToolInvocation
{
	public ToolInvocation(JsonObject arguments)
		=> Arguments = arguments;

	/// <summary>The raw arguments, for shapes the typed accessors don't cover (arrays, objects).</summary>
	public JsonObject Arguments { get; }

	public string GetString(string name)
		=> TryGetString(name, out var value)
			? value!
			: throw new ArgumentException($"Missing or non-string argument '{name}'.", nameof(name));

	public int GetInt32(string name)
		=> Get<int>(name);

	public bool GetBoolean(string name)
		=> Get<bool>(name);

	public bool TryGetString(string name, out string? value)
	{
		if (Arguments.TryGetPropertyValue(name, out var node) && node is JsonValue jsonValue && jsonValue.TryGetValue(out string? s))
		{
			value = s;
			return true;
		}

		value = null;
		return false;
	}

	private T Get<T>(string name)
	{
		if (Arguments.TryGetPropertyValue(name, out var node) && node is JsonValue jsonValue && jsonValue.TryGetValue(out T? value))
		{
			return value!;
		}

		throw new ArgumentException($"Missing or non-{typeof(T).Name} argument '{name}'.", nameof(name));
	}
}
