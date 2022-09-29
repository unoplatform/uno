#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Java.Lang;

namespace Uno.UI
{
	public static partial class DebugHelper
	{
		/// <summary>
		/// Returns the complete current native stack. Useful in advanced debugging scenarios.
		/// </summary>		
		public static string[] NativeStackTrace => Thread.CurrentThread().GetStackTrace().Select(ste => ste.ToString()).ToArray();
	}
}
