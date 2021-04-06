using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Uno.Foundation.Interop
{
	/// <summary>
	/// A cached version of <see cref="Marshal.SizeOf(object)"/>
	/// </summary>
	internal class MarshalSizeOfHelper
	{
		/// <summary>
		/// Use a Hashtable here as it's still faster than <see cref="Dictionary{TKey, TValue}"/> on
		/// various platforms, but particularly on wasm. https://github.com/dotnet/runtime/issues/50757
		/// </summary>
		private static Hashtable _cache = new Hashtable();

		/// <summary>
		/// Gets the marshalled size of the specified type
		/// </summary>
		internal static int SizeOf(Type type)
		{
			if(_cache[type] is int value)
			{
				return value;
			}
			else
			{
				_cache[type] = value = Marshal.SizeOf(type);
				return value;
			}
		}
	}
}
