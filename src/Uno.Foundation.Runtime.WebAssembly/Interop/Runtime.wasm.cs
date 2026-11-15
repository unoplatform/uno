#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Uno.Diagnostics.Eventing;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Uno.Foundation.Runtime.WebAssembly.Interop;
using Uno.Foundation.Logging;
using System.Globalization;
using System.Runtime.InteropServices.JavaScript;

namespace Uno.Foundation
{
	public static partial class WebAssemblyRuntime
	{
		private static readonly Logger _logger = typeof(WebAssemblyRuntime).Log();

		public static bool IsWebAssembly => PlatformHelper.IsWebAssembly;

		public static class TraceProvider
		{
			// {0B273C3E-11F6-47E7-ABDE-5A777893F3C0}
			public readonly static Guid Id = new Guid(0xb273c3e, 0x11f6, 0x47e7, new byte[] { 0xab, 0xde, 0x5a, 0x77, 0x78, 0x93, 0xf3, 0xc0 });
			public const int InvokeStart = 1;
			public const int InvokeEnd = 2;
			public const int InvokeException = 3;
			public const int UnmarshalledInvokedStart = 4;
			public const int UnmarshalledInvokedEnd = 5;
		}

		private readonly static IEventProvider _trace = Tracing.Get(TraceProvider.Id);

#pragma warning disable CA2211
		/// <summary>
		/// Provides an override for javascript invokes.
		/// </summary>
		public static Func<string, string>? InvokeJSOverride;
#pragma warning restore CA2211

		public static string InvokeJS(string str)
		{
			try
			{
				if (_trace.IsEnabled)
				{
					return InvokeJSWithTrace(str);
				}
				else
				{
					return InnerInvokeJS(str);
				}
			}
			catch (Exception e)
			{
				if (_trace.IsEnabled)
				{
					_trace.WriteEvent(
						TraceProvider.InvokeException,
						new object[] { str, e.ToString() }
					);
				}

				throw;
			}
		}

		/// <remarks>
		/// This method contains or is called by a try/catch containing method and
		/// can be significantly slower than other methods as a result on WebAssembly.
		/// See https://github.com/dotnet/runtime/issues/56309
		/// </remarks>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static string InvokeJSWithTrace(string str)
		{
			using (WritePropertyEventTrace(TraceProvider.InvokeStart, TraceProvider.InvokeEnd, str))
			{
				return InnerInvokeJS(str);
			}
		}

		private static string InnerInvokeJS(string str)
		{
			if (_logger.IsEnabled(LogLevel.Debug))
			{
				_logger.Debug("InvokeJS:" + str);
			}

			string result;

			if (InvokeJSOverride == null)
			{
				result = WebAssembly.Runtime.InvokeJS(str);
			}
			else
			{
				result = InvokeJSOverride(str);
			}

			return result;
		}

		public static object? GetObjectFromGcHandle(string intPtr)
		{
			var ptr = Marshal.StringToHGlobalAuto(intPtr);
			var handle = GCHandle.FromIntPtr(ptr);
			return handle.IsAllocated ? handle.Target : null;
		}

		public static string EscapeJs(string s)
		{
			if (s == null)
			{
				return "";
			}

			bool NeedsEscape(string s2)
			{
				for (int i = 0; i < s2.Length; i++)
				{
					var c = s2[i];

					if (
						c > 255
						|| c < 32
						|| c == '\\'
						|| c == '"'
						|| c == '\r'
						|| c == '\n'
						|| c == '\t'
					)
					{
						return true;
					}
				}

				return false;
			}

			if (NeedsEscape(s))
			{
				var r = new StringBuilder(s.Length);

				foreach (var c in s)
				{
					switch (c)
					{
						case '\\':
							r.Append("\\\\");
							continue;
						case '"':
							r.Append("\\\"");
							continue;
						case '\r':
							continue;
						case '\n':
							r.Append("\\n");
							continue;
						case '\t':
							r.Append("\\t");
							continue;
					}

					if (c < 32)
					{
						continue; // not displayable
					}

					if (c <= 255)
					{
						r.Append(c);
					}
					else
					{
						r.Append("\\u");
						r.Append(((ushort)c).ToString("X4", CultureInfo.InvariantCulture));
					}
				}

				return r.ToString();
			}
			else
			{
				return s;
			}
		}

		private static IDisposable? WritePropertyEventTrace(int startEventId, int stopEventId, string script)
		{
			if (_trace.IsEnabled)
			{
				return _trace.WriteEventActivity(
					startEventId,
					stopEventId,
					new object[] { script }
				);
			}
			else
			{
				return null;
			}
		}
	}
}
