#nullable enable

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Uno.UI.SourceGenerators.Internal.Incremental;

/// <summary>
/// A source location that, unlike <see cref="Location"/>, holds no reference to a syntax tree.
/// </summary>
internal sealed record LocationInfo(string FilePath, TextSpan Span, LinePositionSpan LineSpan)
{
	public Location ToLocation() => Location.Create(FilePath, Span, LineSpan);

	public static LocationInfo? From(Location? location)
		=> location?.SourceTree is { } tree
			? new LocationInfo(tree.FilePath, location.SourceSpan, location.GetLineSpan().Span)
			: null;
}

/// <summary>
/// A diagnostic that can flow through an incremental pipeline: the descriptor is referenced by id and the message
/// arguments are pre-formatted strings.
/// </summary>
internal sealed record DiagnosticInfo(string DescriptorId, LocationInfo? Location, EquatableArray<string> MessageArguments)
{
	public static DiagnosticInfo Create(DiagnosticDescriptor descriptor, Location? location, params string[] messageArguments)
		=> new(descriptor.Id, LocationInfo.From(location), messageArguments.ToEquatableArray());

	public Diagnostic ToDiagnostic(DiagnosticDescriptor descriptor)
		=> Diagnostic.Create(descriptor, Location?.ToLocation(), MessageArguments.Cast<object>().ToArray());
}
