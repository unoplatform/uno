#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using Uno.Foundation.Interop;
using System.Text;
using Uno.Diagnostics.Eventing;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Uno.Foundation.Runtime.WebAssembly.Interop;
using Uno.Foundation.Logging;
using System.Globalization;
using Uno.Foundation.Runtime.WebAssembly.Helpers;
using System.Runtime.InteropServices.JavaScript;

namespace Uno.Foundation
{
	public static partial class WebAssemblyRuntime
	{
		private static Dictionary<string, IntPtr> MethodMap = new Dictionary<string, IntPtr>();

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

		private static IntPtr GetMethodId(string methodName)
		{
			if (!MethodMap.TryGetValue(methodName, out var methodId))
			{
				MethodMap[methodName] = methodId = WebAssembly.Runtime.InvokeJSUnmarshalled(methodName, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
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
				return InvokeJSUnmarshalledWithTrace(functionIdentifier, arg0);
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

		/// <remarks>
		/// This method contains or is called by a try/catch containing method and
		/// can be significantly slower than other methods as a result on WebAssembly.
		/// See https://github.com/dotnet/runtime/issues/56309
		/// </remarks>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool InvokeJSUnmarshalledWithTrace(string functionIdentifier, IntPtr arg0)
		{
			using (WritePropertyEventTrace(TraceProvider.UnmarshalledInvokedStart, TraceProvider.UnmarshalledInvokedEnd, functionIdentifier))
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
		internal static bool InvokeJSUnmarshalled(string functionIdentifier, IntPtr arg0, out Exception? exception)
		{
			if (_trace.IsEnabled)
			{
				return InvokeJSUnmarshalledWithTrace(functionIdentifier, arg0, out exception);
			}
			else
			{
				return InnerInvokeJSUnmarshalled(functionIdentifier, arg0, out exception);
			}
		}

		/// <remarks>
		/// This method contains or is called by a try/catch containing method and
		/// can be significantly slower than other methods as a result on WebAssembly.
		/// See https://github.com/dotnet/runtime/issues/56309
		/// </remarks>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool InvokeJSUnmarshalledWithTrace(string functionIdentifier, IntPtr arg0, out Exception? exception)
		{
			using (WritePropertyEventTrace(TraceProvider.UnmarshalledInvokedStart, TraceProvider.UnmarshalledInvokedEnd, functionIdentifier))
			{
				return InnerInvokeJSUnmarshalled(functionIdentifier, arg0, out exception);
			}
		}

		private static bool InnerInvokeJSUnmarshalled(string functionIdentifier, IntPtr arg0, out Exception? exception)
		{
			exception = null;
			var methodId = GetMethodId(functionIdentifier);

			try
			{
				return WebAssembly.Runtime.InvokeJSUnmarshalled(null, methodId, arg0, IntPtr.Zero) != 0;
			}
			catch (Exception e)
			{
				exception = e;
				return false;
			}
		}

		/// <summary>
		/// Invoke a Javascript method using unmarshaled conversion.
		/// </summary>
		/// <param name="functionIdentifier">A function identifier name</param>
		public static bool InvokeJSUnmarshalled(string functionIdentifier, IntPtr arg0, IntPtr arg1)
		{
			if (_trace.IsEnabled)
			{
				return InvokeJSUnmarshalledWithTrace(functionIdentifier, arg0, arg1);
			}
			else
			{
				return InnerInvokeJSUnmarshalled(functionIdentifier, arg0, arg1);
			}
		}

		/// <remarks>
		/// This method contains or is called by a try/catch containing method and
		/// can be significantly slower than other methods as a result on WebAssembly.
		/// See https://github.com/dotnet/runtime/issues/56309
		/// </remarks>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool InvokeJSUnmarshalledWithTrace(string functionIdentifier, IntPtr arg0, IntPtr arg1)
		{
			using (WritePropertyEventTrace(TraceProvider.UnmarshalledInvokedStart, TraceProvider.UnmarshalledInvokedEnd, functionIdentifier))
			{
				return InnerInvokeJSUnmarshalled(functionIdentifier, arg0, arg1);
			}
		}

		private static bool InnerInvokeJSUnmarshalled(string functionIdentifier, IntPtr arg0, IntPtr arg1)
		{
			var methodId = GetMethodId(functionIdentifier);

			return WebAssembly.Runtime.InvokeJSUnmarshalled(null, methodId, arg0, arg1) != 0;
		}

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

		public static string InvokeJSWithInterop(FormattableString formattable)
		{
			string command;
			if (formattable.ArgumentCount == 0)
			{
				command = formattable.ToString(CultureInfo.InvariantCulture);
			}
			else
			{
				var commandBuilder =
#if DEBUG
					new IndentedStringBuilder();
#else
					new StringBuilder();
#endif

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
							if (!jsObject.Handle.IsAlive)
							{
								throw new InvalidOperationException("JSObjectHandle is invalid.");
							}

							mappedParameters[jsObject] = parameterReference = $"__parameter_{i}";
							commandBuilder.AppendLine($"const {parameterReference} = {jsObject.Handle.GetNativeInstance()};");
						}

						parameters[i] = parameterReference;
					}
				}

#if DEBUG
				commandBuilder.AppendFormatInvariant(formattable.Format, parameters);
#else
				commandBuilder.AppendFormat(CultureInfo.InvariantCulture, formattable.Format, parameters);
#endif
				commandBuilder.Append("return \"ok\"; })();");

				command = commandBuilder.ToString();
			}

			return InvokeJS(command);
		}

		private static readonly Dictionary<long, (TaskCompletionSource<string> task, CancellationTokenRegistration ctReg)> _asyncWaitingList
			= new Dictionary<long, (TaskCompletionSource<string> task, CancellationTokenRegistration ctReg)>();

		private static long _nextAsync;


		/// <summary>
		/// DO NOT USE, use overload with CancellationToken instead
		/// </remarks>
		public static Task<string> InvokeAsync(string promiseCode)
			=> InvokeAsync(promiseCode, CancellationToken.None);

		/// <summary>
		/// Invoke async javascript code.
		/// </summary>
		/// <remarks>
		/// The javascript code is expected to return a Promise&lt;string&gt;
		/// </remarks>
		public static Task<string> InvokeAsync(string promiseCode, CancellationToken ct)
		{
			var handle = Interlocked.Increment(ref _nextAsync);
			var tcs = new TaskCompletionSource<string>();
			var ctReg = ct.CanBeCanceled ? ct.Register(() => RemoveAsyncTask(handle)?.TrySetCanceled()) : default;

			lock (_asyncWaitingList)
			{
				_asyncWaitingList[handle] = (tcs, ctReg);
			}

			var js = new[]
			{
				"const __f = ()=>",
				promiseCode,
				";\nUno.UI.Interop.AsyncInteropHelper.Invoke(",
				handle.ToString(CultureInfo.InvariantCulture),
				", __f);"
			};

			try
			{
				WebAssemblyRuntime.InvokeJS(string.Concat(js));

				return tcs.Task;
			}
			catch (Exception ex)
			{
				RemoveAsyncTask(handle);

				return Task.FromException<string>(ex);
			}
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		[JSExport]
		public static void DispatchAsyncResult([JSMarshalAs<JSType.Number>] long handle, string result)
			=> RemoveAsyncTask(handle)?.TrySetResult(result);

		[EditorBrowsable(EditorBrowsableState.Never)]
		[JSExport]
		public static void DispatchAsyncError([JSMarshalAs<JSType.Number>] long handle, string error)
			=> RemoveAsyncTask(handle)?.TrySetException(new ApplicationException(error));

		private static TaskCompletionSource<string>? RemoveAsyncTask(long handle)
		{
			(TaskCompletionSource<string> task, CancellationTokenRegistration ctReg) listener;
			lock (_asyncWaitingList)
			{
				if (!_asyncWaitingList.TryGetValue(handle, out listener))
				{
					return default;
				}
				_asyncWaitingList.Remove(handle);
			}

			listener.ctReg.Dispose();

			return listener.task;
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
