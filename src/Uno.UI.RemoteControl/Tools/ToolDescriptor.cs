#nullable enable

using System.Collections.Immutable;

namespace Uno.UI.RemoteControl.Tools;

/// <summary>
/// Describes a tool published into the <see cref="ToolRegistry"/>: a named, callable operation
/// with a typed parameter list. The vocabulary mirrors tools/resources without any reference to
/// the protocol a consumer may eventually expose it through.
/// </summary>
internal sealed record ToolDescriptor(
	string Name,
	string Description,
	ImmutableArray<ToolParameter> Parameters,
	bool IsReadOnly);

/// <summary>
/// A single tool parameter, described declaratively. A consumer maps this onto its own schema.
/// </summary>
/// <remarks>
/// When <see cref="JsonSchema"/> is non-null it is the escape hatch for shapes the flat model
/// can't express (nested objects/arrays, numeric constraints): a consumer uses it verbatim as the
/// parameter's schema body and ignores <see cref="Kind"/>. <see cref="IsRequired"/> still governs
/// whether the parameter is required, regardless of <see cref="JsonSchema"/>.
/// </remarks>
internal sealed record ToolParameter(
	string Name,
	string Description,
	ToolParameterKind Kind,
	bool IsRequired = false,
	string? DefaultValue = null,
	ImmutableArray<string> AllowedValues = default,
	string? JsonSchema = null);

internal enum ToolParameterKind
{
	String,
	Integer,
	Number,
	Boolean,
	Array,
	Object,
}
