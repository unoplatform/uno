using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
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
using System.Threading;
using System.Threading.Tasks;

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
			// Matches this signature:
			// https://github.com/mono/mono/blob/f24d652d567c4611f9b4e3095be4e2a1a2ab23a4/sdks/wasm/driver.c#L21
			[MethodImpl(MethodImplOptions.InternalCall)]
			public static extern IntPtr InvokeJSUnmarshalled(out string exceptionMessage, string functionIdentifier, IntPtr arg0, IntPtr arg1, IntPtr arg2);
		}
	}
}

namespace Uno.Foundation
{
	public static class WebAssemblyRuntime
	{
		private static Dictionary<string, IntPtr> MethodMap = new Dictionary<string, IntPtr>();

		private static readonly Lazy<ILogger> _logger = new Lazy<ILogger>(() => typeof(WebAssemblyRuntime).Log());

		
		public static bool IsWebAssembly { get; }
			// Origin of the value : https://github.com/mono/mono/blob/a65055dbdf280004c56036a5d6dde6bec9e42436/mcs/class/corlib/System.Runtime.InteropServices.RuntimeInformation/RuntimeInformation.cs#L115
			= RuntimeInformation.IsOSPlatform(OSPlatform.Create("WEBASSEMBLY")) // Legacy Value (Bootstrapper 1.2.0-dev.29 or earlier).
			|| RuntimeInformation.IsOSPlatform(OSPlatform.Create("BROWSER"));


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

		private static IntPtr GetMethodId(string methodName)
		{
			if (!MethodMap.TryGetValue(methodName, out var methodId))
			{
				MethodMap[methodName] = methodId = WebAssembly.JSInterop.InternalCalls.InvokeJSUnmarshalled(out _, methodName, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
			}

			return methodId;
		}


		/// <summary>
		/// Invoke a Javascript method using unmarshaled conversion.
		/// </summary>
		/// <param name="functionIdentifier">A function identifier name</param>
		public static bool InvokeJSUnmarshalled(string functionIdentifier, IntPtr arg0)
		{
			if (_trace.IsEnabled)
			{
				using (WritePropertyEventTrace(TraceProvider.UnmarshalledInvokedStart, TraceProvider.UnmarshalledInvokedEnd, functionIdentifier))
				{
					var ret = InnerInvokeJSUnmarshalled(functionIdentifier, arg0, out var exception);

					if(exception != null)
					{
						throw exception;
					}

					return ret;
				}
			}
			else
			{
				var ret = InnerInvokeJSUnmarshalled(functionIdentifier, arg0, out var exception);

				if (exception != null)
				{
					throw exception;
				}

				return ret;
			}
		}

		/// <summary>
		/// Invoke a Javascript method using unmarshaled conversion.
		/// </summary>
		/// <param name="functionIdentifier">A function identifier name</param>
		internal static bool InvokeJSUnmarshalled(string functionIdentifier, IntPtr arg0, out Exception exception)
		{
			if (_trace.IsEnabled)
			{
				using (WritePropertyEventTrace(TraceProvider.UnmarshalledInvokedStart, TraceProvider.UnmarshalledInvokedEnd, functionIdentifier))
				{
					return InnerInvokeJSUnmarshalled(functionIdentifier, arg0, out exception);
				}
			}
			else
			{
				return InnerInvokeJSUnmarshalled(functionIdentifier, arg0, out exception);
			}
		}

		private static bool InnerInvokeJSUnmarshalled(string functionIdentifier, IntPtr arg0, out Exception exception)
		{
			exception = null;
			var methodId = GetMethodId(functionIdentifier);

			var res = WebAssembly.JSInterop.InternalCalls.InvokeJSUnmarshalled(out var exceptionMessage, null, methodId, arg0, IntPtr.Zero);

			if (!string.IsNullOrEmpty(exceptionMessage))
			{
				if (_trace.IsEnabled)
				{
					_trace.WriteEvent(
						TraceProvider.InvokeException,
						new object[] { functionIdentifier, exception.ToString() }
					);
				}

				exception = new Exception(exceptionMessage);
			}

			return res != IntPtr.Zero;
		}

		/// <summary>
		/// Invoke a Javascript method using unmarshaled conversion.
		/// </summary>
		/// <param name="functionIdentifier">A function identifier name</param>
		public static bool InvokeJSUnmarshalled(string functionIdentifier, IntPtr arg0, IntPtr arg1)
		{
			if (_trace.IsEnabled)
			{
				using (WritePropertyEventTrace(TraceProvider.UnmarshalledInvokedStart, TraceProvider.UnmarshalledInvokedEnd, functionIdentifier))
				{
					return InnerInvokeJSUnmarshalled(functionIdentifier, arg0, arg1);
				}
			}
			else
			{
				return InnerInvokeJSUnmarshalled(functionIdentifier, arg0, arg1);
			}
		}

		private static bool InnerInvokeJSUnmarshalled(string functionIdentifier, IntPtr arg0, IntPtr arg1)
		{
			var methodId = GetMethodId(functionIdentifier);

			var res = WebAssembly.JSInterop.InternalCalls.InvokeJSUnmarshalled(out var exception, null, methodId, arg0, arg1);

			if (exception != null)
			{
				if (_trace.IsEnabled)
				{
					_trace.WriteEvent(
						TraceProvider.InvokeException,
						new object[] { functionIdentifier, exception.ToString() }
					);
				}

				throw new Exception(exception);
			}

			return res != IntPtr.Zero;
		}

		/// <summary>
		/// Provides an override for javascript invokes.
		/// </summary>
		public static Func<string, string> InvokeJSOverride;

		public static string InvokeJS(string str)
		{
			try
			{
				if (_trace.IsEnabled)
				{
					using (WritePropertyEventTrace(TraceProvider.InvokeStart, TraceProvider.InvokeEnd, str))
					{
						return InnerInvokeJS(str);
					}
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

		private static string InnerInvokeJS(String str)
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

			return result;
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

			return InvokeJS(command);
		}

		private static readonly Dictionary<long, TaskCompletionSource<string>> _asyncWaitingList = new Dictionary<long, TaskCompletionSource<string>>();

		private static long _nextAsync;

		/// <summary>
		/// Invoke async javascript code.
		/// </summary>
		/// <remarks>
		/// The javascript code is expected to return a Promise&lt;string&gt;
		/// </remarks>
		public static Task<string> InvokeAsync(string promiseCode)
		{
			var id = Interlocked.Increment(ref _nextAsync);

			var tcs = new TaskCompletionSource<string>();

			lock (_asyncWaitingList)
			{
				_asyncWaitingList[id] = tcs;
			}

			var js = new[]
			{
				"const __f = ()=>",
				promiseCode,
				";\nUno.UI.Interop.AsyncInteropHelper.Invoke(",
				id.ToStringInvariant(),
				", __f);"
			};

			try
			{

				WebAssemblyRuntime.InvokeJS(string.Concat(js));
			}
			catch (Exception ex)
			{
				return Task.FromException<string>(ex);
			}

			return tcs.Task;
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		public static void DispatchAsyncResult(long handle, string result)
		{
			lock (_asyncWaitingList)
			{
				if (_asyncWaitingList.TryGetValue(handle, out var tcs))
				{
					tcs.TrySetResult(result);
					_asyncWaitingList.Remove(handle);
				}
			}
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		public static void DispatchAsyncError(long handle, string error)
		{
			lock (_asyncWaitingList)
			{
				if (_asyncWaitingList.TryGetValue(handle, out var tcs))
				{
					var exception = new ApplicationException(error);
					tcs.TrySetException(exception);
					_asyncWaitingList.Remove(handle);
				}
			}
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
