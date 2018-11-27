using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics.Contracts;
using System.Reflection;
using System.Runtime.InteropServices;
using Uno.Extensions;
using Uno.Foundation.Interop;
using System.Text;
using Uno.Diagnostics.Eventing;
using Microsoft.Extensions.Logging;
using Uno.Logging;
using System.Runtime.CompilerServices;

namespace WebAssembly
{
	[Obfuscation(Feature = "renaming", Exclude = true)]
	internal sealed class Runtime
	{
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern string InvokeJS(string str, out int exceptional_result);

		internal static string InvokeJS(string str)
		{
			var r = InvokeJS(str, out var exceptionResult);
			if (exceptionResult != 0)
			{
				Console.Error.WriteLine($"Error #{exceptionResult} \"{r}\" executing javascript: \"{str}\"");
			}
			return r;
		}
	}

	namespace JSInterop
	{
		internal static class InternalCalls
		{
			[MethodImpl(MethodImplOptions.InternalCall)]
			public static extern TRes InvokeJSUnmarshalled<T0, T1, T2, TRes>(out string exception, string functionIdentifier, T0 arg0, T1 arg1, T2 arg2);
		}
	}
}

namespace Uno.Foundation
{
	public static class WebAssemblyRuntime
	{
		private static readonly Lazy<ILogger> _logger = new Lazy<ILogger>(() => typeof(WebAssemblyRuntime).Log());

		public static bool IsWebAssembly { get; }
			= RuntimeInformation.IsOSPlatform(OSPlatform.Create("WEBASSEMBLY"));


		[Preserve]
		public static class TraceProvider
		{
			// {0B273C3E-11F6-47E7-ABDE-5A777893F3C0}
			public readonly static Guid Id = new Guid(0xb273c3e, 0x11f6, 0x47e7, new byte[] { 0xab, 0xde, 0x5a, 0x77, 0x78, 0x93, 0xf3, 0xc0 });
			public const int InvokeStart = 1;
			public const int InvokeEnd = 2;
			public const int InvokeException = 3;
		}

		private readonly static IEventProvider _trace = Tracing.Get(TraceProvider.Id);

		/// <summary>
		/// Invoke a Javascript method using unmarshaled conversion.
		/// </summary>
		/// <param name="functionIdentifier">A function identifier name</param>
		public static TRes InvokeJSUnmarshalled<T0, TRes>(string functionIdentifier, T0 arg0)
		{
			var res = WebAssembly.JSInterop.InternalCalls.InvokeJSUnmarshalled<T0, object, object, TRes>(out var exception, functionIdentifier, arg0, null, null);

			if(exception != null)
			{
				throw new Exception(exception);
			}

			return res;
		}

		/// <summary>
		/// Invoke a Javascript method using unmarshaled conversion.
		/// </summary>
		/// <param name="functionIdentifier">A function identifier name</param>
		public static TRes InvokeJSUnmarshalled<T0, T1, TRes>(string functionIdentifier, T0 arg0, T1 arg1)
		{
			var res = WebAssembly.JSInterop.InternalCalls.InvokeJSUnmarshalled<T0, T1, object, TRes>(out var exception, functionIdentifier, arg0, arg1, null);

			if (exception != null)
			{
				throw new Exception(exception);
			}

			return res;
		}

		/// <summary>
		/// Invoke a Javascript method using unmarshaled conversion.
		/// </summary>
		/// <param name="functionIdentifier">A function identifier name</param>
		public static TRes InvokeJSUnmarshalled<T0, T1, T2, TRes>(string functionIdentifier, T0 arg0, T1 arg1, T2 arg2)
		{
			var res = WebAssembly.JSInterop.InternalCalls.InvokeJSUnmarshalled<T0, T1, T2, TRes>(out var exception, functionIdentifier, arg0, arg1, arg2);

			if (exception != null)
			{
				throw new Exception(exception);
			}

			return res;
		}

		/// <summary>
		/// Provides an override for javascript invokes.
		/// </summary>
		public static Func<string, string> InvokeJSOverride;

		public static string InvokeJS(string str)
		{
			using (WritePropertyEventTrace(TraceProvider.InvokeStart, TraceProvider.InvokeEnd, str))
			{
				try
				{
					if (_logger.Value.IsEnabled(LogLevel.Debug))
					{
						_logger.Value.Debug("InvokeJS:" + str);
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

					if(result == null)
					{
						throw new InvalidOperationException("The invoked Javascript method did not return a value (" + str + ")");
					}

					return result;
				}
				catch(Exception e)
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
		}

		public static object GetObjectFromGcHandle(string intPtr)
		{
			var ptr = Marshal.StringToHGlobalAuto(intPtr);
			var handle = GCHandle.FromIntPtr(ptr);
			return handle.IsAllocated ? handle.Target : null;
		}

		public static string InvokeJSWithInterop(FormattableString formattable)
		{
			string command;
			if (formattable.ArgumentCount == 0)
			{
				command = formattable.ToString();
			}
			else
			{
				var commandBuilder = new IndentedStringBuilder();
				commandBuilder.Append("(function() {");

				var parameters = formattable.GetArguments();
				var mappedParameters = new Dictionary<IJSObject, string>();

				for (var i = 0; i < parameters.Length; i++)
				{
					var parameter = parameters[i];
					if (parameter is IJSObject jsObject)
					{
						if (!mappedParameters.TryGetValue(jsObject, out var parameterReference))
						{
							mappedParameters[jsObject] = parameterReference = $"__parameter_{i}";
							commandBuilder.AppendLine($"var {parameterReference} = {jsObject.Handle.GetNativeInstance()};");
						}

						parameters[i] = parameterReference;
					}
				}

				commandBuilder.AppendFormatInvariant(formattable.Format, parameters);
				commandBuilder.Append("return \"ok\"; })();");

				command = commandBuilder.ToString();
			}

			return WebAssembly.Runtime.InvokeJS(command);
		}

		[Pure]
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
						r.Append(((ushort)c).ToString("X4"));
					}
				}

				return r.ToString();
			}
			else
			{
				return s;
			}
		}

		private static IDisposable WritePropertyEventTrace(int startEventId, int stopEventId, string script)
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
