#nullable disable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using SX = Silk.NET.WebGPU.Extensions.WGPU;

namespace Uno.UI.Composition.WebGpu.SilkCompat;

// On Dawn, the wgpu-native functions the backend calls, built on the standard futures: a submission index is the
// future of its queue's work-done event, and a blocking poll waits on it.
internal static unsafe partial class Xlate
{
	// WGPUFeatureName_ImplicitDeviceSynchronization, Dawn's own: the device locks itself around every call.
	private const uint DawnImplicitDeviceSynchronization = 0x00050004;

	// UNO_WEBGPU_BACKENDS picks the backend on Dawn too, through the adapter request rather than wgpu-native's
	// instance extras (Dawn rejects those).
	private static readonly WGPUBackendType DawnBackend = !WebGpuLoader.IsDawn ? WGPUBackendType.Undefined
		: (Environment.GetEnvironmentVariable("UNO_WEBGPU_BACKENDS") ?? "").Split(',')[0].Trim().ToLowerInvariant() switch
		{
			"vulkan" => WGPUBackendType.Vulkan,
			"dx12" => WGPUBackendType.D3D12,
			"dx11" => WGPUBackendType.D3D11,
			"metal" => WGPUBackendType.Metal,
			"gl" => WGPUBackendType.OpenGLES,
			_ => WGPUBackendType.Undefined,
		};

	private static IntPtr _dawnInstance;
	private static long _dawnSubmissions;
	private static readonly ConcurrentDictionary<ulong, ulong> _dawnPending = new();

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static nint DawnCreateInstance(nint descriptor)
	{
		// Waiting on a future with a timeout, which the blocking poll does, is an instance feature on Dawn.
		var feature = WGPUInstanceFeatureName.TimedWaitAny;
		var d = new WGPUInstanceDescriptor { RequiredFeatureCount = 1, RequiredFeatures = &feature };
		return _dawnInstance = WGPU.wgpuCreateInstance(&d);
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static ulong DawnQueueSubmitForIndex(nint queue, nuint count, nint commands)
	{
		WGPU.wgpuQueueSubmit(queue, count, commands);
		var index = (ulong)Interlocked.Increment(ref _dawnSubmissions);
		var future = WGPU.wgpuQueueOnSubmittedWorkDone(queue, WorkDone((IntPtr)index));
		_dawnPending[index] = future.Id;
		return index;
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static uint DawnDevicePoll(nint device, uint wait, nint wrappedSubmissionIndex)
	{
		if (wait != 0)
		{
			ulong future;
			if (wrappedSubmissionIndex != 0)
			{
				// Already settled (and forgotten) when the event fired during an earlier poll.
				var index = ((SX.WrappedSubmissionIndex*)wrappedSubmissionIndex)->SubmissionIndex;
				future = _dawnPending.TryGetValue(index, out var id) ? id : 0;
			}
			else
			{
				// Everything submitted so far.
				var queue = WGPU.wgpuDeviceGetQueue(device);
				future = WGPU.wgpuQueueOnSubmittedWorkDone(queue, WorkDone(IntPtr.Zero)).Id;
				WGPU.wgpuQueueRelease(queue);
			}

			if (future != 0)
			{
				var info = new WGPUFutureWaitInfo { Future = new WGPUFuture { Id = future } };
				_ = WGPU.wgpuInstanceWaitAny(_dawnInstance, 1, &info, ulong.MaxValue);
			}
		}

		WGPU.wgpuInstanceProcessEvents(_dawnInstance);
		return 1;
	}

	private static WGPUQueueWorkDoneCallbackInfo WorkDone(IntPtr index) => new()
	{
		Mode = WGPUCallbackMode.AllowProcessEvents,
		Callback = (IntPtr)(delegate* unmanaged[Cdecl]<WGPUQueueWorkDoneStatus, WGPUStringView, IntPtr, IntPtr, void>)&OnDawnWorkDone,
		Userdata1 = index,
	};

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static void OnDawnWorkDone(WGPUQueueWorkDoneStatus status, WGPUStringView message, IntPtr index, IntPtr unused)
	{
		if (index != IntPtr.Zero)
		{
			_dawnPending.TryRemove((ulong)index, out _);
		}
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static void DawnSetLogCallback(nint callback, nint userdata)
	{
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static void DawnSetLogLevel(int level)
	{
	}

	private static void AddDawn(Dictionary<string, nint> t)
	{
		t["wgpuCreateInstance"] = (nint)(delegate* unmanaged[Cdecl]<nint, nint>)&DawnCreateInstance;
		t["wgpuQueueSubmitForIndex"] = (nint)(delegate* unmanaged[Cdecl]<nint, nuint, nint, ulong>)&DawnQueueSubmitForIndex;
		t["wgpuDevicePoll"] = (nint)(delegate* unmanaged[Cdecl]<nint, uint, nint, uint>)&DawnDevicePoll;
		t["wgpuSetLogCallback"] = (nint)(delegate* unmanaged[Cdecl]<nint, nint, void>)&DawnSetLogCallback;
		t["wgpuSetLogLevel"] = (nint)(delegate* unmanaged[Cdecl]<int, void>)&DawnSetLogLevel;
	}
}
