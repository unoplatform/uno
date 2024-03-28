using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Microsoft/* UWP don't rename */.UI.Xaml.Controls;

namespace Windows.UI.Xaml.Controls
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
