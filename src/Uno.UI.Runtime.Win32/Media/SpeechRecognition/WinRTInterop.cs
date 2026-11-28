#nullable enable

using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Uno.UI.Runtime.Win32;

// Minimal WinRT ABI access for OS APIs the host consumes. The runtime targets plain netX without a
// CsWinRT projection, so activation, HSTRINGs and vtable calls are done by hand. Interface pointers
// are passed around as nint so callers can hold them across awaits.
internal static unsafe partial class WinRTInterop
{
	internal const int S_OK = 0;
	internal const int E_NOINTERFACE = unchecked((int)0x80004002);
	private const int CO_E_NOTINITIALIZED = unchecked((int)0x800401F0);
	private const int RO_INIT_MULTITHREADED = 1;

	internal static readonly Guid IID_IUnknown = new("00000000-0000-0000-c000-000000000046");
	internal static readonly Guid IID_IAgileObject = new("94ea2b94-e9cc-49e0-c0ff-ee64ca8f5b90");
	private static readonly Guid IID_IAsyncInfo = new("00000036-0000-0000-c000-000000000046");
	private static readonly Guid IID_IClosable = new("30d5a829-7fa4-4026-83bb-d75bae4ea99e");

	// IAsyncOperation<T> / IAsyncAction: put_Completed, get_Completed, GetResults.
	private const int Slot_IAsync_put_Completed = 6;
	private const int Slot_IAsync_GetResults = 8;

	// IAsyncInfo: get_Id, get_Status, get_ErrorCode, Cancel, Close.
	private const int Slot_IAsyncInfo_get_ErrorCode = 8;
	private const int Slot_IAsyncInfo_Cancel = 9;

	// IClosable: Close.
	private const int Slot_IClosable_Close = 6;

	private enum AsyncStatus
	{
		Started = 0,
		Completed = 1,
		Canceled = 2,
		Error = 3,
	}

	[LibraryImport("combase.dll")]
	private static partial int RoGetActivationFactory(nint activatableClassId, Guid* iid, nint* factory);

	[LibraryImport("combase.dll")]
	private static partial int RoInitialize(int initType);

	[LibraryImport("combase.dll")]
	private static partial int WindowsCreateString(char* sourceString, uint length, nint* hstring);

	[LibraryImport("combase.dll")]
	private static partial int WindowsDeleteString(nint hstring);

	[LibraryImport("combase.dll")]
	private static partial char* WindowsGetStringRawBuffer(nint hstring, uint* length);

	internal static nint GetActivationFactory(string activatableClassId, Guid iid)
	{
		var classId = CreateHString(activatableClassId);
		try
		{
			nint factory;
			var hr = RoGetActivationFactory(classId, &iid, &factory);
			if (hr == CO_E_NOTINITIALIZED)
			{
				// The UI thread is initialized by the host; other threads may never have entered COM.
				var initHr = RoInitialize(RO_INIT_MULTITHREADED);
				if (initHr < 0)
				{
					Marshal.ThrowExceptionForHR(initHr);
				}

				hr = RoGetActivationFactory(classId, &iid, &factory);
			}

			Marshal.ThrowExceptionForHR(hr);
			return factory;
		}
		finally
		{
			_ = WindowsDeleteString(classId);
		}
	}

	internal static nint CreateHString(string value)
	{
		fixed (char* chars = value)
		{
			nint hstring;
			Marshal.ThrowExceptionForHR(WindowsCreateString(chars, (uint)value.Length, &hstring));
			return hstring;
		}
	}

	internal static void DeleteHString(nint hstring) => _ = WindowsDeleteString(hstring);

	// Reads an HSTRING returned by a WinRT method and releases it.
	private static string ConsumeHString(nint hstring)
	{
		if (hstring == 0)
		{
			return string.Empty;
		}

		try
		{
			uint length;
			var buffer = WindowsGetStringRawBuffer(hstring, &length);
			return new string(buffer, 0, (int)length);
		}
		finally
		{
			_ = WindowsDeleteString(hstring);
		}
	}

	private static void** Vtbl(nint obj) => *(void***)obj;

	internal static uint AddRef(nint obj) => ((delegate* unmanaged[Stdcall]<nint, uint>)Vtbl(obj)[1])(obj);

	internal static void Release(nint obj)
	{
		if (obj != 0)
		{
			((delegate* unmanaged[Stdcall]<nint, uint>)Vtbl(obj)[2])(obj);
		}
	}

	internal static nint QueryInterface(nint obj, Guid iid)
	{
		nint result;
		Marshal.ThrowExceptionForHR(((delegate* unmanaged[Stdcall]<nint, Guid*, nint*, int>)Vtbl(obj)[0])(obj, &iid, &result));
		return result;
	}

	// Method with no arguments returning HRESULT only (e.g. Cancel, Close, IAsyncAction.GetResults).
	internal static void Invoke(nint obj, int slot)
		=> Marshal.ThrowExceptionForHR(((delegate* unmanaged[Stdcall]<nint, int>)Vtbl(obj)[slot])(obj));

	// Getter or method returning an interface pointer.
	internal static nint GetPointer(nint obj, int slot)
	{
		nint result;
		Marshal.ThrowExceptionForHR(((delegate* unmanaged[Stdcall]<nint, nint*, int>)Vtbl(obj)[slot])(obj, &result));
		return result;
	}

	// Method taking one pointer-sized argument (interface pointer or HSTRING) and returning an interface pointer.
	internal static nint GetPointer(nint obj, int slot, nint argument)
	{
		nint result;
		Marshal.ThrowExceptionForHR(((delegate* unmanaged[Stdcall]<nint, nint, nint*, int>)Vtbl(obj)[slot])(obj, argument, &result));
		return result;
	}

	// Method taking a UINT32 argument and returning an interface pointer (e.g. GetAt, GetAlternates).
	internal static nint GetPointer(nint obj, int slot, uint argument)
	{
		nint result;
		Marshal.ThrowExceptionForHR(((delegate* unmanaged[Stdcall]<nint, uint, nint*, int>)Vtbl(obj)[slot])(obj, argument, &result));
		return result;
	}

	// Getter returning a 32-bit value (INT32, UINT32 or an enum).
	internal static int GetInt32(nint obj, int slot)
	{
		int result;
		Marshal.ThrowExceptionForHR(((delegate* unmanaged[Stdcall]<nint, int*, int>)Vtbl(obj)[slot])(obj, &result));
		return result;
	}

	internal static string GetString(nint obj, int slot)
	{
		nint hstring;
		Marshal.ThrowExceptionForHR(((delegate* unmanaged[Stdcall]<nint, nint*, int>)Vtbl(obj)[slot])(obj, &hstring));
		return ConsumeHString(hstring);
	}

	// Setter taking a 64-bit value; Windows.Foundation.TimeSpan is a single INT64 of 100ns ticks.
	internal static void SetInt64(nint obj, int slot, long value)
		=> Marshal.ThrowExceptionForHR(((delegate* unmanaged[Stdcall]<nint, long, int>)Vtbl(obj)[slot])(obj, value));

	internal static long AddEventHandler(nint obj, int slot, nint handler)
	{
		long token;
		Marshal.ThrowExceptionForHR(((delegate* unmanaged[Stdcall]<nint, nint, long*, int>)Vtbl(obj)[slot])(obj, handler, &token));
		return token;
	}

	internal static void RemoveEventHandler(nint obj, int slot, long token)
		=> Marshal.ThrowExceptionForHR(((delegate* unmanaged[Stdcall]<nint, long, int>)Vtbl(obj)[slot])(obj, token));

	internal static void Close(nint obj)
	{
		var closable = QueryInterface(obj, IID_IClosable);
		try
		{
			Invoke(closable, Slot_IClosable_Close);
		}
		finally
		{
			Release(closable);
		}
	}

	internal static void Cancel(nint asyncOperation)
	{
		var asyncInfo = QueryInterface(asyncOperation, IID_IAsyncInfo);
		try
		{
			Invoke(asyncInfo, Slot_IAsyncInfo_Cancel);
		}
		finally
		{
			Release(asyncInfo);
		}
	}

	/// <summary>
	/// Awaits a WinRT IAsyncOperation&lt;T&gt; (when <paramref name="hasResults"/> is true) or IAsyncAction.
	/// </summary>
	/// <param name="completedHandlerIid">IID of the operation's completed handler delegate.</param>
	/// <returns>The owned result interface pointer for an operation, or 0 for an action. The caller keeps ownership of <paramref name="asyncOperation"/>.</returns>
	internal static Task<nint> AwaitAsync(nint asyncOperation, Guid completedHandlerIid, bool hasResults)
	{
		var completion = new TaskCompletionSource<nint>(TaskCreationOptions.RunContinuationsAsynchronously);

		var handler = WinRTDelegate.CreateCompletedHandler(completedHandlerIid, (operation, status) =>
		{
			try
			{
				switch ((AsyncStatus)status)
				{
					case AsyncStatus.Completed when hasResults:
						completion.TrySetResult(GetPointer(operation, Slot_IAsync_GetResults));
						break;

					case AsyncStatus.Completed:
						Invoke(operation, Slot_IAsync_GetResults);
						completion.TrySetResult(0);
						break;

					case AsyncStatus.Canceled:
						completion.TrySetCanceled();
						break;

					default:
						completion.TrySetException(GetAsyncError(operation));
						break;
				}
			}
			catch (Exception e)
			{
				completion.TrySetException(e);
			}
		});

		try
		{
			// The operation keeps its own reference to the handler; it may run synchronously if already complete.
			Marshal.ThrowExceptionForHR(((delegate* unmanaged[Stdcall]<nint, nint, int>)Vtbl(asyncOperation)[Slot_IAsync_put_Completed])(asyncOperation, handler));
		}
		finally
		{
			Release(handler);
		}

		return completion.Task;
	}

	private static Exception GetAsyncError(nint asyncOperation)
	{
		var asyncInfo = QueryInterface(asyncOperation, IID_IAsyncInfo);
		try
		{
			var hr = GetInt32(asyncInfo, Slot_IAsyncInfo_get_ErrorCode);
			return Marshal.GetExceptionForHR(hr) ?? new InvalidOperationException($"The WinRT operation failed with 0x{hr:X8}.");
		}
		finally
		{
			Release(asyncInfo);
		}
	}
}
