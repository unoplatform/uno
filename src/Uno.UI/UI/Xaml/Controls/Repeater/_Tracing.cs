using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.UI.Xaml.Controls
{
	internal static class _Tracing
	{
		[Conditional("TRACE")]
		public static void REPEATER_TRACE_INFO(string text, params object[] parameters)
			=> Console.WriteLine(text, parameters);

		[Conditional("TRACE")]
		public static void REPEATER_TRACE_PERF(string text)
			=> Console.WriteLine(text);

		[Conditional("DEBUG")]
		public static void MUX_ASSERT(bool assertion, string message = null)
			=> global::System.Diagnostics.Debug.Assert(assertion, message);
	}
}
