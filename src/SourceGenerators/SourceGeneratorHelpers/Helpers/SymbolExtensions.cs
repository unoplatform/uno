#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Uno.Extensions;
using Uno;
using Uno.Roslyn;
using Microsoft.CodeAnalysis.PooledObjects;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis
{
	/// <summary>
	/// Roslyn symbol extensions
	/// </summary>
	internal static class SymbolExtensions
	{
		// This is the same as MinimallyQualifiedFormat, but adds the SymbolDisplayGenericsOptions.IncludeVariance.
		private static SymbolDisplayFormat s_format = new SymbolDisplayFormat(
			globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
			genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters | SymbolDisplayGenericsOptions.IncludeVariance,
			memberOptions:
				SymbolDisplayMemberOptions.IncludeParameters |
				SymbolDisplayMemberOptions.IncludeType |
				SymbolDisplayMemberOptions.IncludeRef |
				SymbolDisplayMemberOptions.IncludeContainingType,
			kindOptions:
				SymbolDisplayKindOptions.IncludeMemberKeyword,
			parameterOptions:
				SymbolDisplayParameterOptions.IncludeName |
				SymbolDisplayParameterOptions.IncludeType |
				SymbolDisplayParameterOptions.IncludeParamsRefOut |
				SymbolDisplayParameterOptions.IncludeDefaultValue,
			localOptions: SymbolDisplayLocalOptions.IncludeType,
			miscellaneousOptions:
				SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
				SymbolDisplayMiscellaneousOptions.UseSpecialTypes |
				SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

		/// <summary>
		/// Given an <see cref="INamedTypeSymbol"/>, add the symbol declaration (including parent classes/namespaces) to the given <see cref="IIndentedStringBuilder"/>.
		/// </summary>
		/// <remarks>
		/// <para>Example usage:</para>
		/// <code><![CDATA[
		/// using (myClass.AddToIndentedStringBuilder(builder))
		/// {
		///		using (builder.BlockInvariant("public static void M()"))
		///		{
		///		  builder.AppendLineInvariant("Console.WriteLine(\"Hello world\")");
		///		}
		/// }
		/// ]]></code>
		/// <para>NOTE: Another possible implementation is to accept an <see cref="Action"/> as a parameter to generate the type members, execute the action here, and also dispose
		/// the stack here. The advantage is that callers don't need to worry about disposing the stack.</para>
		/// </remarks>
		public static IDisposable AddToIndentedStringBuilder(
			this INamedTypeSymbol namedTypeSymbol,
			IIndentedStringBuilder builder,
			Action<IIndentedStringBuilder>? beforeClassHeaderAction = null,
			string afterClassHeader = "")
		{
			var stack = new Stack<string>();
			ISymbol symbol = namedTypeSymbol;
			while (symbol != null)
			{
				if (symbol is INamespaceSymbol namespaceSymbol)
				{
					if (!namespaceSymbol.IsGlobalNamespace)
					{
						stack.Push($"namespace {namespaceSymbol}");
					}

					break;
				}
				else if (symbol is INamedTypeSymbol namedSymbol)
				{
					// When generating the class header for the originally given namedTypeSymbol, invoke afterClassHeaderAction.
					// This is usually used to append the base types or interfaces.
					stack.Push(GetDeclarationHeaderFromNamedTypeSymbol(namedSymbol, ReferenceEquals(namedSymbol, namedTypeSymbol) ? afterClassHeader : null));
				}
				else
				{
					throw new InvalidOperationException($"Unexpected symbol type {symbol}");
				}

				symbol = symbol.ContainingSymbol;
			}

			var outputDisposableStack = new Stack<IDisposable>();
			while (stack.Count > 0)
			{
				if (stack.Count == 1)
				{
					// Only the original symbol is left (usually a class header). Execute the given action before adding the class (usually this adds attributes).
					beforeClassHeaderAction?.Invoke(builder);
				}

				outputDisposableStack.Push(builder.BlockInvariant(stack.Pop()));
			}

			return new DisposableAction(() => outputDisposableStack.ForEach(d => d.Dispose()));
		}

		public static string GetDeclarationHeaderFromNamedTypeSymbol(this INamedTypeSymbol namedTypeSymbol, string? afterClassHeader)
		{
			// Interfaces are implicitly abstract, but they can't explicitly have the abstract modifier.
			var abstractKeyword = namedTypeSymbol.IsAbstract && !namedTypeSymbol.IsAbstract ? "abstract " : string.Empty;
			var staticKeyword = namedTypeSymbol.IsStatic ? "static " : string.Empty;

			// records are not handled.
			var typeKeyword = namedTypeSymbol.TypeKind switch
			{
				TypeKind.Class => "class ",
				TypeKind.Interface => "interface ",
				TypeKind.Struct => "struct ",
				_ => throw new ArgumentException($"Unexpected type kind {namedTypeSymbol.TypeKind}")
			};

			var declarationIdentifier = namedTypeSymbol.ToDisplayString(s_format);

			return $"{abstractKeyword}{staticKeyword}partial {typeKeyword}{declarationIdentifier}{afterClassHeader}";
		}

		public static IEnumerable<IPropertySymbol> GetProperties(this INamedTypeSymbol symbol) => symbol.GetMembers().OfType<IPropertySymbol>();

		public static IEnumerable<IPropertySymbol> GetPropertiesWithName(this INamedTypeSymbol symbol, string name) => symbol.GetMembers(name).OfType<IPropertySymbol>();

		public static IEnumerable<ISymbol> GetAllMembersWithName(this ITypeSymbol? symbol, string name)
		{
			do
			{
				if (symbol != null)
				{
					foreach (var member in symbol.GetMembers(name))
					{
						yield return member;
					}
				}

				symbol = symbol?.BaseType;

				if (symbol == null)
				{
					break;
				}

			} while (symbol.SpecialType != SpecialType.System_Object);
		}

		/// <summary>
		/// Determines if the symbol inherits from the specified type.
		/// </summary>
		/// <param name="symbol">The current symbol</param>
		/// <param name="other">A potential base class.</param>
		public static bool Is(this INamedTypeSymbol? symbol, INamedTypeSymbol? other)
		{
			do
			{
				if (SymbolEqualityComparer.Default.Equals(symbol, other))
				{
					return true;
				}

				symbol = symbol?.BaseType;

				if (symbol == null)
				{
					break;
				}

			} while (symbol.SpecialType != SpecialType.System_Object);

			return false;
		}

		/// <summary>
		/// Returns true if the symbol can be accessed from the current module
		/// </summary>
		/// <param name="symbol"></param>
		/// <returns></returns>
		public static bool IsLocallyPublic(this ISymbol symbol, IModuleSymbol currentSymbol) =>
			symbol.DeclaredAccessibility == Accessibility.Public
			||
			(
				symbol.Locations.Any(l => SymbolEqualityComparer.Default.Equals(l.MetadataModule, currentSymbol))
				&& symbol.DeclaredAccessibility == Accessibility.Internal
			);

		public static IEnumerable<IMethodSymbol> GetMethodsWithName(this ITypeSymbol resolvedType, string name)
			=> resolvedType.GetMembers(name).OfType<IMethodSymbol>();

		// includeBaseTypes was added to fix a bug for a specific caller without affecting anything else.
		// But, we should revise it and make sure whether other callers need it or not, and potentially remove it completely and default to true.
		public static IMethodSymbol? GetFirstMethodWithName(this ITypeSymbol resolvedType, string name, bool includeBaseTypes = false)
		{
			var baseType = resolvedType;
			while (baseType is not null)
			{
				var members = baseType.GetMembers(name);

				for (int i = 0; i < members.Length; i++)
				{
					if (members[i] is IMethodSymbol method)
					{
						return method;
					}
				}

				if (!includeBaseTypes)
				{
					return null;
				}

				baseType = baseType.BaseType;
			}


			return null;
		}

		public static IEnumerable<IFieldSymbol> GetFields(this ITypeSymbol resolvedType)
			=> resolvedType.GetMembers().OfType<IFieldSymbol>();

		public static AttributeData? FindAttribute(this ISymbol? property, INamedTypeSymbol? attributeClassSymbol)
		{
			return property?.GetAttributes().FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, attributeClassSymbol));
		}

		public static AttributeData? FindAttributeFlattened(this ISymbol? property, INamedTypeSymbol? attributeClassSymbol)
		{
			return property?.GetAllAttributes().FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, attributeClassSymbol));
		}

		public static ITypeSymbol? TryGetPropertyOrFieldType(this ITypeSymbol type, string propertyOrFieldName)
		{
			if (type.GetAllMembersWithName(propertyOrFieldName).FirstOrDefault() is ISymbol member)
			{
				return member switch
				{
					IPropertySymbol propertySymbol => propertySymbol.Type,
					IFieldSymbol fieldSymbol => fieldSymbol.Type,
					_ => null,
				};
			}

			return null;
		}

		public static IEnumerable<INamedTypeSymbol> GetAllInterfaces(this ITypeSymbol? symbol)
		{
			if (symbol != null)
			{
				if (symbol.TypeKind == TypeKind.Interface)
				{
					yield return (INamedTypeSymbol)symbol;
				}

				foreach (var @interface in symbol.AllInterfaces)
				{
					yield return @interface;
				}
			}
		}

		public static ISymbol? GetMemberInlcudingBaseTypes(this INamespaceOrTypeSymbol symbol, string memberName)
		{
			if (symbol is INamespaceSymbol)
			{
				return symbol.GetMembers(memberName).FirstOrDefault();
			}

			var typeSymbol = (ITypeSymbol?)symbol;
			while (typeSymbol is not null)
			{
				if (typeSymbol.GetMembers(memberName).FirstOrDefault() is { } member)
				{
					return member;
				}

				typeSymbol = typeSymbol.BaseType;
			}

			return null;
		}

		public static ISymbol? GetMemberInlcudingBaseTypes<TArg>(this INamespaceOrTypeSymbol symbol, TArg arg, Func<ISymbol, TArg, bool> predicate)
		{
			if (symbol is INamespaceSymbol)
			{
				foreach (var candicate in symbol.GetMembers())
				{
					if (predicate(candicate, arg))
					{
						return candicate;
					}
				}

				return null;
			}

			var typeSymbol = (ITypeSymbol?)symbol;
			while (typeSymbol is not null)
			{
				foreach (var candidate in typeSymbol.GetMembers())
				{
					if (predicate(candidate, arg))
					{
						return candidate;
					}
				}

				typeSymbol = typeSymbol.BaseType;
			}

			return null;
		}

		public static bool IsNullable(this ITypeSymbol type)
		{
			return ((type as INamedTypeSymbol)?.IsGenericType ?? false)
				&& type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T;
		}

		public static bool IsNullable(this ITypeSymbol type, out ITypeSymbol? nullableType)
		{
			if (type.IsNullable())
			{
				nullableType = ((INamedTypeSymbol)type).TypeArguments.First();
				return true;
			}
			else
			{
				nullableType = null;
				return false;
			}
		}

		public static IEnumerable<INamedTypeSymbol> GetNamespaceTypes(this INamespaceSymbol sym)
		{
			foreach (var child in sym.GetTypeMembers())
			{
				yield return child;
			}

			foreach (var ns in sym.GetNamespaceMembers())
			{
				foreach (var child2 in GetNamespaceTypes(ns))
				{
					yield return child2;
				}
			}
		}

		// https://github.com/CommunityToolkit/dotnet/blob/e6257d8c65126f2f977f2dcbce3fe6045086f270/CommunityToolkit.Mvvm.SourceGenerators/Extensions/INamedTypeSymbolExtensions.cs#L16-L44
		/// <summary>
		/// Gets a valid filename for a given <see cref="INamedTypeSymbol"/> instance.
		/// </summary>
		/// <param name="symbol">The input <see cref="INamedTypeSymbol"/> instance.</param>
		/// <returns>The full metadata name for <paramref name="symbol"/> that is also a valid filename.</returns>
		public static string GetFullMetadataNameForFileName(this INamedTypeSymbol symbol)
		{
			static StringBuilder BuildFrom(ISymbol? symbol, StringBuilder builder)
			{
				return symbol switch
				{
					INamespaceSymbol ns when ns.IsGlobalNamespace => builder,
					INamespaceSymbol ns when ns.ContainingNamespace is { IsGlobalNamespace: false }
						=> BuildFrom(ns.ContainingNamespace, builder.Insert(0, $".{ns.MetadataName}")),
					ITypeSymbol ts when ts.ContainingType is ISymbol pt
						=> BuildFrom(pt, builder.Insert(0, $"+{ts.MetadataName}")),
					ITypeSymbol ts when ts.ContainingNamespace is ISymbol pn and not INamespaceSymbol { IsGlobalNamespace: true }
						=> BuildFrom(pn, builder.Insert(0, $".{ts.MetadataName}")),
					ISymbol => BuildFrom(symbol.ContainingSymbol, builder.Insert(0, symbol.MetadataName)),
					_ => builder
				};
			}
			// Build the full metadata name by concatenating the metadata names of all symbols from the input
			// one to the outermost namespace, if any. Additionally, the ` and + symbols need to be replaced
			// to avoid errors when generating code. This is a known issue with source generators not accepting
			// those characters at the moment, see: https://github.com/dotnet/roslyn/issues/58476.
			return BuildFrom(symbol, new StringBuilder(256)).Replace('`', '-').Replace('+', '.').ToString();
		}

		private static string? GetFullName(this ITypeSymbol? type)
		{
			if (type is IArrayTypeSymbol arrayType)
			{
				return $"{arrayType.ElementType.GetFullName()}[]";
			}

			if (type is ITypeSymbol ts && ts.IsNullable(out var t))
			{
				return $"System.Nullable`1[{t.GetFullName()}]";
			}

			return type?.GetFullyQualifiedTypeExcludingGlobal();
		}

		// forRegisterAttributeDotReplacement is used specifically by NativeCtorsGenerator to generate for the Android/iOS RegisterAttribute
		// A non-null value means we are generating for RegisterAttribute, and we replace invalid characters with '_'.
		// The '.' is special cased to be replaced by the value of forRegisterAttributeDotReplacement, whether it's '_' or '/'
		public static string GetFullMetadataName(this ITypeSymbol symbol, char? forRegisterAttributeDotReplacement = null)
		{
			ISymbol s = symbol;
			var sb = new StringBuilder(s.MetadataName);

			var last = s;
			s = s.ContainingSymbol;

			if (s == null)
			{
				return symbol.GetFullName()!;
			}

			while (!IsRootNamespace(s))
			{
				if (s is ITypeSymbol && last is ITypeSymbol)
				{
					sb.Insert(0, '+');
				}
				else
				{
					sb.Insert(0, '.');
				}
				sb.Insert(0, s.MetadataName);

				s = s.ContainingSymbol;
			}

			var namedType = symbol as INamedTypeSymbol;

			// When generating for RegisterAttribute, the name we pass to the attribute is used by Xamarin tooling for generating Java files, specifically, it's used for the class name.
			// The characters '.', '+', and '`' are not valid characters for a class name.
			// On Android, we use '/' as replacement for '.' to match Jni name:
			// https://github.com/xamarin/java.interop/blob/38c8a827e78ffe9c80ad2313a9e0e0d4f8215184/src/Java.Interop.Tools.TypeNameMappings/Java.Interop.Tools.TypeNameMappings/JavaNativeTypeManager.cs#L693-L699
			if (forRegisterAttributeDotReplacement.HasValue)
			{
				var replacement = forRegisterAttributeDotReplacement.Value;
				sb.Replace('.', replacement).Replace('+', '_').Replace('`', '_');
			}
			else if (namedType?.TypeArguments.Any() ?? false)
			{
				// We don't append type arguments when generating for RegisterAttribute because '[' and ']' are invalid characters for a class name.
				var genericArgs = namedType.TypeArguments.Select(a => GetFullMetadataName(a, null)).JoinBy(",");
				sb.Append($"[{genericArgs}]");
			}

			return sb.ToString();
		}

		private static bool IsRootNamespace(ISymbol s)
		{
			return s is INamespaceSymbol { IsGlobalNamespace: true };
		}

		/// <summary>
		/// Return attributes on the current type and all its ancestors
		/// </summary>
		/// <param name="symbol"></param>
		/// <returns></returns>
		public static IEnumerable<AttributeData> GetAllAttributes(this ISymbol? symbol)
		{
			while (symbol != null)
			{
				foreach (var attribute in symbol.GetAttributes())
				{
					yield return attribute;
				}

				symbol = (symbol as INamedTypeSymbol)?.BaseType;
			}
		}

		/// <summary>
		/// Return properties of the current type and all of its ancestors
		/// </summary>
		/// <param name="symbol"></param>
		/// <returns></returns>
		public static IEnumerable<IPropertySymbol> GetAllProperties(this INamedTypeSymbol? symbol)
		{
			while (symbol != null)
			{
				foreach (var property in symbol.GetMembers().OfType<IPropertySymbol>())
				{
					yield return property;
				}

				symbol = symbol.BaseType;
			}
		}

		/// <summary>
		/// Return property with specific name of the current type and all of its ancestors.
		/// </summary>
		/// <param name="symbol">The type to look property in (lookup includes base types)</param>
		/// <param name="name">The property name to lookup.</param>
		/// <returns>The found property symbol or not if not found.</returns>
		public static IPropertySymbol? GetPropertyWithName(this INamedTypeSymbol? symbol, string name)
		{
			while (symbol != null)
			{
				foreach (var property in symbol.GetMembers(name))
				{
					if (property.Kind == SymbolKind.Property)
					{
						return (IPropertySymbol)property;
					}
				}

				symbol = symbol.BaseType;
			}

			return null;
		}

		/// <summary>
		/// Return field with specific name of the current type and all of its ancestors.
		/// </summary>
		/// <param name="symbol">The type to look field in (lookup includes base types)</param>
		/// <param name="name">The field name to lookup.</param>
		/// <returns>The found field symbol or not if not found.</returns>
		public static IFieldSymbol? GetFieldWithName(this INamedTypeSymbol? symbol, string name)
		{
			while (symbol != null)
			{
				foreach (var field in symbol.GetMembers(name))
				{
					if (field.Kind == SymbolKind.Field)
					{
						return (IFieldSymbol)field;
					}
				}

				symbol = symbol.BaseType;
			}

			return null;
		}

		/// <summary>
		/// Builds a fully qualified type string, including generic types and global:: prefix.
		/// </summary>
		public static string GetFullyQualifiedTypeIncludingGlobal(this ITypeSymbol type)
		{
			return type.GetFullyQualifiedType(includeGlobalNamespace: true);
		}

		/// <summary>
		/// Builds a fully qualified type string, including generic types and global:: prefix.
		/// </summary>
		public static string GetFullyQualifiedTypeExcludingGlobal(this ITypeSymbol type)
		{
			return type.GetFullyQualifiedType(includeGlobalNamespace: false);
		}

		private static string GetFullyQualifiedType(this ITypeSymbol type, bool includeGlobalNamespace)
		{
			var pool = PooledStringBuilder.GetInstance();
			var visitor = new UnoNamedTypeSymbolDisplayVisitor(pool.Builder, includeGlobalNamespace);
			type.Accept(visitor);
			return pool.ToStringAndFree();
		}

		public static TypedConstant? FindNamedArg(this AttributeData attribute, string argName)
			=> attribute.NamedArguments is { IsDefaultOrEmpty: false } args
				&& args.FirstOrDefault(arg => arg.Key == argName) is { Key: not null } arg
					? arg.Value
					: null;

		public static T? GetNamedValue<T>(this AttributeData attribute, string argName)
			where T : Enum
			=> attribute.FindNamedArg(argName) is { IsNull: false, Kind: TypedConstantKind.Enum } arg && arg.Type!.Name == typeof(T).Name
				? (T)arg.Value!
				: default(T?);

		/// <summary>
		/// Returns the property type of a dependency-property or an attached dependency-property setter.
		/// </summary>
		/// <param name="propertyOrSetter">The dependency-property or the attached dependency-property setter</param>
		/// <returns>The property type</returns>
		public static INamedTypeSymbol? FindDependencyPropertyType(this ISymbol propertyOrSetter)
		{
			if (propertyOrSetter is IPropertySymbol dependencyProperty)
			{
				return dependencyProperty.Type.OriginalDefinition is { SpecialType: SpecialType.System_Nullable_T }
					? (dependencyProperty.Type as INamedTypeSymbol)?.TypeArguments[0] as INamedTypeSymbol
					: dependencyProperty.Type as INamedTypeSymbol;
			}
			else if (propertyOrSetter is IMethodSymbol { IsStatic: true, Parameters.Length: 2 } attachedPropertySetter)
			{
				return attachedPropertySetter.Parameters[1].Type as INamedTypeSymbol;
			}

			return null;
		}
	}
}
