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
	private static WGPUBackendType DawnBackend => WebGpuLoader.IsDawn ? _dawnBackend.Value : WGPUBackendType.Undefined;

	private static readonly Lazy<WGPUBackendType> _dawnBackend = new(() =>
		(Environment.GetEnvironmentVariable("UNO_WEBGPU_BACKENDS") ?? "").Split(',')[0].Trim().ToLowerInvariant() switch
		{
			"vulkan" => WGPUBackendType.Vulkan,
			"dx12" => WGPUBackendType.D3D12,
			"dx11" => WGPUBackendType.D3D11,
			"metal" => WGPUBackendType.Metal,
			"gl" => WGPUBackendType.OpenGLES,
			_ => WGPUBackendType.Undefined,
		});

	private static IntPtr _dawnInstance;
	private static long _dawnSubmissions;
	private static readonly ConcurrentDictionary<ulong, ulong> _dawnPending = new();

	// dawn::native::DawnInstanceDescriptor (DawnNative.h), the C++ struct chained onto instance creation.
	[StructLayout(LayoutKind.Sequential)]
	private struct DawnInstanceDescriptor
	{
		public WGPUChainedStruct Chain;
		public uint AdditionalRuntimeSearchPathsCount;
		public IntPtr AdditionalRuntimeSearchPaths;
		public IntPtr Platform;
		public int BackendValidationLevel;
		public byte BeginCaptureOnStartup;
		public IntPtr LoggingNext, LoggingCallback, LoggingUserdata1, LoggingUserdata2;
	}

	private const WGPUSType DawnInstanceDescriptorSType = (WGPUSType)0x00050006;
	private const WGPUSType DawnTogglesDescriptorSType = (WGPUSType)0x0005000A;

	// WGPUDawnTogglesDescriptor (dawn/webgpu.h), chainable onto an adapter request.
	[StructLayout(LayoutKind.Sequential)]
	private struct WGPUDawnTogglesDescriptor
	{
		public WGPUChainedStruct Chain;
		public nuint EnabledToggleCount;
		public IntPtr EnabledToggles;
		public nuint DisabledToggleCount;
		public IntPtr DisabledToggles;
	}

	// On D3D12 Dawn compiles shaders with DXC unless told otherwise, and DXC is not part of Windows. FXC is
	// (d3dcompiler_47.dll, found through the search path above), and it is what wgpu-native uses there too.
	private static readonly IntPtr _dawnDisabledToggles = DawnToggleList("use_dxc");

	private static IntPtr DawnToggleList(params string[] names)
	{
		var list = Marshal.AllocHGlobal(IntPtr.Size * names.Length);
		for (var i = 0; i < names.Length; i++)
		{
			Marshal.WriteIntPtr(list, i * IntPtr.Size, Marshal.StringToCoTaskMemUTF8(names[i]));
		}
		return list;
	}
	private const int DawnBackendValidationDisabled = 2;

	// Dawn opens its shader compilers and the Vulkan loader from its own folder and these paths only, never through the
	// system's search order. Windows ships d3dcompiler_47.dll and vulkan-1.dll in System32, so Dawn finds them there
	// instead of the app shipping its own. Kept for the process: Dawn may hold on to the strings.
	private static readonly IntPtr _dawnSearchPaths = DawnSearchPaths();

	private static IntPtr DawnSearchPaths()
	{
		if (!OperatingSystem.IsWindows())
		{
			return IntPtr.Zero;
		}
		var paths = Marshal.AllocHGlobal(IntPtr.Size);
		Marshal.WriteIntPtr(paths, Marshal.StringToCoTaskMemUTF8(Environment.SystemDirectory + System.IO.Path.DirectorySeparatorChar));
		return paths;
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static nint DawnCreateInstance(nint descriptor)
	{
		var dawn = new DawnInstanceDescriptor
		{
			Chain = new WGPUChainedStruct { SType = DawnInstanceDescriptorSType },
			AdditionalRuntimeSearchPathsCount = _dawnSearchPaths == IntPtr.Zero ? 0u : 1u,
			AdditionalRuntimeSearchPaths = _dawnSearchPaths,
			BackendValidationLevel = DawnBackendValidationDisabled,
		};
		// Waiting on a future with a timeout, which the blocking poll does, is an instance feature on Dawn.
		var feature = WGPUInstanceFeatureName.TimedWaitAny;
		var d = new WGPUInstanceDescriptor { NextInChain = (WGPUChainedStruct*)&dawn, RequiredFeatureCount = 1, RequiredFeatures = &feature };
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
