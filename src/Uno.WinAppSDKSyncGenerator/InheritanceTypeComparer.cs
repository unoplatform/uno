using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Uno.WinAppSDKSyncGenerator
{
	internal class InheritanceTypeComparer : IEqualityComparer<ITypeSymbol>
	{
		public static readonly InheritanceTypeComparer Instance = new InheritanceTypeComparer();

		private InheritanceTypeComparer()
		{
		}

		public bool Equals(ITypeSymbol x, ITypeSymbol y)
		{
			if (x is null || y is null)
			{
				return x is null && y is null;
			}

			// Walk up y's inheritance chain to check if x is y or a base type of y.
			var current = y;
			while (current is not null)
			{
				if (SymbolEqualityComparer.Default.Equals(x, current))
				{
					return true;
				}

				if (current.SpecialType == SpecialType.System_Object)
				{
					break;
				}

				current = current.BaseType;
			}

			return false;
		}

		public int GetHashCode(ITypeSymbol obj) => throw new NotImplementedException();
	}
}
