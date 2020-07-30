using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Uno.UWPSyncGenerator
{
	internal class InheritanceTypeComparer : IEqualityComparer<ITypeSymbol>
	{
		public bool Equals(ITypeSymbol x, ITypeSymbol y)
		{
			while(y?.BaseType?.Name != "Object")
			{
				if(SymbolEqualityComparer.Default.Equals(x, y))
				{
					return true;
				}

				y = y.BaseType;
			}

			return false;
		}

		public int GetHashCode(ITypeSymbol obj) => obj.GetHashCode();
	}
}
