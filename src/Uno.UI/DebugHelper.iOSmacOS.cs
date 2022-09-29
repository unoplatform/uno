#nullable disable

using System;
using System.Collections.Generic;
using System.Text;
using Foundation;

namespace Uno.UI
{
	public static partial class DebugHelper
	{
		/// <summary>
		/// Returns the complete current native stack. Useful in advanced debugging scenarios.
		/// </summary>
		public static string[] NativeStackTrace => NSThread.NativeCallStack;
	}
}
