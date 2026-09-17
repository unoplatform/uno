#nullable enable

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Uno.UI.SourceGenerators.DependencyObject;

/// <summary>
/// Diagnostics reported by <see cref="DependencyPropertyGenerator"/> (UnoInternal0010 to UnoInternal0024).
/// </summary>
internal static class DependencyPropertyDiagnostics
{
	private const string Category = "Usage";

	// These diagnostics are internal to the repository and never shipped, so there is no release to track.
#pragma warning disable RS2008 // Enable analyzer release tracking
	public static readonly DiagnosticDescriptor NotPartialDefinition = new(
		"UnoInternal0010",
		"Generated dependency property must be a partial definition",
		"'{0}' must be a partial definition without an implementation part to use [GeneratedDependencyProperty]",
		Category,
		DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	public static readonly DiagnosticDescriptor LegacyIdentifierDeclaration = new(
		"UnoInternal0011",
		"Migrate to a partial property",
		"The generator now declares '{0}': move [GeneratedDependencyProperty] to the partial property '{1}' (or a static partial 'Get{1}' method for an attached property) and remove '{0}'",
		Category,
		DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	public static readonly DiagnosticDescriptor UnsupportedPropertyShape = new(
		"UnoInternal0012",
		"Unsupported generated dependency property shape",
		"'{0}' cannot be a generated dependency property: {1}",
		Category,
		DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	public static readonly DiagnosticDescriptor InvalidContainingType = new(
		"UnoInternal0013",
		"Invalid containing type for a generated dependency property",
		"'{0}' cannot declare the generated dependency property '{1}': {2}",
		Category,
		DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	public static readonly DiagnosticDescriptor InvalidAttachedAccessor = new(
		"UnoInternal0014",
		"Invalid attached property accessor",
		"'{0}' is not a valid attached property accessor: {1}",
		Category,
		DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	public static readonly DiagnosticDescriptor IdentifierConflict = new(
		"UnoInternal0015",
		"Dependency property identifier already declared",
		"'{0}' already declares '{1}', which the generator declares; remove it, or declare it as 'static partial DependencyProperty {1} {{ get; }}' to customize it",
		Category,
		DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	public static readonly DiagnosticDescriptor InvalidIdentifierDeclaration = new(
		"UnoInternal0016",
		"Invalid dependency property identifier declaration",
		"'{0}' must be a static get-only partial property definition of type DependencyProperty, without an implementation part",
		Category,
		DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	public static readonly DiagnosticDescriptor DefaultValueConflict = new(
		"UnoInternal0017",
		"Conflicting default values",
		"'{0}' sets DefaultValue and also has a '{1}' method; use only one of them",
		Category,
		DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	public static readonly DiagnosticDescriptor InvalidDefaultValueMethod = new(
		"UnoInternal0018",
		"Invalid default value method",
		"'{0}' must be a static parameterless method that returns a value",
		Category,
		DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	public static readonly DiagnosticDescriptor IncompatibleDefaultValue = new(
		"UnoInternal0019",
		"Incompatible default value",
		"DefaultValue '{0}' is not compatible with the type '{1}' of the dependency property '{2}'",
		Category,
		DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	public static readonly DiagnosticDescriptor InvalidChangedCallback = new(
		"UnoInternal0020",
		"Invalid property changed callback",
		"No '{0}' method has a supported signature for the dependency property '{1}'; expected {2}",
		Category,
		DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	public static readonly DiagnosticDescriptor InvalidCoerceCallback = new(
		"UnoInternal0021",
		"Invalid coerce callback",
		"No '{0}' method has a supported signature for the dependency property '{1}'; expected {2}",
		Category,
		DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	public static readonly DiagnosticDescriptor LocalCacheRequiresBackingFieldOwner = new(
		"UnoInternal0022",
		"Attached local cache requires a backing field owner",
		"LocalCache on the attached property '{0}' requires AttachedBackingFieldOwner",
		Category,
		DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	public static readonly DiagnosticDescriptor InvalidBackingFieldOwner = new(
		"UnoInternal0023",
		"Invalid backing field owner",
		"AttachedBackingFieldOwner '{0}' of the attached property '{1}' must be a non-generic partial class declared in this compilation, related to the target type '{2}'",
		Category,
		DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	public static readonly DiagnosticDescriptor WeakStorageWithLocalCache = new(
		"UnoInternal0024",
		"Weak storage with a local cache",
		"'{0}' uses FrameworkPropertyMetadataOptions.WeakStorage, which a local cache would defeat by holding a strong reference; set LocalCache = false",
		Category,
		DiagnosticSeverity.Error,
		isEnabledByDefault: true);
#pragma warning restore RS2008 // Enable analyzer release tracking

	public static ImmutableDictionary<string, DiagnosticDescriptor> ById { get; } = new[]
	{
		NotPartialDefinition,
		LegacyIdentifierDeclaration,
		UnsupportedPropertyShape,
		InvalidContainingType,
		InvalidAttachedAccessor,
		IdentifierConflict,
		InvalidIdentifierDeclaration,
		DefaultValueConflict,
		InvalidDefaultValueMethod,
		IncompatibleDefaultValue,
		InvalidChangedCallback,
		InvalidCoerceCallback,
		LocalCacheRequiresBackingFieldOwner,
		InvalidBackingFieldOwner,
		WeakStorageWithLocalCache,
	}.ToImmutableDictionary(d => d.Id);
}
