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
	bool IsReadOnly)
{
	/// <summary>
	/// The declared parameters. Normalized so an uninitialized (<c>default</c>)
	/// <see cref="ImmutableArray{T}"/> reads as empty instead of throwing on enumeration.
	/// </summary>
	public ImmutableArray<ToolParameter> Parameters { get; init; }
		= Parameters.IsDefault ? ImmutableArray<ToolParameter>.Empty : Parameters;
}
