#nullable enable

using System.Collections.Immutable;

namespace Uno.UI.RemoteControl.Tools;

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
	string? JsonSchema = null)
{
	/// <summary>
	/// Optional enum constraint on the value. Normalized so an uninitialized (<c>default</c>)
	/// <see cref="ImmutableArray{T}"/> reads as empty instead of throwing on enumeration.
	/// </summary>
	public ImmutableArray<string> AllowedValues { get; init; }
		= AllowedValues.IsDefault ? ImmutableArray<string>.Empty : AllowedValues;
}
