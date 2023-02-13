using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Uno.UWPSyncGenerator
{
	internal class InheritanceTypeComparer : IEqualityComparer<ITypeSymbol>
	{
		public static readonly InheritanceTypeComparer Instance = new InheritanceTypeComparer();

		private InheritanceTypeComparer()
		{
		}

		public bool Equals(ITypeSymbol x, ITypeSymbol y)
		{
			while (y?.BaseType?.SpecialType != SpecialType.System_Object)
			{
				if (SymbolEqualityComparer.Default.Equals(x, y))
				{
					return true;
				}

				y = y.BaseType;
			}

			return false;
		}

		public int GetHashCode(ITypeSymbol obj) => throw new NotImplementedException();
	}
}
