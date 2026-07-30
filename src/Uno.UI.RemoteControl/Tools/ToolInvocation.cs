#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
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
			? value
			: throw new ArgumentException($"Missing or non-string argument '{name}'.", nameof(name));

	public int GetInt32(string name)
		=> TryGetInt32(name, out var value)
			? value
			: throw new ArgumentException($"Missing or non-Int32 argument '{name}'.", nameof(name));

	public bool GetBoolean(string name)
		=> TryGetBoolean(name, out var value)
			? value
			: throw new ArgumentException($"Missing or non-Boolean argument '{name}'.", nameof(name));

	public bool TryGetString(string name, [NotNullWhen(true)] out string? value)
	{
		// Guard on `s is not null`: an explicitly-typed JSON null reads back as a non-failing null,
		// which would otherwise hand a null to a caller expecting a non-null string.
		if (Arguments.TryGetPropertyValue(name, out var node)
			&& node is JsonValue jsonValue
			&& jsonValue.TryGetValue(out string? s)
			&& s is not null)
		{
			value = s;
			return true;
		}

		value = null;
		return false;
	}

	public bool TryGetInt32(string name, out int value)
		=> TryGet(name, out value);

	public bool TryGetBoolean(string name, out bool value)
		=> TryGet(name, out value);

	private bool TryGet<T>(string name, out T value)
	{
		if (Arguments.TryGetPropertyValue(name, out var node) && node is JsonValue jsonValue && jsonValue.TryGetValue(out T? parsed) && parsed is not null)
		{
			value = parsed;
			return true;
		}

		value = default!;
		return false;
	}
}
