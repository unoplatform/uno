using System;
using System.Collections.Generic;
using System.Linq;
using Uno.Disposables;
using System.Text;

namespace Uno.UI
{
	public static partial class ViewExtensions
	{
		/// <summary>
		/// (Mock) Gets an enumerator containing all the children of a View group
		/// </summary>
		/// <param name="group"></param>
		/// <returns></returns>
		internal static IEnumerable<object> GetChildren(this object group)
		{
			return Array.Empty<object>();
		}
	}
}
