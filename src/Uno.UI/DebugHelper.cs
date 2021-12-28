using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Uno.UI
{
	public static partial class DebugHelper
	{
#if XAMARIN
		/// <summary>
		/// Dump the complete current native stack to debug output. Useful in advanced debugging scenarios.
		/// </summary>
		/// <returns>The stack as a single string</returns>
		public static string DumpNativeStack()
		{
			var sb = new StringBuilder();
			foreach (var st in NativeStackTrace)
			{
				sb.AppendLine(st);
			}

			var output = sb.ToString();
			Debug.Write(output);
			return output;
		}
#endif
	}
}
