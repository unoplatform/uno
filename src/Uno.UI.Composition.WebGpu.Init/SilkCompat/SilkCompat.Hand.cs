#nullable disable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using SW = Silk.NET.WebGPU;
using SX = Silk.NET.WebGPU.Extensions.WGPU;

namespace Uno.UI.Composition.WebGpu.SilkCompat;

// The translations a rule can't derive: callbacks (the old (fn, userdata) pair becomes a CallbackInfo whose trampoline
// converts the message and status back), output structs, and descriptors the stable header reshaped.
internal static unsafe partial class Xlate
{
	// WGPU_STRLEN: the string is null-terminated, which is what every old char* is.
	internal static WGPUStringView Str(byte* text) => new() { Data = (IntPtr)text, Length = nuint.MaxValue };

	// Old callbacks take a null-terminated char*; a StringView needn't be terminated.
	private static IntPtr CString(WGPUStringView view)
	{
		if (view.Data == IntPtr.Zero)
		{
			return IntPtr.Zero;
		}

		var length = view.Length == nuint.MaxValue ? (nuint)Marshal.PtrToStringUTF8(view.Data)!.Length : view.Length;
		var copy = (byte*)NativeMemory.Alloc(length + 1);
		Buffer.MemoryCopy((void*)view.Data, copy, (long)length, (long)length);
		copy[length] = 0;
		return (IntPtr)copy;
	}

	private static void Free(IntPtr text) => NativeMemory.Free((void*)text);

	private static IntPtr Strdup(WGPUStringView view) => CString(view);

	// The old API fired callbacks as soon as they could; AllowSpontaneous is its stable-header equivalent.
	private const WGPUCallbackMode Spontaneous = WGPUCallbackMode.AllowSpontaneous;

	// ---------------------------------------------------------------- chained structs the generator leaves to us

	internal static WGPUChainedStruct* ChainHand(SW.ChainedStruct* s, ref Arena a)
	{
		var old = (int)s->SType;
		if (old == (int)SW.SType.PrimitiveDepthClipControl)
		{
			// Folded into PrimitiveState.UnclippedDepth by its Fill.
			return Chain(s->Next, ref a);
		}

		if (old == (int)SX.NativeSType.STypeInstanceExtras)
		{
			var d = (WGPUInstanceExtras*)a.Alloc(sizeof(WGPUInstanceExtras));
			Fill((SX.InstanceExtras*)s, d, ref a);
			return (WGPUChainedStruct*)d;
		}

		if (old == (int)SW.SType.SurfaceDescriptorFromCanvasHtmlSelector)
		{
			// The browser's canvas surface source is Dawn's extension in the stable header (emdawnwebgpu).
			var canvas = (SW.SurfaceDescriptorFromCanvasHTMLSelector*)s;
			var d = (WGPUEmscriptenSurfaceSourceCanvasHTMLSelector*)a.Alloc(sizeof(WGPUEmscriptenSurfaceSourceCanvasHTMLSelector));
			d->Chain.Next = (IntPtr)Chain(s->Next, ref a);
			d->Chain.SType = WGPU.SType_EmscriptenSurfaceSourceCanvasHTMLSelector;
			d->Selector = Str(canvas->Selector);
			return (WGPUChainedStruct*)d;
		}

		throw new NotSupportedException($"Silk.NET compatibility: chained struct with SType 0x{old:x} is not translated.");
	}

	internal static void Fill(SW.PrimitiveState* s, WGPUPrimitiveState* d, ref Arena a)
	{
		for (var c = s->NextInChain; c != null; c = c->Next)
		{
			if (c->SType == SW.SType.PrimitiveDepthClipControl)
			{
				d->UnclippedDepth = ((SW.PrimitiveDepthClipControl*)c)->UnclippedDepth ? 1u : 0u;
			}
		}

		d->NextInChain = Chain(s->NextInChain, ref a);
		d->Topology = (WGPUPrimitiveTopology)O2N_PrimitiveTopology((int)s->Topology);
		d->StripIndexFormat = (WGPUIndexFormat)(int)s->StripIndexFormat;
		d->FrontFace = (WGPUFrontFace)O2N_FrontFace((int)s->FrontFace);
		d->CullMode = (WGPUCullMode)O2N_CullMode((int)s->CullMode);
	}

	private static void Fill(SX.InstanceExtras* s, WGPUInstanceExtras* d, ref Arena a)
	{
		d->Chain.Next = (IntPtr)Chain(s->Chain.Next, ref a);
		d->Chain.SType = (WGPUSType)(int)WGPUNativeSType.WGPUSType_InstanceExtras;
		// Same bits in both headers, except DX11 (1 << 4), which the stable header dropped.
		const uint Dx11 = 1u << 4;
		d->Backends = (WGPUInstanceBackend)(ulong)((uint)s->Backends & ~Dx11);
		d->Flags = (WGPUInstanceFlag)(ulong)(uint)O2N_InstanceFlag((int)s->Flags);
		d->Dx12ShaderCompiler = (WGPUDx12Compiler)(int)s->Dx12ShaderCompiler;
		d->Gles3MinorVersion = (WGPUGles3MinorVersion)(int)s->Gles3MinorVersion;
		d->DxcPath = Str(s->DxcPath);
	}

	// ---------------------------------------------------------------- adapter and device

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static void wgpuInstanceRequestAdapter(nint instance, nint options, nint callback, nint userdata)
	{
		var a = Arena.Rent();
		WGPURequestAdapterOptions* o = null;
		if (options != 0)
		{
			o = (WGPURequestAdapterOptions*)a.Alloc(sizeof(WGPURequestAdapterOptions));
			Fill((SW.RequestAdapterOptions*)options, o, ref a);
		}
		var info = new WGPURequestAdapterCallbackInfo
		{
			Mode = Spontaneous,
			Callback = (IntPtr)(delegate* unmanaged[Cdecl]<WGPURequestAdapterStatus, IntPtr, WGPUStringView, IntPtr, IntPtr, void>)&OnAdapter,
			Userdata1 = callback,
			Userdata2 = userdata,
		};
		WGPU.wgpuInstanceRequestAdapter(instance, o, info);
		a.Dispose();
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static void OnAdapter(WGPURequestAdapterStatus status, IntPtr adapter, WGPUStringView message, IntPtr callback, IntPtr userdata)
	{
		var text = CString(message);
		((delegate* unmanaged[Cdecl]<int, IntPtr, IntPtr, IntPtr, void>)callback)(N2O_RequestAdapterStatus((int)status), adapter, text, userdata);
		Free(text);
	}

	// Silk users register uncaptured-error handlers after creation; the stable header only takes one at creation.
	private static readonly ConcurrentDictionary<IntPtr, (IntPtr Callback, IntPtr Userdata)> _errorHandlers = new();

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static void wgpuAdapterRequestDevice(nint adapter, nint descriptor, nint callback, nint userdata)
	{
		var a = Arena.Rent();
		var d = (WGPUDeviceDescriptor*)a.Alloc(sizeof(WGPUDeviceDescriptor));
		var lost = new WGPUDeviceLostCallbackInfo
		{
			Mode = Spontaneous,
			Callback = (IntPtr)(delegate* unmanaged[Cdecl]<IntPtr*, WGPUDeviceLostReason, WGPUStringView, IntPtr, IntPtr, void>)&OnDeviceLost,
		};
		if (descriptor != 0)
		{
			var s = (SW.DeviceDescriptor*)descriptor;
			d->NextInChain = Chain(s->NextInChain, ref a);
			d->Label = Str(s->Label);
			d->RequiredFeatureCount = s->RequiredFeatureCount;
			if (s->RequiredFeatures != null)
			{
				var features = (WGPUFeatureName*)a.Alloc(sizeof(WGPUFeatureName) * (int)s->RequiredFeatureCount);
				for (var i = 0; i < (int)s->RequiredFeatureCount; i++)
				{
					features[i] = (WGPUFeatureName)O2N_FeatureName((int)s->RequiredFeatures[i]);
				}
				d->RequiredFeatures = features;
			}
			if (s->RequiredLimits != null)
			{
				var limits = (WGPULimits*)a.Alloc(sizeof(WGPULimits));
				Fill(&s->RequiredLimits->Limits, limits, ref a);
				d->RequiredLimits = limits;
			}
			d->DefaultQueue.NextInChain = Chain(s->DefaultQueue.NextInChain, ref a);
			d->DefaultQueue.Label = Str(s->DefaultQueue.Label);
			lost.Userdata1 = (IntPtr)s->DeviceLostCallback.Handle;
			lost.Userdata2 = (IntPtr)s->DeviceLostUserdata;
		}
		d->DeviceLostCallbackInfo = lost;
		d->UncapturedErrorCallbackInfo = new WGPUUncapturedErrorCallbackInfo
		{
			Callback = (IntPtr)(delegate* unmanaged[Cdecl]<IntPtr*, WGPUErrorType, WGPUStringView, IntPtr, IntPtr, void>)&OnUncapturedError,
		};
		var info = new WGPURequestDeviceCallbackInfo
		{
			Mode = Spontaneous,
			Callback = (IntPtr)(delegate* unmanaged[Cdecl]<WGPURequestDeviceStatus, IntPtr, WGPUStringView, IntPtr, IntPtr, void>)&OnDevice,
			Userdata1 = callback,
			Userdata2 = userdata,
		};
		WGPU.wgpuAdapterRequestDevice(adapter, d, info);
		a.Dispose();
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static void OnDevice(WGPURequestDeviceStatus status, IntPtr device, WGPUStringView message, IntPtr callback, IntPtr userdata)
	{
		var text = CString(message);
		((delegate* unmanaged[Cdecl]<int, IntPtr, IntPtr, IntPtr, void>)callback)(N2O_RequestDeviceStatus((int)status), device, text, userdata);
		Free(text);
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static void OnDeviceLost(IntPtr* device, WGPUDeviceLostReason reason, WGPUStringView message, IntPtr callback, IntPtr userdata)
	{
		if (callback == IntPtr.Zero)
		{
			return;
		}
		var text = CString(message);
		((delegate* unmanaged[Cdecl]<int, IntPtr, IntPtr, void>)callback)(N2O_DeviceLostReason((int)reason), text, userdata);
		Free(text);
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static void OnUncapturedError(IntPtr* device, WGPUErrorType type, WGPUStringView message, IntPtr u1, IntPtr u2)
	{
		if (device == null || !_errorHandlers.TryGetValue(*device, out var handler))
		{
			return;
		}
		var text = CString(message);
		((delegate* unmanaged[Cdecl]<int, IntPtr, IntPtr, void>)handler.Callback)(N2O_ErrorType((int)type), text, handler.Userdata);
		Free(text);
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static void wgpuDeviceSetUncapturedErrorCallback(nint device, nint callback, nint userdata)
	{
		if (callback == 0)
		{
			_errorHandlers.TryRemove(device, out _);
		}
		else
		{
			_errorHandlers[device] = (callback, userdata);
		}
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static void wgpuAdapterGetProperties(nint adapter, nint properties)
	{
		WGPUAdapterInfo info = default;
		var p = (SW.AdapterProperties*)properties;
		*p = default;
		if (WGPU.wgpuAdapterGetInfo(adapter, &info) != WGPUStatus.Success)
		{
			return;
		}
		p->VendorID = info.VendorID;
		p->DeviceID = info.DeviceID;
		p->VendorName = (byte*)Strdup(info.Vendor);
		p->Architecture = (byte*)Strdup(info.Architecture);
		p->Name = (byte*)Strdup(info.Device);
		p->DriverDescription = (byte*)Strdup(info.Description);
		p->AdapterType = (SW.AdapterType)N2O_AdapterType((int)info.AdapterType);
		p->BackendType = (SW.BackendType)(int)info.BackendType;
		WGPU.wgpuAdapterInfoFreeMembers(info);
	}

	// ---------------------------------------------------------------- buffers

	// What a map request's callback needs: the old callback and userdata, and the buffer whose map state it settles.
	private struct MapRequest
	{
		public IntPtr Callback;
		public IntPtr Userdata;
		public IntPtr Buffer;
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static void wgpuBufferMapAsync(nint buffer, uint mode, nuint offset, nuint size, nint callback, nint userdata)
	{
		var request = (MapRequest*)NativeMemory.Alloc((nuint)sizeof(MapRequest));
		*request = new MapRequest { Callback = callback, Userdata = userdata, Buffer = buffer };
		var info = new WGPUBufferMapCallbackInfo
		{
			Mode = Spontaneous,
			Callback = (IntPtr)(delegate* unmanaged[Cdecl]<WGPUMapAsyncStatus, WGPUStringView, IntPtr, IntPtr, void>)&OnMapped,
			Userdata1 = (IntPtr)request,
		};
		SetMapState(buffer, SW.BufferMapState.Pending);
		WGPU.wgpuBufferMapAsync(buffer, (WGPUMapMode)(ulong)mode, offset, size, info);
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static void OnMapped(WGPUMapAsyncStatus status, WGPUStringView message, IntPtr requestPointer, IntPtr unused)
	{
		var request = *(MapRequest*)requestPointer;
		NativeMemory.Free((void*)requestPointer);
		// Settled before the callback runs, which may read it.
		SetMapState(request.Buffer, status == WGPUMapAsyncStatus.Success ? SW.BufferMapState.Mapped : SW.BufferMapState.Unmapped);
		// The old BufferMapAsyncStatus: Success = 0, ValidationError = 1, Unknown = 2.
		var old = status == WGPUMapAsyncStatus.Success ? 0 : status == WGPUMapAsyncStatus.Error ? 1 : 2;
		((delegate* unmanaged[Cdecl]<int, IntPtr, void>)request.Callback)(old, request.Userdata);
	}

	// ---------------------------------------------------------------- surfaces

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static void wgpuSurfaceGetCapabilities(nint surface, nint adapter, nint capabilities)
	{
		var c = (SW.SurfaceCapabilities*)capabilities;
		WGPUSurfaceCapabilities caps = default;
		*c = default;
		if (WGPU.wgpuSurfaceGetCapabilities(surface, adapter, &caps) != WGPUStatus.Success)
		{
			return;
		}

		// Old-numbered copies, owned by us until wgpuSurfaceCapabilitiesFreeMembers.
		c->FormatCount = caps.FormatCount;
		c->Formats = (SW.TextureFormat*)NativeMemory.Alloc(caps.FormatCount + 1, sizeof(int));
		for (nuint i = 0; i < caps.FormatCount; i++) { c->Formats[i] = (SW.TextureFormat)N2O_TextureFormat((int)caps.Formats[i]); }
		c->PresentModeCount = caps.PresentModeCount;
		c->PresentModes = (SW.PresentMode*)NativeMemory.Alloc(caps.PresentModeCount + 1, sizeof(int));
		for (nuint i = 0; i < caps.PresentModeCount; i++) { c->PresentModes[i] = (SW.PresentMode)N2O_PresentMode((int)caps.PresentModes[i]); }
		c->AlphaModeCount = caps.AlphaModeCount;
		c->AlphaModes = (SW.CompositeAlphaMode*)NativeMemory.Alloc(caps.AlphaModeCount + 1, sizeof(int));
		for (nuint i = 0; i < caps.AlphaModeCount; i++) { c->AlphaModes[i] = (SW.CompositeAlphaMode)(int)caps.AlphaModes[i]; }
		WGPU.wgpuSurfaceCapabilitiesFreeMembers(caps);
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static void wgpuSurfaceCapabilitiesFreeMembers(SW.SurfaceCapabilities capabilities)
	{
		NativeMemory.Free(capabilities.Formats);
		NativeMemory.Free(capabilities.PresentModes);
		NativeMemory.Free(capabilities.AlphaModes);
	}

	// WGPUSurfaceGetCurrentTextureStatus_Occluded, a wgpu-native extension (ffi/wgpu.h) the generated header lacks. It is
	// passed through under the same value, which the Silk.NET enum has no name for.
	private const WGPUSurfaceGetCurrentTextureStatus WgpuSurfaceStatusOccluded = (WGPUSurfaceGetCurrentTextureStatus)0x00030001;
	internal const SW.SurfaceGetCurrentTextureStatus SurfaceStatusOccluded = (SW.SurfaceGetCurrentTextureStatus)0x00030001;

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static void wgpuSurfaceGetCurrentTexture(nint surface, nint surfaceTexture)
	{
		WGPUSurfaceTexture st = default;
		WGPU.wgpuSurfaceGetCurrentTexture(surface, &st);
		var o = (SW.SurfaceTexture*)surfaceTexture;
		o->Texture = (SW.Texture*)st.Texture;
		o->Suboptimal = st.Status == WGPUSurfaceGetCurrentTextureStatus.SuccessSuboptimal;
		o->Status = st.Status switch
		{
			WGPUSurfaceGetCurrentTextureStatus.SuccessOptimal or WGPUSurfaceGetCurrentTextureStatus.SuccessSuboptimal => SW.SurfaceGetCurrentTextureStatus.Success,
			WGPUSurfaceGetCurrentTextureStatus.Timeout => SW.SurfaceGetCurrentTextureStatus.Timeout,
			WgpuSurfaceStatusOccluded => SurfaceStatusOccluded,
			WGPUSurfaceGetCurrentTextureStatus.Outdated => SW.SurfaceGetCurrentTextureStatus.Outdated,
			_ => SW.SurfaceGetCurrentTextureStatus.Lost,
		};
	}

	// ---------------------------------------------------------------- wgpu-native extras

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static uint wgpuDevicePoll(nint device, uint wait, nint wrappedSubmissionIndex)
	{
		var index = wrappedSubmissionIndex == 0 ? null : &((SX.WrappedSubmissionIndex*)wrappedSubmissionIndex)->SubmissionIndex;
		return WGPU.wgpuDevicePoll(device, wait, index);
	}

	private static IntPtr _logCallback, _logUserdata;

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static void wgpuSetLogCallback(nint callback, nint userdata)
	{
		_logCallback = callback;
		_logUserdata = userdata;
		WGPU.wgpuSetLogCallback(callback == 0 ? IntPtr.Zero : (IntPtr)(delegate* unmanaged[Cdecl]<WGPULogLevel, WGPUStringView, IntPtr, void>)&OnLog, IntPtr.Zero);
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static void OnLog(WGPULogLevel level, WGPUStringView message, IntPtr userdata)
	{
		var text = CString(message);
		((delegate* unmanaged[Cdecl]<int, IntPtr, IntPtr, void>)_logCallback)((int)level, text, _logUserdata);
		Free(text);
	}

	// ---------------------------------------------------------------- queue, error scopes, compilation, async pipelines

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static void wgpuQueueOnSubmittedWorkDone(nint queue, nint callback, nint userdata)
	{
		var info = new WGPUQueueWorkDoneCallbackInfo
		{
			Mode = Spontaneous,
			Callback = (IntPtr)(delegate* unmanaged[Cdecl]<WGPUQueueWorkDoneStatus, WGPUStringView, IntPtr, IntPtr, void>)&OnWorkDone,
			Userdata1 = callback,
			Userdata2 = userdata,
		};
		WGPU.wgpuQueueOnSubmittedWorkDone(queue, info);
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static void OnWorkDone(WGPUQueueWorkDoneStatus status, WGPUStringView message, IntPtr callback, IntPtr userdata)
	{
		var s = (int)status;
		var old = N2OKnown_QueueWorkDoneStatus(s) ? N2O_QueueWorkDoneStatus(s) : (int)SW.QueueWorkDoneStatus.Unknown;
		((delegate* unmanaged[Cdecl]<int, IntPtr, void>)callback)(old, userdata);
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static void wgpuDevicePopErrorScope(nint device, nint callback, nint userdata)
	{
		var info = new WGPUPopErrorScopeCallbackInfo
		{
			Mode = Spontaneous,
			Callback = (IntPtr)(delegate* unmanaged[Cdecl]<WGPUPopErrorScopeStatus, WGPUErrorType, WGPUStringView, IntPtr, IntPtr, void>)&OnErrorScopePopped,
			Userdata1 = callback,
			Userdata2 = userdata,
		};
		WGPU.wgpuDevicePopErrorScope(device, info);
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static void OnErrorScopePopped(WGPUPopErrorScopeStatus status, WGPUErrorType type, WGPUStringView message, IntPtr callback, IntPtr userdata)
	{
		// The old callback has no status: a failed pop (an empty scope stack) reports an Unknown error.
		var t = (int)type;
		var old = status == WGPUPopErrorScopeStatus.Success && N2OKnown_ErrorType(t) ? N2O_ErrorType(t) : (int)SW.ErrorType.Unknown;
		var text = CString(message);
		((delegate* unmanaged[Cdecl]<int, IntPtr, IntPtr, void>)callback)(old, text, userdata);
		Free(text);
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static void wgpuShaderModuleGetCompilationInfo(nint shaderModule, nint callback, nint userdata)
	{
		if (!OperatingSystem.IsBrowser())
		{
			// wgpu-native has no compilation info (it aborts on the request); report the request as failed rather
			// than as a module without messages.
			var none = new SW.CompilationInfo();
			((delegate* unmanaged[Cdecl]<int, SW.CompilationInfo*, IntPtr, void>)callback)((int)SW.CompilationInfoRequestStatus.Error, &none, userdata);
			return;
		}

		var info = new WGPUCompilationInfoCallbackInfo
		{
			Mode = Spontaneous,
			Callback = (IntPtr)(delegate* unmanaged[Cdecl]<WGPUCompilationInfoRequestStatus, WGPUCompilationInfo*, IntPtr, IntPtr, void>)&OnCompilationInfo,
			Userdata1 = callback,
			Userdata2 = userdata,
		};
		WGPU.wgpuShaderModuleGetCompilationInfo(shaderModule, info);
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static void OnCompilationInfo(WGPUCompilationInfoRequestStatus status, WGPUCompilationInfo* info, IntPtr callback, IntPtr userdata)
	{
		var s = (int)status;
		var oldStatus = N2OKnown_CompilationInfoRequestStatus(s) ? N2O_CompilationInfoRequestStatus(s) : (int)SW.CompilationInfoRequestStatus.Unknown;
		var count = info == null ? 0 : (int)info->MessageCount;
		var messages = count == 0 ? null : (SW.CompilationMessage*)NativeMemory.AllocZeroed((nuint)count, (nuint)sizeof(SW.CompilationMessage));
		for (var i = 0; i < count; i++)
		{
			var m = &info->Messages[i];
			messages[i].Message = (byte*)CString(m->Message);
			messages[i].Type = (SW.CompilationMessageType)N2O_CompilationMessageType((int)m->Type);
			messages[i].LineNum = m->LineNum;
			messages[i].LinePos = m->LinePos;
			messages[i].Offset = m->Offset;
			messages[i].Length = m->Length;
			// The stable header only counts UTF-8 bytes; these UTF-16 positions are exact for ASCII shader text.
			messages[i].Utf16LinePos = m->LinePos;
			messages[i].Utf16Offset = m->Offset;
			messages[i].Utf16Length = m->Length;
		}

		var old = new SW.CompilationInfo { MessageCount = (nuint)count, Messages = messages };
		((delegate* unmanaged[Cdecl]<int, SW.CompilationInfo*, IntPtr, void>)callback)(oldStatus, &old, userdata);
		for (var i = 0; i < count; i++)
		{
			Free((IntPtr)messages[i].Message);
		}
		NativeMemory.Free(messages);
	}

	// ---------------------------------------------------------------- what wgpu-native doesn't implement (it aborts)

	// The result of an error scope popped right after a call, in native memory: wgpu-native reports a pop at once,
	// but if the callback ever came late it must not write into a returned stack frame.
	private struct ScopeCapture
	{
		public int State; // 0 waiting, 1 reported, 2 abandoned by the caller
		public WGPUErrorType Type;
		public IntPtr Message;
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static void OnScopeCaptured(WGPUPopErrorScopeStatus status, WGPUErrorType type, WGPUStringView message, IntPtr capture, IntPtr unused)
	{
		var c = (ScopeCapture*)capture;
		c->Type = status == WGPUPopErrorScopeStatus.Success ? type : WGPUErrorType.Unknown;
		c->Message = CString(message);
		if (Interlocked.Exchange(ref c->State, 1) == 2)
		{
			Free(c->Message);
			NativeMemory.Free(c);
		}
	}

	// Runs `create` inside a validation error scope and reports it the way the old async pipeline callbacks did.
	private static void CreatePipelineNow(nint device, Func<IntPtr> create, Action<IntPtr> release, nint callback, nint userdata)
	{
		WGPU.wgpuDevicePushErrorScope(device, WGPUErrorFilter.Validation);
		var pipeline = create();
		var capture = (ScopeCapture*)NativeMemory.AllocZeroed((nuint)sizeof(ScopeCapture));
		WGPU.wgpuDevicePopErrorScope(device, new WGPUPopErrorScopeCallbackInfo
		{
			Mode = Spontaneous,
			Callback = (IntPtr)(delegate* unmanaged[Cdecl]<WGPUPopErrorScopeStatus, WGPUErrorType, WGPUStringView, IntPtr, IntPtr, void>)&OnScopeCaptured,
			Userdata1 = (IntPtr)capture,
		});

		var reported = Interlocked.CompareExchange(ref capture->State, 2, 0) != 0;
		var type = reported ? capture->Type : WGPUErrorType.NoError;
		var message = reported ? capture->Message : IntPtr.Zero;
		var status = type == WGPUErrorType.NoError ? SW.CreatePipelineAsyncStatus.Success
			: type == WGPUErrorType.Validation ? SW.CreatePipelineAsyncStatus.ValidationError
			: SW.CreatePipelineAsyncStatus.InternalError;
		if (status != SW.CreatePipelineAsyncStatus.Success && pipeline != IntPtr.Zero)
		{
			// The old callback hands no pipeline on failure.
			release(pipeline);
			pipeline = IntPtr.Zero;
		}

		((delegate* unmanaged[Cdecl]<int, IntPtr, IntPtr, IntPtr, void>)callback)((int)status, pipeline, message, userdata);
		if (reported)
		{
			Free(message);
			NativeMemory.Free(capture);
		}
	}

	// wgpu-native doesn't implement wgpuBufferGetMapState, so map states are tracked here, keyed by buffer.
	private static readonly ConcurrentDictionary<IntPtr, SW.BufferMapState> _mapStates = new();

	private static void SetMapState(IntPtr buffer, SW.BufferMapState state)
	{
		if (!OperatingSystem.IsBrowser())
		{
			_mapStates[buffer] = state;
		}
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static nint wgpuDeviceCreateBuffer(nint device, nint descriptor)
	{
		var a = Arena.Rent();
		WGPUBufferDescriptor* d = null;
		if (descriptor != 0)
		{
			d = (WGPUBufferDescriptor*)a.Alloc(sizeof(WGPUBufferDescriptor));
			Fill((SW.BufferDescriptor*)descriptor, d, ref a);
		}
		var buffer = WGPU.wgpuDeviceCreateBuffer(device, d);
		if (buffer != IntPtr.Zero)
		{
			SetMapState(buffer, d != null && d->MappedAtCreation != 0 ? SW.BufferMapState.Mapped : SW.BufferMapState.Unmapped);
		}
		a.Dispose();
		return buffer;
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static void wgpuBufferUnmap(nint buffer)
	{
		WGPU.wgpuBufferUnmap(buffer);
		SetMapState(buffer, SW.BufferMapState.Unmapped);
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static void wgpuBufferDestroy(nint buffer)
	{
		WGPU.wgpuBufferDestroy(buffer);
		SetMapState(buffer, SW.BufferMapState.Unmapped);
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static void wgpuBufferRelease(nint buffer)
	{
		// Forgotten with the caller's reference: the state of a buffer someone else still holds then reads Unmapped.
		_mapStates.TryRemove(buffer, out _);
		WGPU.wgpuBufferRelease(buffer);
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static int wgpuBufferGetMapState(nint buffer)
		=> OperatingSystem.IsBrowser()
			? N2O_BufferMapState((int)WGPU.wgpuBufferGetMapState(buffer))
			: (int)_mapStates.GetValueOrDefault(buffer, SW.BufferMapState.Unmapped);

	// A pipeline status the old API has no member for (the stable CallbackCancelled) becomes its Unknown.
	private static int OldPipelineStatus(WGPUCreatePipelineAsyncStatus status)
		=> N2OKnown_CreatePipelineAsyncStatus((int)status) ? N2O_CreatePipelineAsyncStatus((int)status) : (int)SW.CreatePipelineAsyncStatus.Unknown;

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static void wgpuDeviceCreateComputePipelineAsync(nint device, nint descriptor, nint callback, nint userdata)
	{
		var a = Arena.Rent();
		WGPUComputePipelineDescriptor* d = null;
		if (descriptor != 0)
		{
			d = (WGPUComputePipelineDescriptor*)a.Alloc(sizeof(WGPUComputePipelineDescriptor));
			Fill((SW.ComputePipelineDescriptor*)descriptor, d, ref a);
		}
		if (!OperatingSystem.IsBrowser())
		{
			// wgpu-native doesn't implement the async creation (it aborts): create now and report through the callback.
			CreatePipelineNow(device, () => WGPU.wgpuDeviceCreateComputePipeline(device, d), WGPU.wgpuComputePipelineRelease, callback, userdata);
			a.Dispose();
			return;
		}
		var info = new WGPUCreateComputePipelineAsyncCallbackInfo
		{
			Mode = Spontaneous,
			Callback = (IntPtr)(delegate* unmanaged[Cdecl]<WGPUCreatePipelineAsyncStatus, IntPtr, WGPUStringView, IntPtr, IntPtr, void>)&OnPipelineCreated,
			Userdata1 = callback,
			Userdata2 = userdata,
		};
		WGPU.wgpuDeviceCreateComputePipelineAsync(device, d, info);
		a.Dispose();
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static void wgpuDeviceCreateRenderPipelineAsync(nint device, nint descriptor, nint callback, nint userdata)
	{
		var a = Arena.Rent();
		WGPURenderPipelineDescriptor* d = null;
		if (descriptor != 0)
		{
			d = (WGPURenderPipelineDescriptor*)a.Alloc(sizeof(WGPURenderPipelineDescriptor));
			Fill((SW.RenderPipelineDescriptor*)descriptor, d, ref a);
		}
		if (!OperatingSystem.IsBrowser())
		{
			CreatePipelineNow(device, () => WGPU.wgpuDeviceCreateRenderPipeline(device, d), WGPU.wgpuRenderPipelineRelease, callback, userdata);
			a.Dispose();
			return;
		}
		var info = new WGPUCreateRenderPipelineAsyncCallbackInfo
		{
			Mode = Spontaneous,
			Callback = (IntPtr)(delegate* unmanaged[Cdecl]<WGPUCreatePipelineAsyncStatus, IntPtr, WGPUStringView, IntPtr, IntPtr, void>)&OnPipelineCreated,
			Userdata1 = callback,
			Userdata2 = userdata,
		};
		WGPU.wgpuDeviceCreateRenderPipelineAsync(device, d, info);
		a.Dispose();
	}

	// Compute and render pipeline callbacks share a shape in both headers.
	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static void OnPipelineCreated(WGPUCreatePipelineAsyncStatus status, IntPtr pipeline, WGPUStringView message, IntPtr callback, IntPtr userdata)
	{
		var text = CString(message);
		((delegate* unmanaged[Cdecl]<int, IntPtr, IntPtr, IntPtr, void>)callback)(OldPipelineStatus(status), pipeline, text, userdata);
		Free(text);
	}

	// ---------------------------------------------------------------- limits, features, adapter info, formats

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static uint wgpuAdapterGetLimits(nint adapter, nint limits)
	{
		WGPULimits stable = default;
		if (WGPU.wgpuAdapterGetLimits(adapter, &stable) != WGPUStatus.Success)
		{
			return 0;
		}
		Unfill(&stable, &((SW.SupportedLimits*)limits)->Limits);
		return 1;
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static uint wgpuDeviceGetLimits(nint device, nint limits)
	{
		WGPULimits stable = default;
		if (WGPU.wgpuDeviceGetLimits(device, &stable) != WGPUStatus.Success)
		{
			return 0;
		}
		Unfill(&stable, &((SW.SupportedLimits*)limits)->Limits);
		return 1;
	}

	// The old API reports features as a count plus an optional array to fill; features it has no name for are left out.
	private static nuint EnumerateFeatures(WGPUSupportedFeatures features, SW.FeatureName* output)
	{
		nuint count = 0;
		for (nuint i = 0; i < features.FeatureCount; i++)
		{
			var v = (int)features.Features[i];
			int old;
			if (N2OKnown_FeatureName(v)) { old = N2O_FeatureName(v); }
			else if (N2OKnown_NativeFeature(v)) { old = N2O_NativeFeature(v); }
			else { continue; }

			if (output != null)
			{
				output[count] = (SW.FeatureName)old;
			}
			count++;
		}
		WGPU.wgpuSupportedFeaturesFreeMembers(features);
		return count;
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static nuint wgpuAdapterEnumerateFeatures(nint adapter, nint features)
	{
		WGPUSupportedFeatures stable = default;
		WGPU.wgpuAdapterGetFeatures(adapter, &stable);
		return EnumerateFeatures(stable, (SW.FeatureName*)features);
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static nuint wgpuDeviceEnumerateFeatures(nint device, nint features)
	{
		WGPUSupportedFeatures stable = default;
		WGPU.wgpuDeviceGetFeatures(device, &stable);
		return EnumerateFeatures(stable, (SW.FeatureName*)features);
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static void wgpuAdapterRequestAdapterInfo(nint adapter, nint callback, nint userdata)
	{
		// The old callback receives the strings for its duration only, so they're freed once it returns.
		WGPUAdapterInfo info = default;
		WGPU.wgpuAdapterGetInfo(adapter, &info);
		var old = new SW.AdapterInfo
		{
			Vendor = (byte*)CString(info.Vendor),
			Architecture = (byte*)CString(info.Architecture),
			Device = (byte*)CString(info.Device),
			Description = (byte*)CString(info.Description),
		};
		((delegate* unmanaged[Cdecl]<SW.AdapterInfo, IntPtr, void>)callback)(old, userdata);
		Free((IntPtr)old.Vendor);
		Free((IntPtr)old.Architecture);
		Free((IntPtr)old.Device);
		Free((IntPtr)old.Description);
		WGPU.wgpuAdapterInfoFreeMembers(info);
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static int wgpuSurfaceGetPreferredFormat(nint surface, nint adapter)
	{
		// The stable header dropped this: the surface's first offered format is its preferred one.
		WGPUSurfaceCapabilities caps = default;
		if (WGPU.wgpuSurfaceGetCapabilities(surface, adapter, &caps) != WGPUStatus.Success)
		{
			return (int)SW.TextureFormat.Undefined;
		}
		var format = caps.FormatCount > 0 ? N2O_TextureFormat((int)caps.Formats[0]) : (int)SW.TextureFormat.Undefined;
		WGPU.wgpuSurfaceCapabilitiesFreeMembers(caps);
		return format;
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static uint wgpuInstanceHasWGSLLanguageFeature(nint instance, int feature)
		// Same values; the stable header renamed the enum. wgpu-native doesn't implement the query and aborts the
		// process on it, so outside the browser (emdawnwebgpu) no language feature is reported.
		=> OperatingSystem.IsBrowser() ? WGPU.wgpuInstanceHasWGSLLanguageFeature(instance, (WGPUWGSLLanguageFeatureName)feature) : 0u;

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static nint wgpuGetProcAddress(nint device, nint procName)
		=> SilkCompatContext.Instance.TryGetProcAddress(Marshal.PtrToStringUTF8(procName) ?? "", out var address) ? address : 0;

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static void wgpuRenderPassEncoderSetPushConstants(nint encoder, uint stages, uint offset, uint sizeBytes, nint data)
		// The stable wgpu-native calls push constants "immediates": one range, visible to every stage.
		=> WGPU.wgpuRenderPassEncoderSetImmediates(encoder, offset, data, sizeBytes);

	private static void AddHand(Dictionary<string, nint> t)
	{
		t["wgpuQueueOnSubmittedWorkDone"] = (nint)(delegate* unmanaged[Cdecl]<nint, nint, nint, void>)&wgpuQueueOnSubmittedWorkDone;
		t["wgpuDevicePopErrorScope"] = (nint)(delegate* unmanaged[Cdecl]<nint, nint, nint, void>)&wgpuDevicePopErrorScope;
		t["wgpuShaderModuleGetCompilationInfo"] = (nint)(delegate* unmanaged[Cdecl]<nint, nint, nint, void>)&wgpuShaderModuleGetCompilationInfo;
		t["wgpuDeviceCreateComputePipelineAsync"] = (nint)(delegate* unmanaged[Cdecl]<nint, nint, nint, nint, void>)&wgpuDeviceCreateComputePipelineAsync;
		t["wgpuDeviceCreateRenderPipelineAsync"] = (nint)(delegate* unmanaged[Cdecl]<nint, nint, nint, nint, void>)&wgpuDeviceCreateRenderPipelineAsync;
		t["wgpuAdapterGetLimits"] = (nint)(delegate* unmanaged[Cdecl]<nint, nint, uint>)&wgpuAdapterGetLimits;
		t["wgpuDeviceGetLimits"] = (nint)(delegate* unmanaged[Cdecl]<nint, nint, uint>)&wgpuDeviceGetLimits;
		t["wgpuAdapterEnumerateFeatures"] = (nint)(delegate* unmanaged[Cdecl]<nint, nint, nuint>)&wgpuAdapterEnumerateFeatures;
		t["wgpuDeviceEnumerateFeatures"] = (nint)(delegate* unmanaged[Cdecl]<nint, nint, nuint>)&wgpuDeviceEnumerateFeatures;
		t["wgpuAdapterRequestAdapterInfo"] = (nint)(delegate* unmanaged[Cdecl]<nint, nint, nint, void>)&wgpuAdapterRequestAdapterInfo;
		t["wgpuSurfaceGetPreferredFormat"] = (nint)(delegate* unmanaged[Cdecl]<nint, nint, int>)&wgpuSurfaceGetPreferredFormat;
		t["wgpuInstanceHasWGSLLanguageFeature"] = (nint)(delegate* unmanaged[Cdecl]<nint, int, uint>)&wgpuInstanceHasWGSLLanguageFeature;
		t["wgpuGetProcAddress"] = (nint)(delegate* unmanaged[Cdecl]<nint, nint, nint>)&wgpuGetProcAddress;
		t["wgpuRenderPassEncoderSetPushConstants"] = (nint)(delegate* unmanaged[Cdecl]<nint, uint, uint, uint, nint, void>)&wgpuRenderPassEncoderSetPushConstants;
		t["wgpuDeviceCreateBuffer"] = (nint)(delegate* unmanaged[Cdecl]<nint, nint, nint>)&wgpuDeviceCreateBuffer;
		t["wgpuBufferUnmap"] = (nint)(delegate* unmanaged[Cdecl]<nint, void>)&wgpuBufferUnmap;
		t["wgpuBufferDestroy"] = (nint)(delegate* unmanaged[Cdecl]<nint, void>)&wgpuBufferDestroy;
		t["wgpuBufferRelease"] = (nint)(delegate* unmanaged[Cdecl]<nint, void>)&wgpuBufferRelease;
		t["wgpuBufferGetMapState"] = (nint)(delegate* unmanaged[Cdecl]<nint, int>)&wgpuBufferGetMapState;
		// wgpuGenerateReport is not translated: the stable wgpu-native reshaped its report, and the bindings don't bind it.

		t["wgpuInstanceRequestAdapter"] = (nint)(delegate* unmanaged[Cdecl]<nint, nint, nint, nint, void>)&wgpuInstanceRequestAdapter;
		t["wgpuAdapterRequestDevice"] = (nint)(delegate* unmanaged[Cdecl]<nint, nint, nint, nint, void>)&wgpuAdapterRequestDevice;
		t["wgpuDeviceSetUncapturedErrorCallback"] = (nint)(delegate* unmanaged[Cdecl]<nint, nint, nint, void>)&wgpuDeviceSetUncapturedErrorCallback;
		t["wgpuAdapterGetProperties"] = (nint)(delegate* unmanaged[Cdecl]<nint, nint, void>)&wgpuAdapterGetProperties;
		t["wgpuBufferMapAsync"] = (nint)(delegate* unmanaged[Cdecl]<nint, uint, nuint, nuint, nint, nint, void>)&wgpuBufferMapAsync;
		t["wgpuSurfaceGetCapabilities"] = (nint)(delegate* unmanaged[Cdecl]<nint, nint, nint, void>)&wgpuSurfaceGetCapabilities;
		t["wgpuSurfaceCapabilitiesFreeMembers"] = (nint)(delegate* unmanaged[Cdecl]<SW.SurfaceCapabilities, void>)&wgpuSurfaceCapabilitiesFreeMembers;
		t["wgpuSurfaceGetCurrentTexture"] = (nint)(delegate* unmanaged[Cdecl]<nint, nint, void>)&wgpuSurfaceGetCurrentTexture;
		// wgpu-native's own functions: the browser has none of them.
		t["wgpuDevicePoll"] = OperatingSystem.IsBrowser() ? 0 : (nint)(delegate* unmanaged[Cdecl]<nint, uint, nint, uint>)&wgpuDevicePoll;
		t["wgpuSetLogCallback"] = OperatingSystem.IsBrowser() ? 0 : (nint)(delegate* unmanaged[Cdecl]<nint, nint, void>)&wgpuSetLogCallback;
	}
}
