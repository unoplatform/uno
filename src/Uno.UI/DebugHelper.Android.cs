using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Java.Lang;

namespace Uno.UI
{
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static partial class DebugHelper
	{
		/// <summary>
		/// Returns the complete current native stack. Useful in advanced debugging scenarios.
		/// </summary>
		[EditorBrowsable(EditorBrowsableState.Never)]
		public static string[] NativeStackTrace => Thread.CurrentThread().GetStackTrace().Select(ste => ste.ToString()).ToArray();
	}
}
