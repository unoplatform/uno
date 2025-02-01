#nullable enable

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Uno.Analyzers;

internal static class SymbolExtensions
{
	public static bool DerivesFrom(this INamedTypeSymbol? symbol, INamedTypeSymbol? expectedBaseClass)
	{
		while (symbol is not null)
		{
			if (symbol.Equals(expectedBaseClass, SymbolEqualityComparer.Default))
			{
				return true;
			}

			symbol = symbol.BaseType;
		}

		return false;
	}
}
