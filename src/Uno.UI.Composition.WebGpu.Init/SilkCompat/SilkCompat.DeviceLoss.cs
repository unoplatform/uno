#nullable disable
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using SW = Silk.NET.WebGPU;

namespace Uno.UI.Composition.WebGpu.SilkCompat;

// wgpu-native panics on an error from the calls below, and a panic can't cross into managed code: it aborts the
// process. So once the device is lost, none of them may reach it. Its lost callback doesn't help on its own: it fires
// only when a call that reports errors runs into the loss, never when the loss happens.
internal static unsafe partial class Xlate
{
	// One device per process: see WebGpuInitDevice.GetShared. Stays null on the browser, which imports its device and
	// whose implementation reports errors instead of panicking.
	private static nint _device;
	private static int _deviceLost;
	private static int _pendingMaps;

	/// <summary>Held across a submission, and by a present that needs the queue to stay idle while it acquires.</summary>
	internal static readonly object SubmissionGate = new();

	/// <summary>
	/// Whether the device is lost, asking wgpu rather than waiting to be told. Creating an encoder is the cheapest call
	/// that reports a loss, and it does so synchronously through the lost callback.
	/// </summary>
	internal static bool IsDeviceLost
	{
		get
		{
			if (Volatile.Read(ref _deviceLost) != 0)
			{
				return true;
			}

			if (_device != 0)
			{
				WGPU.wgpuCommandEncoderRelease(WGPU.wgpuDeviceCreateCommandEncoder(_device, null));
			}

			return Volatile.Read(ref _deviceLost) != 0;
		}
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static void GuardedQueueSubmit(nint queue, nuint commandCount, nint commands)
	{
		lock (SubmissionGate)
		{
			if (!IsDeviceLost)
			{
				WGPU.wgpuQueueSubmit(queue, commandCount, commands);
			}
		}
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static ulong GuardedQueueSubmitForIndex(nint queue, nuint commandCount, nint commands)
	{
		lock (SubmissionGate)
		{
			return IsDeviceLost ? 0 : WGPU.wgpuQueueSubmitForIndex(queue, commandCount, commands);
		}
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static void GuardedSurfaceConfigure(nint surface, nint config)
	{
		if (!IsDeviceLost)
		{
			((delegate* unmanaged[Cdecl]<nint, nint, void>)&wgpuSurfaceConfigure)(surface, config);
		}
	}

	// A map that failed because of the loss leaves nothing to return, and asking for its range panics.
	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static nint GuardedBufferGetMappedRange(nint buffer, nuint offset, nuint size)
		=> Volatile.Read(ref _deviceLost) != 0 && (!_mapStates.TryGetValue(buffer, out var state) || state != SW.BufferMapState.Mapped)
			? 0
			: WGPU.wgpuBufferGetMappedRange(buffer, offset, size);

	private static void AddDeviceLossGuards(Dictionary<string, nint> t)
	{
		if (OperatingSystem.IsBrowser())
		{
			return;
		}

		t["wgpuQueueSubmit"] = (nint)(delegate* unmanaged[Cdecl]<nint, nuint, nint, void>)&GuardedQueueSubmit;
		t["wgpuQueueSubmitForIndex"] = (nint)(delegate* unmanaged[Cdecl]<nint, nuint, nint, ulong>)&GuardedQueueSubmitForIndex;
		t["wgpuSurfaceConfigure"] = (nint)(delegate* unmanaged[Cdecl]<nint, nint, void>)&GuardedSurfaceConfigure;
		t["wgpuBufferGetMappedRange"] = (nint)(delegate* unmanaged[Cdecl]<nint, nuint, nuint, nint>)&GuardedBufferGetMappedRange;
	}
}
