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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Uno.SourceGenerators.Helpers
{
	public static class NamedTypeSymbolExtensions
	{
		/// <summary>
		/// Get symbolic names associated weith the INamedTypeSymbol
		/// </summary> 
		/// <param name="typeToUseForSubstitutions">The concrete nametype symbol type to replace with(example using a concretized data type</param>
		/// <returns></returns>
		public static SymbolNames GetSymbolNames(this INamedTypeSymbol typeSymbol, INamedTypeSymbol typeToUseForSubstitutions = null)
		{
			var substitutions = typeToUseForSubstitutions.GetSubstitutionTypes();

			var symbolName = typeSymbol.Name;

			// Generate a filename suffix from the namespace to prevent filename clashing when processing classes with the same name.
			var filenameSuffix = $"{typeSymbol.GetNamespaceHash():X}";

			if (typeSymbol.TypeArguments.Length == 0) // not a generic type
			{
				return new SymbolNames(typeSymbol, symbolName, "", symbolName, symbolName, symbolName, $"{symbolName}_{filenameSuffix}", "");
			}

			var argumentNames = typeSymbol.GetTypeArgumentNames(substitutions);
			var genericArguments = string.Join(", ", argumentNames);

			// symbolNameWithGenerics: MyType<T1, T2>
			var symbolNameWithGenerics = $"{symbolName}<{genericArguments}>";

			// symbolNameWithGenerics: MyType&lt;T1, T2&gt;
			var symbolForXml = $"{symbolName}&lt;{genericArguments}&gt;";

			// symbolNameDefinition: MyType<,>
			var symbolNameDefinition = $"{symbolName}<{string.Join(",", typeSymbol.TypeArguments.Select(ta => ""))}>";

			// symbolNameWithGenerics: MyType_T1_T2
			var symbolFilename = $"{symbolName}_{string.Join("_", argumentNames)}_{filenameSuffix}";

			var genericConstraints = " " + string.Join(" ", typeSymbol
				.TypeArguments
				.OfType<ITypeParameterSymbol>()
				.SelectMany((tps, i) => tps.ConstraintTypes.Select(c => (tps: tps, c: c, i: i)))
				.Select(x => $"where {argumentNames[x.i]} : {x.c}"));

			return new SymbolNames(typeSymbol, symbolName, $"<{genericArguments}>", symbolNameWithGenerics, symbolForXml, symbolNameDefinition, symbolFilename, genericConstraints);
		}

		/// <summary>
		///  
		/// </summary>
		/// <param name="typeSymbol"></param>
		/// <param name="substitutions"></param>
		/// <returns></returns>
		public static string[] GetTypeArgumentNames(
			this ITypeSymbol typeSymbol,
			(string argumentName, string type)[] substitutions)
		{
			if (typeSymbol is INamedTypeSymbol namedSymbol)
			{
				var dict = substitutions.ToDictionary(x => x.argumentName, x => x.type);

				return namedSymbol.TypeArguments
					.Select(ta => ta.ToString())
					.Select(t => dict.ContainsKey(t) ? dict[t] : t)
					.ToArray();
			}
			return new string[0];
		}

		public static (string argumentName, string type)[] GetSubstitutionTypes(this INamedTypeSymbol type)
		{
			if (type == null || type.TypeArguments.Length == 0)
			{
				return new(string, string)[] { };
			}

			var argumentParameters = type.TypeParameters;
			var argumentTypes = type.TypeArguments;

			var result = new(string, string)[type.TypeArguments.Length];

			for (var i = 0; i < argumentParameters.Length; i++)
			{
				var parameterType = argumentTypes[i] as INamedTypeSymbol;
				var parameterNames = parameterType.GetSymbolNames();

				result[i] = (argumentParameters[i].Name, parameterNames.GetSymbolFullNameWithGenerics());
			}

			return result;
		}

		private static int GetNamespaceHash(this INamedTypeSymbol typeSymbol)
		{
			var hashCode = 0;
			INamespaceSymbol namespaceSymbol = typeSymbol.ContainingNamespace;
			while (!namespaceSymbol?.IsGlobalNamespace ?? false)
			{
				hashCode ^= namespaceSymbol.Name.GetStableHashCode();
				namespaceSymbol = namespaceSymbol.ContainingNamespace;
			}

			return hashCode;
		}

		/// <summary>
		///  A fully managed version of the 64bit GetHashCode() that does not use any randomization and will always return the same value for equal strings.
		///  <see cref=" https://stackoverflow.com/questions/36845430/persistent-hashcode-for-strings"00/>
		/// </summary>
		/// <param name="str"></param>
		/// <returns></returns>
		private static int GetStableHashCode(this string str)
		{
			unchecked
			{
				int hash1 = 5381;
				int hash2 = hash1;

				for (int i = 0; i < str.Length && str[i] != '\0'; i += 2)
				{
					hash1 = ((hash1 << 5) + hash1) ^ str[i];
					if (i == str.Length - 1 || str[i + 1] == '\0')
						break;
					hash2 = ((hash2 << 5) + hash2) ^ str[i + 1];
				}

				return hash1 + (hash2 * 1566083941);
			}
		}
	}

	public class SymbolNames
	{
		public SymbolNames(
			INamedTypeSymbol symbol,
			string symbolName,
			string genericArguments,
			string symbolNameWithGenerics,
			string symbolFoxXml,
			string symbolNameDefinition,
			string symbolFilename,
			string genericConstraints)
		{
			Symbol = symbol;
			SymbolName = symbolName;
			GenericArguments = genericArguments;
			SymbolNameWithGenerics = symbolNameWithGenerics;
			SymbolFoxXml = symbolFoxXml;
			SymbolNameDefinition = symbolNameDefinition;
			SymbolFilename = symbolFilename;
			GenericConstraints = genericConstraints;
		}

		public INamedTypeSymbol Symbol { get; }

		/// <summary>
		/// The name of the symbol, without any namespace, container type or generic decorations.
		/// </summary>
		public string SymbolName { get; }

		/// <summary>
		/// Generic arguments, if any.  Ex: `&lt;T1, T2&gt;`
		/// </summary>
		/// <remarks>
		/// Empty if no generics
		/// </remarks>
		public string GenericArguments { get; }

		/// <summary>
		/// SymbolName + GenericArguments, Ex: `MyType&lt;T1, T2&gt;`
		/// </summary>
		public string SymbolNameWithGenerics { get; }

		/// <summary>
		/// Same as SymbolNameWithGenerics but escaped for XML
		/// </summary>
		public string SymbolFoxXml { get; }

		/// <summary>
		/// Definition for symbol name, ex: `MyType&lt;,&gt;`
		/// </summary>
		public string SymbolNameDefinition { get; }

		/// <summary>
		/// Appropriate result filename for the type
		/// </summary>
		public string SymbolFilename { get; }

		/// <summary>
		/// Generic constraints on the type, Ex: `where T1 : string`
		/// </summary>
		/// <remarks>
		/// Empty if no generics or no constraints
		/// </remarks>
		public string GenericConstraints { get; }

		private string GetContainingTypeFullName(INamedTypeSymbol typeForSubstitutions = null)
		{
			if (Symbol.ContainingType != null)
			{
				return Symbol
					.ContainingType
					.GetSymbolNames(typeForSubstitutions)
					.GetSymbolFullNameWithGenerics();
			}

			return "global::" + Symbol.ContainingNamespace;
		}

		private static readonly IReadOnlyList<SpecialType> LanguageSupportedSpecialTypes = new[]
		{
			SpecialType.System_Boolean,
			SpecialType.System_Byte,
			SpecialType.System_Char,
			SpecialType.System_Decimal,
			SpecialType.System_Double,
			SpecialType.System_Int16,
			SpecialType.System_Int32,
			SpecialType.System_Int64,
			SpecialType.System_Object,
			SpecialType.System_SByte,
			SpecialType.System_String,
			SpecialType.System_UInt16,
			SpecialType.System_UInt32,
			SpecialType.System_UInt64,
		};

		public string GetSymbolFullNameWithGenerics(INamedTypeSymbol typeForSubstitutions = null)
		{
			if ((!Symbol.IsGenericType || typeForSubstitutions == null)
				&& LanguageSupportedSpecialTypes.Contains(Symbol.SpecialType))
			{
				return Symbol.ToString();
			}

			if (Symbol.IsTupleType)
			{
				var tupleElements = Symbol.TupleElements
					.Select(t =>
					{
						var type = t.Type.GetSymbolNames(typeForSubstitutions)?.GetSymbolFullNameWithGenerics(typeForSubstitutions) ?? t.Type.ToString();
						var name = t.Name;

						if (string.IsNullOrEmpty(name))
						{
							return type;
						}

						return type + " " + name;
					});

				return "(" + string.Join(", ", tupleElements) + ")";
			}

			if (string.IsNullOrEmpty(SymbolNameWithGenerics))
			{
				return Symbol.ToString();
			}

			return GetContainingTypeFullName(typeForSubstitutions) + "." + SymbolNameWithGenerics;
		}


		public void Deconstruct(
			out string symbolName,
			out string genericArguments,
			out string symbolNameWithGenerics,
			out string symbolFoxXml,
			out string symbolNameDefinition,
			out string symbolFilename,
			out string genericConstraints)
		{
			symbolName = SymbolName;
			genericArguments = GenericArguments;
			symbolNameWithGenerics = SymbolNameWithGenerics;
			symbolFoxXml = SymbolFoxXml;
			symbolNameDefinition = SymbolNameDefinition;
			symbolFilename = SymbolFilename;
			genericConstraints = GenericConstraints;
		}
	}
}
