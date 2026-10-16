#nullable enable

using Uno.UI.SourceGenerators.Internal.Incremental;

namespace Uno.UI.SourceGenerators.DependencyObject.Models;

/// <summary>
/// The result of transforming one <c>[GeneratedDependencyProperty]</c> target: either a property to generate, or the
/// diagnostics explaining why it can't be.
/// </summary>
internal sealed record DependencyPropertyResult(HierarchyInfo ContainingType, DependencyPropertyInfo? Property, EquatableArray<DiagnosticInfo> Diagnostics);

/// <summary>
/// The dependency properties generated into one containing type, which produces one source file.
/// </summary>
/// <param name="HintName">The hint name of the source file, unique ignoring case among all the generated files.</param>
internal sealed record DependencyPropertyTypeInfo(HierarchyInfo ContainingType, string HintName, EquatableArray<DependencyPropertyInfo> Properties);

/// <param name="Name">The property name, without the <c>Property</c> suffix.</param>
/// <param name="PropertyType">The fully qualified property type.</param>
/// <param name="IsBoolean">Whether the property type is <see cref="bool"/>, whose cached value is bit-packed.</param>
/// <param name="Identifier">How the <c>{Name}Property</c> identifier is declared.</param>
/// <param name="Instance">The CLR property accessors, for an instance property.</param>
/// <param name="Attached">The Get/Set accessors, for an attached property.</param>
/// <param name="DefaultValue">The metadata default value.</param>
/// <param name="Options">The <c>FrameworkPropertyMetadataOptions</c> expression.</param>
/// <param name="ChangedCallback">The callback invocation, in terms of <c>instance</c> and <c>args</c>.</param>
/// <param name="CoerceCallback">The callback invocation, in terms of <c>instance</c>, <c>baseValue</c> and <c>precedence</c>.</param>
/// <param name="LocalCache">Whether the value is cached in a backing field.</param>
internal sealed record DependencyPropertyInfo(
	string Name,
	string PropertyType,
	bool IsBoolean,
	IdentifierInfo Identifier,
	InstancePropertyInfo? Instance,
	AttachedPropertyInfo? Attached,
	DefaultValueInfo DefaultValue,
	string Options,
	string? ChangedCallback,
	string? CoerceCallback,
	bool LocalCache);

/// <param name="Modifiers">The modifiers of the identifier declaration.</param>
/// <param name="IsExplicit">Whether the user declared the identifier as a partial property, which the generator implements.</param>
internal sealed record IdentifierInfo(string Modifiers, bool IsExplicit);

/// <param name="Modifiers">The modifiers of the partial property definition, echoed on the implementation.</param>
/// <param name="GetterModifiers">The get accessor modifiers.</param>
/// <param name="SetterModifiers">The set accessor modifiers, or <see langword="null"/> when there is no setter.</param>
internal sealed record InstancePropertyInfo(string Modifiers, string GetterModifiers, string? SetterModifiers);

/// <param name="TargetType">The fully qualified type of the target parameter.</param>
/// <param name="Getter">The <c>Get{Name}</c> partial definition.</param>
/// <param name="Setter">The <c>Set{Name}</c> partial definition to implement, if any.</param>
/// <param name="BackingFieldOwner">The type holding the local cache, when enabled.</param>
internal sealed record AttachedPropertyInfo(
	string TargetType,
	AttachedAccessorInfo Getter,
	AttachedAccessorInfo? Setter,
	BackingFieldOwnerInfo? BackingFieldOwner);

/// <param name="Modifiers">The modifiers of the partial method definition, echoed on the implementation.</param>
/// <param name="TargetParameterName">The escaped name of the target parameter.</param>
/// <param name="ValueParameterName">The escaped name of the value parameter, for a setter.</param>
/// <param name="IsExtension">Whether the target parameter has the <c>this</c> modifier.</param>
internal sealed record AttachedAccessorInfo(string Modifiers, string TargetParameterName, string? ValueParameterName, bool IsExtension);

/// <param name="ContainingType">The owner type, reopened to add the backing fields.</param>
/// <param name="FieldName">The backing field name, unique across the types that declare attached properties.</param>
internal sealed record BackingFieldOwnerInfo(HierarchyInfo ContainingType, string FieldName);

/// <param name="Expression">The default value expression.</param>
/// <param name="CachedBox">The cached box holding this exact value, as <c>{Holder}.{Field}</c> in <c>Uno.UI.Helpers.Boxes</c>, if any.</param>
/// <param name="BoxableType">The fully qualified type to look up a <c>Boxer.Box</c> overload for, if boxing through it is worthwhile.</param>
internal sealed record DefaultValueInfo(string Expression, string? CachedBox, string? BoxableType);

/// <summary>
/// What the compilation offers from the <c>Uno.UI.Helpers.Boxes</c> namespace.
/// </summary>
/// <param name="IsAccessible">Whether <c>Boxer</c> can be used from generated code.</param>
/// <param name="BoxableTypes">The fully qualified parameter types of the <c>Boxer.Box</c> overloads.</param>
/// <param name="CachedBoxes">The cached box fields, as <c>{Holder}.{Field}</c>.</param>
internal sealed record BoxesInfo(bool IsAccessible, EquatableArray<string> BoxableTypes, EquatableArray<string> CachedBoxes)
{
	public const string Namespace = "Uno.UI.Helpers.Boxes";

	public const string BoxerMetadataName = Namespace + ".Boxer";
}
