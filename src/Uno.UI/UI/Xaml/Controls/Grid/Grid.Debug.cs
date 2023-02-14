using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls
{
	partial class Grid
	{
		[Conditional("DEBUG")]
		private static void ASSERT(bool assertion, string message = null)
		{
			global::System.Diagnostics.Debug.Assert(assertion, message);
		}
	}
}
