using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.UI.Xaml.Controls
{
	internal static partial class _Tracing
	{
		[Conditional("REPEATER_TRACE_ENABLED")]
		public static void REPEATER_TRACE_INFO(string text, params object[] parameters)
		{
			var builder = new StringBuilder();
			var param = parameters.GetEnumerator();
			var needsIndent = text.Count(c => c == '%') < parameters.Length;

			if (needsIndent && param.MoveNext() && param.Current is int indent)
			{
				builder.Append('\t', indent);
			}

			for (var textIndex = 0; textIndex < text.Length; textIndex++)
			{
				var c = text[textIndex];
				if (c == '%' && param.MoveNext())
				{
					if (text.Length > textIndex + 4
						&& text.Substring(textIndex, 4) == "%.0f"
						&& param.Current is IFormattable formattable)
					{
						textIndex += 3;
						builder.Append(formattable.ToString("F1", CultureInfo.InvariantCulture));
					}
					else
					{
						builder.Append(param.Current);
					}
				}
				else
				{
					builder.Append(c);
				}
			}

			Console.Write(builder.ToString());
		}

		[Conditional("REPEATER_TRACE_ENABLED")]
		public static void REPEATER_TRACE_PERF(string text)
			=> Console.WriteLine(text);

		[Conditional("DEBUG")]
		public static void MUX_ASSERT(bool assertion, string message = null)
			=> global::System.Diagnostics.Debug.Assert(assertion, message);
	}
}
