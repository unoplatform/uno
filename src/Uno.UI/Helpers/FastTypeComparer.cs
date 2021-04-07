#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Uno.UI.Helpers
{
	/// <summary>
	/// A fast type comparer for dictionaries, to avoid going through object.Equals type checking. 
	/// To be used along with <see cref="Hashtable"/> when <see cref="Type"/> is the key.
	/// </summary>
	internal class FastTypeComparer : IEqualityComparer
	{
		public new bool Equals(object? x, object? y) => object.ReferenceEquals(x, y);

		public int GetHashCode(object? obj) => RuntimeHelpers.GetHashCode(obj);

		/// <summary>
		/// Provides a single instance
		/// </summary>
		public static FastTypeComparer Default { get; } = new FastTypeComparer();
	}
}
