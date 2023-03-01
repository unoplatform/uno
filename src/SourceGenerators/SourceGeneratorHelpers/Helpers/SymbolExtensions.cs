#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Uno.Extensions;
using Uno;
using Uno.Roslyn;
using Microsoft.CodeAnalysis.PooledObjects;

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

		public static IEnumerable<IEventSymbol> GetAllEvents(this INamedTypeSymbol? symbol)
		{
			do
			{
				foreach (var member in GetEvents(symbol))
				{
					yield return member;
				}

				symbol = symbol?.BaseType;

				if (symbol == null)
				{
					break;
				}

			} while (symbol.SpecialType != SpecialType.System_Object);
		}

		public static IEnumerable<ISymbol> GetAllMembers(this ITypeSymbol? symbol)
		{
			do
			{
				if (symbol != null)
				{
					foreach (var member in symbol.GetMembers())
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

		public static IEnumerable<IEventSymbol> GetEvents(INamedTypeSymbol? symbol)
			=> symbol?.GetMembers().OfType<IEventSymbol>() ?? Enumerable.Empty<IEventSymbol>();

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

		public static bool IsPublic(this ISymbol symbol) => symbol.DeclaredAccessibility == Accessibility.Public;

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

		public static IEnumerable<IMethodSymbol> GetMethods(this ITypeSymbol resolvedType)
			=> resolvedType.GetMembers().OfType<IMethodSymbol>();

		public static IEnumerable<IMethodSymbol> GetMethodsWithName(this ITypeSymbol resolvedType, string name)
			=> resolvedType.GetMembers(name).OfType<IMethodSymbol>();

		public static IMethodSymbol? GetFirstMethodWithName(this ITypeSymbol resolvedType, string name)
		{
			var members = resolvedType.GetMembers(name);

			for (int i = 0; i < members.Length; i++)
			{
				if (members[i] is IMethodSymbol method)
				{
					return method;
				}
			}

			return null;
		}

		public static IEnumerable<IFieldSymbol> GetFields(this ITypeSymbol resolvedType)
			=> resolvedType.GetMembers().OfType<IFieldSymbol>();

		public static IEnumerable<IFieldSymbol> GetFieldsWithName(this ITypeSymbol resolvedType, string name)
			=> resolvedType.GetMembers(name).OfType<IFieldSymbol>();

		/// <summary>
		/// Return fields of the current type and all of its ancestors
		/// </summary>
		/// <param name="symbol"></param>
		/// <returns></returns>
		public static IEnumerable<IFieldSymbol> GetAllFields(this INamedTypeSymbol? symbol)
		{
			while (symbol != null)
			{
				foreach (var property in symbol.GetMembers().OfType<IFieldSymbol>())
				{
					yield return property;
				}

				symbol = symbol?.BaseType;
			}
		}

		/// <summary>
		/// Return fields of the current type and all of its ancestors
		/// </summary>
		/// <param name="symbol"></param>
		/// <returns></returns>
		public static IEnumerable<IFieldSymbol> GetAllFieldsWithName(this INamedTypeSymbol? symbol, string name)
		{
			while (symbol != null)
			{
				foreach (var property in symbol.GetMembers(name).OfType<IFieldSymbol>())
				{
					yield return property;
				}

				symbol = symbol?.BaseType;
			}
		}

		public static AttributeData? FindAttribute(this ISymbol? property, string attributeClassFullName)
		{
			return property?.GetAttributes().FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == attributeClassFullName);
		}

		public static AttributeData? FindAttribute(this ISymbol? property, INamedTypeSymbol? attributeClassSymbol)
		{
			return property?.GetAttributes().FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, attributeClassSymbol));
		}

		public static AttributeData? FindAttributeFlattened(this ISymbol? property, INamedTypeSymbol? attributeClassSymbol)
		{
			return property?.GetAllAttributes().FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, attributeClassSymbol));
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

		public static string GetFullMetadataName(this ITypeSymbol symbol)
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

			if (namedType?.TypeArguments.Any() ?? false)
			{
				var genericArgs = namedType.TypeArguments.Select(GetFullMetadataName).JoinBy(",");
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
		/// Return properties of the current type and all of its ancestors
		/// </summary>
		/// <param name="symbol"></param>
		/// <returns></returns>
		public static IEnumerable<IPropertySymbol> GetAllPropertiesWithName(this INamedTypeSymbol? symbol, string name)
		{
			while (symbol != null)
			{
				foreach (var property in symbol.GetMembers(name).OfType<IPropertySymbol>())
				{
					yield return property;
				}

				symbol = symbol.BaseType;
			}
		}

		public static IFieldSymbol? FindField(this INamedTypeSymbol symbol, INamedTypeSymbol fieldType, string fieldName, StringComparison comparison = default)
		{
			return symbol.GetFields().FirstOrDefault(x => SymbolEqualityComparer.Default.Equals(x.Type, fieldType) && x.Name.Equals(fieldName, comparison));
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
	}
}
