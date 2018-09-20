// ******************************************************************
// Copyright � 2015-2018 nventive inc. All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// ******************************************************************
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;


namespace Uno.SourceGenerators.Helpers
{
	public static class TypeSymbolExtensions
	{
		public static bool IsPrimitive(this ITypeSymbol type)
		{
			switch (type.SpecialType)
			{
				case SpecialType.System_Boolean:
				case SpecialType.System_Byte:
				case SpecialType.System_Char:
				case SpecialType.System_DateTime:
				case SpecialType.System_Decimal:
				case SpecialType.System_Double:
				case SpecialType.System_Enum:
				case SpecialType.System_Int16:
				case SpecialType.System_Int32:
				case SpecialType.System_Int64:
				case SpecialType.System_IntPtr:
				case SpecialType.System_SByte:
				case SpecialType.System_String:
				case SpecialType.System_Single:
				case SpecialType.System_UInt16:
				case SpecialType.System_UInt32:
				case SpecialType.System_UInt64:
				case SpecialType.System_UIntPtr:
					return true;
			}

			return false;
		}

		private static ImmutableDictionary<(ITypeSymbol, bool), bool> _isImmutable =
			ImmutableDictionary<(ITypeSymbol, bool), bool>.Empty;

		public static SymbolNames GetSymbolNames(this ITypeSymbol symbol, INamedTypeSymbol typeToUseForSubstitutions = null)
		{
			return symbol is INamedTypeSymbol namedSymbol
				? namedSymbol.GetSymbolNames(typeToUseForSubstitutions)
				: null;
		}

		public static bool IsImmutable(this ITypeSymbol type, bool treatArrayAsImmutable, IReadOnlyList<ITypeSymbol> knownAsImmutable)
		{
			bool GetIsImmutable((ITypeSymbol type, bool treatArrayAsImmutable) x)
			{
				var (t, asImmutable) = x;
				foreach (var attribute in t.GetAttributes())
				{
					switch (attribute.AttributeClass.ToString())
					{
						case "Uno.ImmutableAttribute":
						case "Uno.GeneratedImmutableAttribute":
							return true;
					}
				}

				if (t.IsPrimitive())
				{
					return true;
				}

				switch (t.SpecialType)
				{
					case SpecialType.None:
					case SpecialType.System_Collections_Generic_IReadOnlyCollection_T:
					case SpecialType.System_Collections_Generic_IReadOnlyList_T:
						break; // need further validation
					default:
						return false;
				}

				if (t is IArrayTypeSymbol arrayType)
				{
					return arrayType.ElementType?.IsImmutable(asImmutable, knownAsImmutable) ?? false;
				}

				var definitionType = t.GetDefinitionType();

				switch (definitionType.ToString())
				{
					case "System.Attribute": // strange, but valid
					case "System.DateTime":
					case "System.DateTimeOffset":
					case "System.Guid":
					case "System.TimeSpan":
					case "System.Type":
					case "System.Uri":
					case "System.Version":
						return true;

					// .NET framework
					case "System.Collections.Generic.IReadOnlyList<T>":
					case "System.Collections.Generic.IReadOnlyCollection<T>":
					case "System.Nullable<T>":
					case "System.Tuple<T>":
					// System.Collections.Immutable (nuget package)
					case "System.Collections.Immutable.IImmutableList<T>":
					case "System.Collections.Immutable.IImmutableQueue<T>":
					case "System.Collections.Immutable.IImmutableSet<T>":
					case "System.Collections.Immutable.IImmutableStack<T>":
					case "System.Collections.Immutable.ImmutableArray<T>":
					case "System.Collections.Immutable.ImmutableHashSet<T>":
					case "System.Collections.Immutable.ImmutableList<T>":
					case "System.Collections.Immutable.ImmutableQueue<T>":
					case "System.Collections.Immutable.ImmutableSortedSet<T>":
					case "System.Collections.Immutable.ImmutableStack<T>":
						{
							var argumentParameter = (t as INamedTypeSymbol)?.TypeArguments.FirstOrDefault();
							return argumentParameter == null || argumentParameter.IsImmutable(asImmutable, knownAsImmutable);
						}
					case "System.Collections.Immutable.IImmutableDictionary<TKey, TValue>":
					case "System.Collections.Immutable.ImmutableDictionary<TKey, TValue>":
					case "System.Collections.Immutable.ImmutableSortedDictionary<TKey, TValue>":
						{
							var keyTypeParameter = (t as INamedTypeSymbol)?.TypeArguments.FirstOrDefault();
							var valueTypeParameter = (t as INamedTypeSymbol)?.TypeArguments.Skip(1).FirstOrDefault();
							return (keyTypeParameter == null || keyTypeParameter.IsImmutable(asImmutable, knownAsImmutable))
								&& (valueTypeParameter == null || valueTypeParameter.IsImmutable(asImmutable, knownAsImmutable));
						}
				}

				if (knownAsImmutable.Contains(definitionType))
				{
					return true;
				}

				switch (definitionType.GetType().Name)
				{
					case "TupleTypeSymbol":
						return true; // tuples are immutables
				}

				switch (definitionType.BaseType?.ToString())
				{
					case "System.Enum":
						return true;
					case "System.Array":
						return asImmutable;
				}

				return IsImmutableByRequirements(t, treatArrayAsImmutable, knownAsImmutable);
			}

			return ImmutableInterlocked.GetOrAdd(ref _isImmutable, (type, treatArrayAsImmutable), GetIsImmutable);
		}

		private static bool IsImmutableByRequirements(ITypeSymbol type, bool treatArrayAsImmutable, IReadOnlyList<ITypeSymbol> knownAsImmutable)
		{
			// Check if type is complying to immutable requirements
			// 1) all instance fields are readonly
			// 2) all properties are getter-only
			// 3) base class is immutable

			foreach (var member in type.GetMembers())
			{
				if (member is IFieldSymbol f && !(f.IsStatic || f.IsReadOnly || f.IsImplicitlyDeclared) && f.Type.IsImmutable(treatArrayAsImmutable, knownAsImmutable))
				{
					return false; // there's a non-readonly non-static field
				}
				if (member is IPropertySymbol p && !(p.IsStatic || p.IsReadOnly || p.IsImplicitlyDeclared) && p.Type.IsImmutable(treatArrayAsImmutable, knownAsImmutable))
				{
					return false; // there's a non-readonly non-static property
				}
			}

			// It's immutable if the base type is (or no base type)
			return type.BaseType == null
				|| type.BaseType.SpecialType == SpecialType.System_Object
				|| type.BaseType.SpecialType == SpecialType.System_ValueType
				|| type.BaseType.IsImmutable(treatArrayAsImmutable, knownAsImmutable);
		}

		public static ITypeSymbol GetDefinitionType(this ITypeSymbol type)
		{
			var definitionType = type;

			while ((definitionType as INamedTypeSymbol)?.ConstructedFrom.Equals(definitionType) == false)
			{
				definitionType = ((INamedTypeSymbol)definitionType).ConstructedFrom;
			}

			return definitionType;
		}

		private static ImmutableDictionary<ITypeSymbol, (ITypeSymbol, bool, bool)> _isCollection =
			ImmutableDictionary<ITypeSymbol, (ITypeSymbol, bool, bool)>.Empty;

		public static bool IsCollection(this ITypeSymbol type, out ITypeSymbol elementType, out bool isReadonlyCollection, out bool isOrdered)
		{
			(ITypeSymbol elementType, bool isReadonlyCollection, bool isOrdered) GetTypeArgumentIfItIsACollection(INamedTypeSymbol t)
			{
				if (t != null)
				{
					var interfaceDefinition = t.GetDefinitionType();

					switch (interfaceDefinition.ToString())
					{
						case "System.Collections.Immutable.ImmutableHashSet<T>":
						case "System.Collections.Immutable.IImmutableSet<T>":
							{
								return (t.TypeArguments[0], true, false);
							}
						case "System.Collections.Immutable.IImmutableList<T>":
						case "System.Collections.Immutable.IImmutableQueue<T>":
						case "System.Collections.Immutable.IImmutableStack<T>":
						case "System.Collections.Immutable.ImmutableArray<T>":
						case "System.Collections.Immutable.ImmutableList<T>":
						case "System.Collections.Immutable.ImmutableQueue<T>":
						case "System.Collections.Immutable.ImmutableSortedSet<T>":
						case "System.Collections.Immutable.ImmutableStack<T>":
							{
								return (t.TypeArguments[0], true, true);
							}
						case "System.Collections.Generic.IReadOnlyCollection<T>":
							{
								return (t.TypeArguments[0], true, true);
							}
						case "System.Collections.Generic.HashSet<T>":
							{
								return (t.TypeArguments[0], false, false);
							}
						case "System.Collections.Generic.ICollection<T>":
						case "System.Collections.Generic.List<T>":
						case "System.Collections.Generic.LinkedList<T>":
						case "System.Collections.Generic.Queue<T>":
						case "System.Collections.Generic.SortedSet<T>":
						case "System.Collections.Generic.Stack<T>":
							{
								return (t.TypeArguments[0], false, true);
							}
					}
				}

				return (default(ITypeSymbol), default(bool), default(bool));
			}

			(ITypeSymbol elementType, bool isReadonlyCollection, bool isOrdered) GetIsCollection(ITypeSymbol t)
			{
				var r = GetTypeArgumentIfItIsACollection(t as INamedTypeSymbol);

				if (r.elementType != null)
				{
					return r;
				}

				foreach (var @interface in t.AllInterfaces)
				{
					r = GetTypeArgumentIfItIsACollection(@interface);
					if (r.elementType != null)
					{
						return r;
					}
				}

				return (default(ITypeSymbol), default(bool), default(bool));
			}

			(elementType, isReadonlyCollection, isOrdered) = ImmutableInterlocked.GetOrAdd(ref _isCollection, type, GetIsCollection);
			return elementType != null;
		}

		private static ImmutableDictionary<ITypeSymbol, (ITypeSymbol, ITypeSymbol, bool)> _isDictionary =
			ImmutableDictionary<ITypeSymbol, (ITypeSymbol, ITypeSymbol, bool)>.Empty;

		public static bool IsDictionary(this ITypeSymbol type, out ITypeSymbol keyType, out ITypeSymbol valueType, out bool isReadonlyDictionary)
		{
			(ITypeSymbol keyType, ITypeSymbol valueType, bool isReadonlyDictionary) GetTypeArgumentIfItIsACollection(INamedTypeSymbol t)
			{
				if (t != null)
				{
					var interfaceDefinition = t.GetDefinitionType();

					switch (interfaceDefinition.ToString())
					{
						case "System.Collections.Generic.IReadOnlyDictionary<TKey, TValue>":
							{
								return (t.TypeArguments[0], t.TypeArguments[1], true);
							}
						case "System.Collections.Generic.IDictionary<TKey, TValue>":
							{
								return (t.TypeArguments[0], t.TypeArguments[1], false);
							}
					}
				}

				return (null, null, false);
			}

			(ITypeSymbol keyType, ITypeSymbol valueType, bool isReadonlyDictionary) GetIsACollection(ITypeSymbol t)
			{
				var r = GetTypeArgumentIfItIsACollection(t as INamedTypeSymbol);

				if (r.keyType != null)
				{
					return r;
				}

				foreach (var @interface in t.AllInterfaces)
				{
					r = GetTypeArgumentIfItIsACollection(@interface);
					if (r.keyType != null)
					{
						return r;
					}
				}

				return (null, null, false);
			}

			(keyType, valueType, isReadonlyDictionary) = ImmutableInterlocked.GetOrAdd(ref _isDictionary, type, GetIsACollection);
			return keyType != null;
		}

		private static MethodInfo _isAutoPropertyGetter;

		public static bool IsAutoProperty(this IPropertySymbol symbol)
		{
			if (symbol.IsWithEvents || symbol.IsIndexer || !symbol.IsReadOnly)
			{
				return false;
			}

			while (!Equals(symbol.OriginalDefinition, symbol))
			{
				// In some cases we're dealing with a derived type of `WrappedPropertySymbol`.
				// This code needs to deal with the SourcePropertySymbol from Roslyn,
				// the type containing the `IsAutoProperty` internal member.
				symbol = symbol.OriginalDefinition;
			}

			var type = symbol.GetType();
			switch (type.FullName)
			{
				case "Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE.PEPropertySymbol":
					return symbol.IsReadOnly; // It's from compiled code. We assume it's an auto-property when "read only"
				case "Microsoft.CodeAnalysis.CSharp.Symbols.SourcePropertySymbol":
					break; // ok
				default:
					throw new InvalidOperationException(
						"Unable to find the internal property `IsAutoProperty` on implementation of `IPropertySymbol`. " +
						"Should be on internal class `PropertySymbol`. Maybe you are using an incompatible version of Roslyn.");
			}

			if (_isAutoPropertyGetter == null)
			{
				_isAutoPropertyGetter = type
					.GetProperty("IsAutoProperty", BindingFlags.Instance | BindingFlags.NonPublic)
					.GetMethod;
			}

			var isAuto = _isAutoPropertyGetter.Invoke(symbol, new object[] { });
			return (bool)isAuto;
		}

		public static bool IsFromPartialDeclaration(this ISymbol symbol)
		{
			return symbol
				.DeclaringSyntaxReferences
				.Select(reference => reference.GetSyntax(CancellationToken.None))
				.OfType<ClassDeclarationSyntax>()
				.Any(node => node.Modifiers.Any(SyntaxKind.PartialKeyword));
		}

		private static ImmutableDictionary<ITypeSymbol, ITypeSymbol> _isFunc =
			ImmutableDictionary<ITypeSymbol, ITypeSymbol>.Empty;

		public static bool IsFunc(this ITypeSymbol type, out ITypeSymbol resultType)
		{
			ITypeSymbol GetIsFunc(ITypeSymbol t)
			{
				if (t != null && t is INamedTypeSymbol namedTypeSymbol)
				{
					var interfaceDefinition = t.GetDefinitionType();

					switch (interfaceDefinition.ToString())
					{
						case "System.Func<TResult>":
							return namedTypeSymbol.TypeArguments[0];
					}
				}

				return null;
			}

			resultType = ImmutableInterlocked.GetOrAdd(ref _isFunc, type, GetIsFunc);
			return resultType != null;
		}
	}
}
