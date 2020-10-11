using Microsoft.CodeAnalysis;
using Uno.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Roslyn symbol extensions
    /// </summary>
    internal static class SymbolExtensions
	{
		public static IEnumerable<IPropertySymbol> GetProperties(this INamedTypeSymbol symbol) => symbol.GetMembers().OfType<IPropertySymbol>();

		public static IEnumerable<IEventSymbol> GetAllEvents(this INamedTypeSymbol symbol)
		{
			do
			{
				foreach (var member in GetEvents(symbol))
				{
					yield return member;
				}

				symbol = symbol.BaseType;

				if (symbol == null)
				{
					break;
				}

			} while (symbol.SpecialType != SpecialType.System_Object);
		}

		public static IEnumerable<ISymbol> GetAllMembers(this ITypeSymbol symbol)
		{
			do
			{
				foreach (var member in symbol.GetMembers())
				{
					yield return member;
				}

				symbol = symbol.BaseType;

				if (symbol == null)
				{
					break;
				}

			} while (symbol.SpecialType != SpecialType.System_Object);
		}

		public static IEnumerable<ISymbol> GetAllMembersWithName(this ITypeSymbol symbol, string name)
		{
			do
			{
				foreach (var member in symbol.GetMembers(name))
				{
					yield return member;
				}

				symbol = symbol.BaseType;

				if (symbol == null)
				{
					break;
				}

			} while (symbol.SpecialType != SpecialType.System_Object);
		}

		public static IEnumerable<IEventSymbol> GetEvents(INamedTypeSymbol symbol) => symbol.GetMembers().OfType<IEventSymbol>();

		/// <summary>
		/// Determines if the symbol inherits from the specified type.
		/// </summary>
		/// <param name="symbol">The current symbol</param>
		/// <param name="typeName">A potential base class.</param>
		public static bool Is(this INamedTypeSymbol symbol, string typeName)
		{
			do
			{
				if (symbol.ToDisplayString() == typeName)
				{
					return true;
				}

				symbol = symbol.BaseType;

				if (symbol == null)
				{
					break;
				}

			} while (symbol.SpecialType != SpecialType.System_Object);

			return false;
		}

		/// <summary>
		/// Determines if the symbol inherits from the specified type.
		/// </summary>
		/// <param name="symbol">The current symbol</param>
		/// <param name="other">A potential base class.</param>
		public static bool Is(this INamedTypeSymbol symbol, INamedTypeSymbol other)
		{
			do
			{
				if (Equals(symbol, other))
				{
					return true;
				}

				symbol = symbol.BaseType;

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
				symbol.Locations.Any(l => Equals(l.MetadataModule, currentSymbol))
				&& symbol.DeclaredAccessibility == Accessibility.Internal
			);

		public static IEnumerable<IMethodSymbol> GetMethods(this INamedTypeSymbol resolvedType)
			=> resolvedType.GetMembers().OfType<IMethodSymbol>();

		public static IEnumerable<IMethodSymbol> GetMethodsWithName(this INamedTypeSymbol resolvedType, string name)
			=> resolvedType.GetMembers(name).OfType<IMethodSymbol>();

		public static IEnumerable<IFieldSymbol> GetFields(this INamedTypeSymbol resolvedType)
			=> resolvedType.GetMembers().OfType<IFieldSymbol>();

		public static IEnumerable<IFieldSymbol> GetFieldsWithName(this INamedTypeSymbol resolvedType, string name)
			=> resolvedType.GetMembers(name).OfType<IFieldSymbol>();

		/// <summary>
		/// Return fields of the current type and all of its ancestors
		/// </summary>
		/// <param name="symbol"></param>
		/// <returns></returns>
		public static IEnumerable<IFieldSymbol> GetAllFields(this INamedTypeSymbol symbol)
		{
			while (symbol != null)
			{
				foreach (var property in symbol.GetMembers().OfType<IFieldSymbol>())
				{
					yield return property;
				}

				symbol = symbol.BaseType;
			}
		}

		/// <summary>
		/// Return fields of the current type and all of its ancestors
		/// </summary>
		/// <param name="symbol"></param>
		/// <returns></returns>
		public static IEnumerable<IFieldSymbol> GetAllFieldsWithName(this INamedTypeSymbol symbol, string name)
		{
			while (symbol != null)
			{
				foreach (var property in symbol.GetMembers(name).OfType<IFieldSymbol>())
				{
					yield return property;
				}

				symbol = symbol.BaseType;
			}
		}


		public static IEnumerable<IFieldSymbol> GetFieldsWithAttribute(this ITypeSymbol resolvedType, string name)
		{
			return resolvedType
				.GetMembers()
				.OfType<IFieldSymbol>()
				.Where(f => f.FindAttribute(name) != null);
		}

		public static AttributeData FindAttribute(this ISymbol property, string attributeClassFullName)
		{
			return property.GetAttributes().FirstOrDefault(a => a.AttributeClass.ToDisplayString() == attributeClassFullName);
		}

		public static AttributeData FindAttribute(this ISymbol property, INamedTypeSymbol attributeClassSymbol)
		{
			return property.GetAttributes().FirstOrDefault(a => Equals(a.AttributeClass, attributeClassSymbol));
		}

		public static AttributeData FindAttributeFlattened(this ISymbol property, INamedTypeSymbol attributeClassSymbol)
		{
			return property.GetAllAttributes().FirstOrDefault(a => Equals(a.AttributeClass, attributeClassSymbol));
		}

		/// <summary>
		/// Returns the element type of the IEnumerable, if any.
		/// </summary>
		/// <param name="resolvedType"></param>
		/// <returns></returns>
		public static ITypeSymbol EnumerableOf(this ITypeSymbol resolvedType)
		{
			var intf = resolvedType
				.GetAllInterfaces(includeCurrent: true)
				.FirstOrDefault(i => i.ToDisplayString().StartsWith("System.Collections.Generic.IEnumerable", StringComparison.OrdinalIgnoreCase));

			return intf?.TypeArguments.First();
		}

		public static IEnumerable<INamedTypeSymbol> GetAllInterfaces(this ITypeSymbol symbol, bool includeCurrent = true)
		{
			if (symbol != null)
			{
				if (includeCurrent && symbol.TypeKind == TypeKind.Interface)
				{
					yield return (INamedTypeSymbol)symbol;
				}

				do
				{
					foreach (var intf in symbol.Interfaces)
					{
						yield return intf;

						foreach (var innerInterface in intf.GetAllInterfaces())
						{
							yield return innerInterface;
						}
					}

					symbol = symbol.BaseType;

					if (symbol == null)
					{
						break;
					}

				} while (symbol.SpecialType != SpecialType.System_Object);
			}
		}

		public static bool IsNullable(this ITypeSymbol type)
		{
			return ((type as INamedTypeSymbol)?.IsGenericType ?? false)
				&& type.OriginalDefinition.ToDisplayString().Equals("System.Nullable<T>", StringComparison.OrdinalIgnoreCase);
		}

		public static bool IsNullable(this ITypeSymbol type, out ITypeSymbol nullableType)
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

		public static ITypeSymbol NullableOf(this ITypeSymbol type)
		{
			return type.IsNullable()
				? ((INamedTypeSymbol)type).TypeArguments.First()
				: null;
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
		private static readonly Dictionary<string, string> _fullNamesMaping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
		{
			{"string",     typeof(string).ToString()},
			{"long",       typeof(long).ToString()},
			{"int",        typeof(int).ToString()},
			{"short",      typeof(short).ToString()},
			{"ulong",      typeof(ulong).ToString()},
			{"uint",       typeof(uint).ToString()},
			{"ushort",     typeof(ushort).ToString()},
			{"byte",       typeof(byte).ToString()},
			{"double",     typeof(double).ToString()},
			{"float",      typeof(float).ToString()},
			{"decimal",    typeof(decimal).ToString()},
			{"bool",       typeof(bool).ToString()},
		};

		public static string GetFullName(this INamespaceOrTypeSymbol type)
		{
			IArrayTypeSymbol arrayType = type as IArrayTypeSymbol;
			if (arrayType != null)
			{
				return $"{arrayType.ElementType.GetFullName()}[]";
			}

			ITypeSymbol t;
			if ((type as ITypeSymbol).IsNullable(out t))
			{
				return $"System.Nullable`1[{t.GetFullName()}]";
			}

			var name = type.ToDisplayString();

			return _fullNamesMaping.UnoGetValueOrDefault(name, name);
		}

		public static string GetFullMetadataName(this INamespaceOrTypeSymbol symbol)
		{
			ISymbol s = symbol;
			var sb = new StringBuilder(s.MetadataName);

			var last = s;
			s = s.ContainingSymbol;

			if (s == null)
			{
				return symbol.GetFullName();
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
				sb.Append($"[{ genericArgs }]");
			}

			return sb.ToString();
		}

		private static bool IsRootNamespace(ISymbol s)
		{
			return s is INamespaceSymbol && ((INamespaceSymbol)s).IsGlobalNamespace;
		}
		/// <summary>
		/// Return attributes on the current type and all its ancestors
		/// </summary>
		/// <param name="symbol"></param>
		/// <returns></returns>
		public static IEnumerable<AttributeData> GetAllAttributes(this ISymbol symbol)
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
		public static IEnumerable<IPropertySymbol> GetAllProperties(this INamedTypeSymbol symbol)
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
		public static IEnumerable<IPropertySymbol> GetAllPropertiesWithName(this INamedTypeSymbol symbol, string name)
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

		/// <summary>
		/// Converts declared accessibility on a symbol to a string usable in generated code.
		/// </summary>
		/// <param name="symbol">The symbol to get an accessibility string for.</param>
		/// <returns>Accessibility in format "public", "protected internal", etc.</returns>
		public static string GetAccessibilityAsCodeString(this ISymbol symbol)
		{
			switch (symbol.DeclaredAccessibility)
			{
				case Accessibility.Private:
					return "private";
				case Accessibility.ProtectedOrInternal:
					return "protected internal";
				case Accessibility.Protected:
					return "protected";
				case Accessibility.Internal:
					return "internal";
				case Accessibility.Public:
					return "public";
			}

			throw new ArgumentOutOfRangeException($"{symbol.DeclaredAccessibility} is not supported.");
		}

		public static IFieldSymbol FindField(this INamedTypeSymbol symbol, INamedTypeSymbol fieldType, string fieldName, StringComparison comparison = default)
		{
			return symbol.GetFields().FirstOrDefault(x => Equals(x.Type, fieldType) && x.Name.Equals(fieldName, comparison));
		}
	}
}
