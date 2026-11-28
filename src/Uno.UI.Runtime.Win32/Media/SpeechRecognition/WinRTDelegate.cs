#nullable enable

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Uno.Foundation.Logging;

namespace Uno.UI.Runtime.Win32;

// A COM-callable WinRT delegate (IUnknown + Invoke) backed by a managed callback. WinRT async
// operations and event sources only query IUnknown, IAgileObject and the delegate's own IID.
// Being agile, Invoke runs on whichever thread the source completes or raises on.
internal static unsafe class WinRTDelegate
{
	[StructLayout(LayoutKind.Sequential)]
	private struct Native
	{
		public void** Vtbl;
		public int RefCount;
		public nint State;
	}

	private sealed class State(Guid iid, Delegate callback)
	{
		public Guid Iid { get; } = iid;

		public Delegate Callback { get; } = callback;
	}

	// Process-lifetime vtables shared by every instance of each shape.
	private static readonly void** _completedHandlerVtbl = CreateVtbl((delegate* unmanaged[Stdcall]<Native*, nint, int, int>)&InvokeCompleted);
	private static readonly void** _eventHandlerVtbl = CreateVtbl((delegate* unmanaged[Stdcall]<Native*, nint, nint, int>)&InvokeEvent);

	/// <summary>
	/// Creates an AsyncOperationCompletedHandler&lt;T&gt; or AsyncActionCompletedHandler, invoked with (asyncInfo, AsyncStatus).
	/// </summary>
	/// <returns>An owned reference; release it once handed to WinRT.</returns>
	internal static nint CreateCompletedHandler(Guid iid, Action<nint, int> callback) => Create(_completedHandlerVtbl, iid, callback);

	/// <summary>
	/// Creates a TypedEventHandler&lt;TSender, TArgs&gt;, invoked with (sender, args).
	/// </summary>
	/// <returns>An owned reference; release it once handed to WinRT.</returns>
	internal static nint CreateEventHandler(Guid iid, Action<nint, nint> callback) => Create(_eventHandlerVtbl, iid, callback);

	private static void** CreateVtbl(void* invoke)
	{
		var vtbl = (void**)NativeMemory.Alloc((nuint)(4 * sizeof(void*)));
		vtbl[0] = (delegate* unmanaged[Stdcall]<Native*, Guid*, void**, int>)&QueryInterface;
		vtbl[1] = (delegate* unmanaged[Stdcall]<Native*, uint>)&AddRef;
		vtbl[2] = (delegate* unmanaged[Stdcall]<Native*, uint>)&Release;
		vtbl[3] = invoke;
		return vtbl;
	}

	private static nint Create(void** vtbl, Guid iid, Delegate callback)
	{
		var native = (Native*)NativeMemory.Alloc((nuint)sizeof(Native));
		native->Vtbl = vtbl;
		native->RefCount = 1;
		native->State = GCHandle.ToIntPtr(GCHandle.Alloc(new State(iid, callback)));
		return (nint)native;
	}

	private static State GetState(Native* native) => (State)GCHandle.FromIntPtr(native->State).Target!;

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
	private static int QueryInterface(Native* @this, Guid* iid, void** result)
	{
		if (*iid == WinRTInterop.IID_IUnknown || *iid == WinRTInterop.IID_IAgileObject || *iid == GetState(@this).Iid)
		{
			Interlocked.Increment(ref @this->RefCount);
			*result = @this;
			return WinRTInterop.S_OK;
		}

		*result = null;
		return WinRTInterop.E_NOINTERFACE;
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
	private static uint AddRef(Native* @this) => (uint)Interlocked.Increment(ref @this->RefCount);

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
	private static uint Release(Native* @this)
	{
		var count = Interlocked.Decrement(ref @this->RefCount);
		if (count == 0)
		{
			GCHandle.FromIntPtr(@this->State).Free();
			NativeMemory.Free(@this);
		}

		return (uint)count;
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
	private static int InvokeCompleted(Native* @this, nint asyncInfo, int status)
	{
		try
		{
			((Action<nint, int>)GetState(@this).Callback)(asyncInfo, status);
			return WinRTInterop.S_OK;
		}
		catch (Exception e)
		{
			typeof(WinRTDelegate).LogError()?.Error("WinRT completion handler failed.", e);
			return e.HResult;
		}
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
	private static int InvokeEvent(Native* @this, nint sender, nint args)
	{
		try
		{
			((Action<nint, nint>)GetState(@this).Callback)(sender, args);
			return WinRTInterop.S_OK;
		}
		catch (Exception e)
		{
			typeof(WinRTDelegate).LogError()?.Error("WinRT event handler failed.", e);
			return e.HResult;
		}
	}
}
