#nullable enable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Uno.UI.SourceGenerators.DependencyObject.Models;
using Uno.UI.SourceGenerators.Internal.Incremental;

namespace Uno.UI.SourceGenerators.DependencyObject;

/// <summary>
/// Turns one <c>[GeneratedDependencyProperty]</c> target into an equatable model. Invalid input produces diagnostics
/// instead of a property; nothing here throws.
/// </summary>
/// <remarks>
/// The XAML generator's <c>HasGeneratedDependencyProperty</c> treats any attributed property or single-parameter static
/// <c>Get{Name}</c> method as a DP, so every shape accepted here must stay within that.
/// </remarks>
internal sealed partial class DependencyPropertyModelBuilder
{
	public const string AttributeMetadataName = "Uno.UI.Xaml.GeneratedDependencyPropertyAttribute";

	private const string PropertySuffix = "Property";
	private const string XamlNamespace = "Microsoft.UI.Xaml";

	private static readonly SymbolDisplayFormat s_fullyQualifiedFormat = SymbolDisplayFormat.FullyQualifiedFormat;

	private readonly GeneratorAttributeSyntaxContext _context;
	private readonly CancellationToken _cancellationToken;
	private readonly Compilation _compilation;
	private readonly INamedTypeSymbol _containingType;
	private readonly HierarchyInfo _hierarchy;
	private readonly AttributeArguments _arguments;
	private readonly ImmutableArray<DiagnosticInfo>.Builder _diagnostics = ImmutableArray.CreateBuilder<DiagnosticInfo>();

	private DependencyPropertyModelBuilder(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
	{
		_context = context;
		_cancellationToken = cancellationToken;
		_compilation = context.SemanticModel.Compilation;
		_containingType = context.TargetSymbol.ContainingType;
		_hierarchy = HierarchyInfo.From(_containingType);
		_arguments = AttributeArguments.Read(context.Attributes[0], cancellationToken);
	}

	public static DependencyPropertyResult Build(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();

		var builder = new DependencyPropertyModelBuilder(context, cancellationToken);
		var property = context.TargetSymbol switch
		{
			IPropertySymbol propertySymbol => builder.BuildInstanceProperty(propertySymbol),
			IMethodSymbol methodSymbol => builder.BuildAttachedProperty(methodSymbol),
			_ => null,
		};

		return new DependencyPropertyResult(
			builder._hierarchy,
			builder._diagnostics.Count == 0 ? property : null,
			new EquatableArray<DiagnosticInfo>(builder._diagnostics.ToImmutable()));
	}

	private DependencyPropertyInfo? BuildInstanceProperty(IPropertySymbol property)
	{
		var location = GetIdentifierLocation(_context.TargetNode);
		var displayName = $"{_containingType.Name}.{property.Name}";

		if (property.IsStatic && IsXamlType(property.Type, "DependencyProperty") && property.Name.Length > PropertySuffix.Length && property.Name.EndsWith(PropertySuffix, System.StringComparison.Ordinal))
		{
			Report(DependencyPropertyDiagnostics.LegacyIdentifierDeclaration, location, property.Name, property.Name.Substring(0, property.Name.Length - PropertySuffix.Length));
			return null;
		}

		var unsupportedReason = property switch
		{
			{ IsIndexer: true } => "indexers are not supported",
			{ IsStatic: true } => "static properties are not supported, use a static partial 'Get' method for an attached property",
			{ IsAbstract: true } => "abstract properties are not supported",
			{ ExplicitInterfaceImplementations.Length: > 0 } => "explicit interface implementations are not supported",
			{ ReturnsByRef: true } or { ReturnsByRefReadonly: true } => "ref-returning properties are not supported",
			{ GetMethod: null } => "a get accessor is required",
			{ SetMethod.IsInitOnly: true } => "init accessors are not supported",
			_ => null,
		};

		if (unsupportedReason is not null)
		{
			Report(DependencyPropertyDiagnostics.UnsupportedPropertyShape, location, displayName, unsupportedReason);
			return null;
		}

		if (!property.IsPartialDefinition || property.PartialImplementationPart is not null || _context.TargetNode is not PropertyDeclarationSyntax syntax)
		{
			Report(DependencyPropertyDiagnostics.NotPartialDefinition, location, displayName);
			return null;
		}

		if (!ValidateContainingType(property.Name, isAttached: false, location))
		{
			return null;
		}

		var propertyType = property.Type;
		var identifier = ResolveIdentifier(property.Name, GetAccessibilityKeywords(syntax.Modifiers), location);
		var defaultValue = ResolveDefaultValue(property.Name, propertyType, location, displayName);
		var changedCallback = ResolveChangedCallback(property.Name, propertyType, targetType: null, location);
		var coerceCallback = ResolveCoerceCallback(property.Name, propertyType, targetType: null, location);
		var options = FormatOptions(out var hasWeakStorage);

		var localCache = _arguments.LocalCache ?? true;
		if (localCache && hasWeakStorage)
		{
			Report(DependencyPropertyDiagnostics.WeakStorageWithLocalCache, location, displayName);
		}

		if (identifier is null || defaultValue is null || _diagnostics.Count > 0)
		{
			return null;
		}

		string getterModifiers = "";
		string? setterModifiers = null;
		foreach (var accessor in syntax.AccessorList?.Accessors ?? default)
		{
			if (accessor.IsKind(SyntaxKind.GetAccessorDeclaration))
			{
				getterModifiers = JoinModifiers(accessor.Modifiers);
			}
			else if (accessor.IsKind(SyntaxKind.SetAccessorDeclaration))
			{
				setterModifiers = JoinModifiers(accessor.Modifiers);
			}
		}

		return new DependencyPropertyInfo(
			Name: property.Name,
			PropertyType: propertyType.ToDisplayString(s_fullyQualifiedFormat),
			IsBoolean: propertyType.SpecialType == SpecialType.System_Boolean,
			Identifier: identifier,
			Instance: new InstancePropertyInfo(JoinModifiers(syntax.Modifiers), getterModifiers, setterModifiers),
			Attached: null,
			DefaultValue: defaultValue,
			Options: options,
			ChangedCallback: changedCallback,
			CoerceCallback: coerceCallback,
			LocalCache: localCache);
	}

	private DependencyPropertyInfo? BuildAttachedProperty(IMethodSymbol getter)
	{
		var location = GetIdentifierLocation(_context.TargetNode);
		var displayName = $"{_containingType.Name}.{getter.Name}";

		if (getter.Name.Length <= 3 ||
			!getter.Name.StartsWith("Get", System.StringComparison.Ordinal) ||
			!getter.IsStatic ||
			getter.ReturnsVoid ||
			getter.ReturnsByRef ||
			getter.ReturnsByRefReadonly ||
			getter.IsGenericMethod ||
			getter.Parameters.Length != 1 ||
			getter.Parameters[0].RefKind != RefKind.None ||
			!IsDependencyObject(getter.Parameters[0].Type))
		{
			Report(
				DependencyPropertyDiagnostics.InvalidAttachedAccessor,
				location,
				displayName,
				"[GeneratedDependencyProperty] on a method requires a static non-generic 'Get{Name}' method that returns the value and takes a single DependencyObject parameter");
			return null;
		}

		if (!getter.IsPartialDefinition || getter.PartialImplementationPart is not null || _context.TargetNode is not MethodDeclarationSyntax syntax)
		{
			Report(DependencyPropertyDiagnostics.NotPartialDefinition, location, displayName);
			return null;
		}

		var name = getter.Name.Substring(3);
		if (!ValidateContainingType(name, isAttached: true, location))
		{
			return null;
		}

		var propertyType = getter.ReturnType;
		var targetParameter = getter.Parameters[0];
		var setter = ResolveAttachedSetter(name, targetParameter.Type, propertyType);
		var identifier = ResolveIdentifier(name, GetAccessibilityKeywords(syntax.Modifiers), location);
		var defaultValue = ResolveDefaultValue(name, propertyType, location, displayName);
		var changedCallback = ResolveChangedCallback(name, propertyType, targetParameter.Type, location);
		var coerceCallback = ResolveCoerceCallback(name, propertyType, targetParameter.Type, location);
		var options = FormatOptions(out var hasWeakStorage);

		BackingFieldOwnerInfo? backingFieldOwner = null;
		if (_arguments.AttachedBackingFieldOwner is { } owner && _arguments.LocalCache != false)
		{
			backingFieldOwner = ResolveBackingFieldOwner(owner, targetParameter.Type, name, location, displayName);
			if (hasWeakStorage)
			{
				Report(DependencyPropertyDiagnostics.WeakStorageWithLocalCache, location, displayName);
			}
		}
		else if (_arguments.LocalCache == true && _arguments.AttachedBackingFieldOwner is null)
		{
			Report(DependencyPropertyDiagnostics.LocalCacheRequiresBackingFieldOwner, _arguments.GetLocation(AttributeArguments.LocalCacheName) ?? location, displayName);
		}

		if (identifier is null || defaultValue is null || _diagnostics.Count > 0)
		{
			return null;
		}

		return new DependencyPropertyInfo(
			Name: name,
			PropertyType: propertyType.ToDisplayString(s_fullyQualifiedFormat),
			IsBoolean: propertyType.SpecialType == SpecialType.System_Boolean,
			Identifier: identifier,
			Instance: null,
			Attached: new AttachedPropertyInfo(
				TargetType: targetParameter.Type.ToDisplayString(s_fullyQualifiedFormat),
				Getter: new AttachedAccessorInfo(JoinModifiers(syntax.Modifiers), EscapeIdentifier(targetParameter.Name), ValueParameterName: null, IsExtension: getter.IsExtensionMethod),
				Setter: setter,
				BackingFieldOwner: backingFieldOwner),
			DefaultValue: defaultValue,
			Options: options,
			ChangedCallback: changedCallback,
			CoerceCallback: coerceCallback,
			LocalCache: backingFieldOwner is not null);
	}

	private AttachedAccessorInfo? ResolveAttachedSetter(string name, ITypeSymbol targetType, ITypeSymbol propertyType)
	{
		// Only partial definitions are implemented; a hand-written setter (e.g. one that validates) is left alone.
		foreach (var setter in GetOrdinaryMethods("Set" + name))
		{
			if (!setter.IsPartialDefinition || setter.PartialImplementationPart is not null)
			{
				continue;
			}

			var isValid = setter.IsStatic &&
				setter.ReturnsVoid &&
				!setter.IsGenericMethod &&
				setter.Parameters.Length == 2 &&
				setter.Parameters.All(p => p.RefKind == RefKind.None) &&
				IsSameTypeOrUnresolved(setter.Parameters[0].Type, targetType) &&
				IsSameTypeOrUnresolved(setter.Parameters[1].Type, propertyType);

			if (!isValid || GetSyntax<MethodDeclarationSyntax>(setter) is not { } syntax)
			{
				Report(
					DependencyPropertyDiagnostics.InvalidAttachedAccessor,
					GetSourceLocation(setter),
					$"{_containingType.Name}.{setter.Name}",
					$"a partial 'Set{name}' method must be static, return void and take ({targetType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)}, {propertyType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)})");
				return null;
			}

			return new AttachedAccessorInfo(
				JoinModifiers(syntax.Modifiers),
				EscapeIdentifier(setter.Parameters[0].Name),
				EscapeIdentifier(setter.Parameters[1].Name),
				setter.IsExtensionMethod);
		}

		return null;
	}

	private bool ValidateContainingType(string name, bool isAttached, Location? location)
	{
		var isValidKind = _containingType.TypeKind == TypeKind.Class && (isAttached
			? _containingType.IsStatic || IsDependencyObject(_containingType)
			: !_containingType.IsStatic && IsDependencyObject(_containingType));

		if (!isValidKind)
		{
			Report(
				DependencyPropertyDiagnostics.InvalidContainingType,
				location,
				_containingType.Name,
				name,
				isAttached ? "attached properties must be declared in a static class or a class deriving from DependencyObject" : "the type must be a class deriving from DependencyObject");
			return false;
		}

		// A type with several declarations that aren't all partial is already an error, so checking the declarations
		// enclosing the target is enough.
		foreach (var declaration in _context.TargetNode.Ancestors().OfType<TypeDeclarationSyntax>())
		{
			if (!declaration.Modifiers.Any(SyntaxKind.PartialKeyword))
			{
				Report(DependencyPropertyDiagnostics.InvalidContainingType, location, _containingType.Name, name, $"'{declaration.Identifier.ValueText}' must be partial");
				return false;
			}
		}

		return true;
	}

	private IdentifierInfo? ResolveIdentifier(string name, string accessibility, Location? location)
	{
		var identifierName = name + PropertySuffix;

		foreach (var member in _containingType.GetMembers(identifierName))
		{
			if (member is IPropertySymbol { IsPartialDefinition: true } declaration)
			{
				if (declaration is { IsStatic: true, IsIndexer: false, SetMethod: null, GetMethod: not null, PartialImplementationPart: null, RefKind: RefKind.None } &&
					IsXamlType(declaration.Type, "DependencyProperty") &&
					GetSyntax<PropertyDeclarationSyntax>(declaration) is { } syntax)
				{
					return new IdentifierInfo(JoinModifiers(syntax.Modifiers), IsExplicit: true);
				}

				Report(DependencyPropertyDiagnostics.InvalidIdentifierDeclaration, GetSourceLocation(declaration) ?? location, $"{_containingType.Name}.{identifierName}");
				return null;
			}

			Report(DependencyPropertyDiagnostics.IdentifierConflict, location, _containingType.Name, identifierName);
			return null;
		}

		var modifiers = accessibility;
		if (HidesInheritedIdentifier(name, identifierName))
		{
			modifiers = modifiers.Length > 0 ? modifiers + " new" : "new";
		}

		return new IdentifierInfo(modifiers.Length > 0 ? modifiers + " static" : "static", IsExplicit: false);
	}

	/// <summary>
	/// Whether a base type has an accessible <c>{Name}Property</c>, including one this generator declares, whose
	/// output isn't visible to the transform.
	/// </summary>
	private bool HidesInheritedIdentifier(string name, string identifierName)
	{
		for (var baseType = _containingType.BaseType; baseType is not null; baseType = baseType.BaseType)
		{
			foreach (var member in baseType.GetMembers(identifierName))
			{
				if (_compilation.IsSymbolAccessibleWithin(member, _containingType))
				{
					return true;
				}
			}

			if (baseType.DeclaringSyntaxReferences.Length == 0)
			{
				continue;
			}

			foreach (var member in baseType.GetMembers(name).Concat(baseType.GetMembers("Get" + name)))
			{
				if (member is IPropertySymbol or IMethodSymbol &&
					HasGeneratedDependencyPropertyAttribute(member) &&
					_compilation.IsSymbolAccessibleWithin(member, _containingType))
				{
					return true;
				}
			}
		}

		return false;
	}

	private BackingFieldOwnerInfo? ResolveBackingFieldOwner(ITypeSymbol owner, ITypeSymbol targetType, string name, Location? location, string displayName)
	{
		// Without the owner there is no cache, but the property itself can still be generated.
		if (IsUnresolved(owner))
		{
			return null;
		}

		var isValid = owner is INamedTypeSymbol { TypeKind: TypeKind.Class, IsStatic: false } &&
			SymbolEqualityComparer.Default.Equals(owner.ContainingAssembly, _compilation.Assembly);

		for (var type = owner as INamedTypeSymbol; isValid && type is not null; type = type.ContainingType)
		{
			isValid = type.TypeParameters.Length == 0 && IsPartial(type);
		}

		// The generated getter tests whether the target is an owner, which doesn't compile when it can never be one.
		if (isValid && !IsUnresolved(targetType))
		{
			var conversion = _compilation.ClassifyCommonConversion(targetType, owner);
			isValid = conversion.Exists && !conversion.IsUserDefined;
		}

		if (!isValid)
		{
			Report(
				DependencyPropertyDiagnostics.InvalidBackingFieldOwner,
				_arguments.GetLocation(AttributeArguments.AttachedBackingFieldOwnerName) ?? location,
				owner.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
				displayName,
				targetType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
			return null;
		}

		var fieldPrefix = new System.Text.StringBuilder("__");
		foreach (var c in _hierarchy.MetadataName)
		{
			fieldPrefix.Append(char.IsLetterOrDigit(c) ? c : '_');
		}

		return new BackingFieldOwnerInfo(HierarchyInfo.From((INamedTypeSymbol)owner), $"{fieldPrefix}_{name}PropertyBackingField");
	}

	private string FormatOptions(out bool hasWeakStorage)
	{
		hasWeakStorage = false;

		if (_arguments.Options is { Kind: TypedConstantKind.Enum, Type: INamedTypeSymbol optionsType } options &&
			ConstantFormatter.TryGetBits(options.Value, out var bits))
		{
			var weakStorage = optionsType.GetMembers("WeakStorage").OfType<IFieldSymbol>().FirstOrDefault();
			hasWeakStorage = weakStorage is { HasConstantValue: true } &&
				ConstantFormatter.TryGetBits(weakStorage.ConstantValue, out var weakStorageBits) &&
				weakStorageBits != 0 &&
				(bits & weakStorageBits) == weakStorageBits;

			return ConstantFormatter.FormatEnum(optionsType, bits);
		}

		return "global::Microsoft.UI.Xaml.FrameworkPropertyMetadataOptions.None";
	}

	private IEnumerable<IMethodSymbol> GetOrdinaryMethods(string name)
		=> _containingType.GetMembers(name).OfType<IMethodSymbol>().Where(m => m.MethodKind == MethodKind.Ordinary);

	private void Report(DiagnosticDescriptor descriptor, Location? location, params string[] messageArguments)
		=> _diagnostics.Add(DiagnosticInfo.Create(descriptor, location, messageArguments));

	private T? GetSyntax<T>(ISymbol symbol)
		where T : SyntaxNode
	{
		foreach (var reference in symbol.DeclaringSyntaxReferences)
		{
			if (reference.GetSyntax(_cancellationToken) is T syntax)
			{
				return syntax;
			}
		}

		return null;
	}

	private static Location? GetSourceLocation(ISymbol symbol)
		=> symbol.Locations.FirstOrDefault(l => l.IsInSource);

	private static Location GetIdentifierLocation(SyntaxNode node)
		=> node switch
		{
			PropertyDeclarationSyntax property => property.Identifier.GetLocation(),
			IndexerDeclarationSyntax indexer => indexer.ThisKeyword.GetLocation(),
			MethodDeclarationSyntax method => method.Identifier.GetLocation(),
			_ => node.GetLocation(),
		};

	private static string JoinModifiers(SyntaxTokenList modifiers)
		=> string.Join(" ", modifiers.Select(m => m.Text));

	private static string GetAccessibilityKeywords(SyntaxTokenList modifiers)
		=> string.Join(" ", modifiers
			.Where(m => m.IsKind(SyntaxKind.PublicKeyword) || m.IsKind(SyntaxKind.InternalKeyword) || m.IsKind(SyntaxKind.ProtectedKeyword) || m.IsKind(SyntaxKind.PrivateKeyword))
			.Select(m => m.Text));

	private bool IsPartial(INamedTypeSymbol type)
		=> type.DeclaringSyntaxReferences.Length > 0 &&
			type.DeclaringSyntaxReferences[0].GetSyntax(_cancellationToken) is TypeDeclarationSyntax declaration &&
			declaration.Modifiers.Any(SyntaxKind.PartialKeyword);

	private static bool HasGeneratedDependencyPropertyAttribute(ISymbol symbol)
		=> symbol.GetAttributes().Any(a => a.AttributeClass is { Name: "GeneratedDependencyPropertyAttribute" } attributeClass &&
			IsInNamespace(attributeClass, "Uno.UI.Xaml"));

	/// <summary>
	/// Whether the type is or derives from DependencyObject, or can't be checked because it or a base type is unresolved.
	/// The generator tests reference a released Uno package in which DependencyObject is still an interface, so
	/// implementing it counts too.
	/// </summary>
	private static bool IsDependencyObject(ITypeSymbol type)
	{
		for (var current = type; current is not null; current = current.BaseType)
		{
			if (IsXamlType(current, "DependencyObject") || IsUnresolved(current))
			{
				return true;
			}
		}

		return type.AllInterfaces.Any(i => IsXamlType(i, "DependencyObject"));
	}

	/// <summary>
	/// Whether the type is or contains a type the compiler couldn't resolve. The compiler already reports those, and
	/// checks involving them can't be answered, so they pass: the WinAppSDK sync tool compiles without the Generated
	/// folders and relies on the identifiers being declared anyway.
	/// </summary>
	private static bool IsUnresolved(ITypeSymbol? type)
		=> type switch
		{
			null => false,
			{ TypeKind: TypeKind.Error } => true,
			IArrayTypeSymbol array => IsUnresolved(array.ElementType),
			IPointerTypeSymbol pointer => IsUnresolved(pointer.PointedAtType),
			INamedTypeSymbol named => named.TypeArguments.Any(IsUnresolved) || IsUnresolved(named.ContainingType),
			_ => false,
		};

	private static bool HasUnresolvedSignature(IMethodSymbol method)
		=> IsUnresolved(method.ReturnType) || method.Parameters.Any(p => IsUnresolved(p.Type));

	private static bool IsSameTypeOrUnresolved(ITypeSymbol type, ITypeSymbol other)
		=> SymbolEqualityComparer.Default.Equals(type, other) || IsUnresolved(type) || IsUnresolved(other);

	private bool IsImplicitlyConvertible(ITypeSymbol source, ITypeSymbol destination)
		=> IsUnresolved(source) || IsUnresolved(destination) || _compilation.HasImplicitConversion(source, destination);

	private static bool IsXamlType(ITypeSymbol? type, string name)
		=> type is INamedTypeSymbol { ContainingType: null } named && named.Name == name && IsInNamespace(named, XamlNamespace);

	private static bool IsInNamespace(ISymbol symbol, string @namespace)
	{
		var current = symbol.ContainingNamespace;
		var end = @namespace.Length;

		while (current is { IsGlobalNamespace: false })
		{
			var start = @namespace.LastIndexOf('.', end - 1) + 1;
			if (end - start != current.Name.Length || string.CompareOrdinal(@namespace, start, current.Name, 0, current.Name.Length) != 0)
			{
				return false;
			}

			current = current.ContainingNamespace;
			end = start - 1;

			if (end < 0)
			{
				return current is null || current.IsGlobalNamespace;
			}
		}

		return false;
	}

	private static string EscapeIdentifier(string identifier) => HierarchyInfo.EscapeIdentifier(identifier);
}
