// WebGPU device bring-up: the host-facing context factories and the instance/adapter/device/queue they create.
// Names no renderer type — the render backend adopts the device through IWebGpuDeviceContext.
#nullable disable
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.JavaScript;
using System.Threading;
using System.Threading.Tasks;
using Silk.NET.WebGPU;
using Silk.NET.WebGPU.Extensions.WGPU;
using Uno.UI.Composition.Drawing;
using Uno.UI.Composition.WebGpu.SilkCompat;
using Windows.Graphics.Effects.Interop;
using Windows.Foundation;
using WColor = Windows.UI.Color;

namespace Uno.UI.Composition.WebGpu;


/// <summary>The host-facing WebGPU context factories. Each builds the device bring-up (WebGpuInitDevice) + a
/// swapchain/browser context and returns it as a neutral ISwapChain — naming no renderer type. The
/// rasterizationScale argument is retained for the host-facing signature but no longer influences init (MSAA is
/// chosen by capability, and the per-frame DPI scale is applied by the drawing session's matrix).</summary>
internal static class WebGpuContext
{
	/// <summary>Win32 HWND swapchain context.</summary>
	public static unsafe ISwapChain CreateWin32(nint hwnd, nint hinstance, float rasterizationScale)
		=> new WebGpuSwapChainContext(TextureFormat.Bgra8Unorm, inst => WebGpuSwapChainContext.CreateHwndSurface(inst, hinstance, hwnd));

	/// <summary>X11 window swapchain context.</summary>
	public static unsafe ISwapChain CreateX11(nint display, nint window, float rasterizationScale)
		=> new WebGpuSwapChainContext(TextureFormat.Bgra8Unorm, inst => WebGpuSwapChainContext.CreateXlibSurface(inst, display, (ulong)window));

	/// <summary>Metal (<c>CAMetalLayer</c>) swapchain context — macOS and iOS/tvOS.</summary>
	public static unsafe ISwapChain CreateMetal(nint caMetalLayer, float rasterizationScale)
		=> new WebGpuSwapChainContext(TextureFormat.Bgra8Unorm, inst => WebGpuSwapChainContext.CreateMetalSurface(inst, caMetalLayer));

	/// <summary>Android <c>ANativeWindow</c> swapchain context.</summary>
	public static unsafe ISwapChain CreateAndroid(nint aNativeWindow, float rasterizationScale)
		=> new WebGpuSwapChainContext(TextureFormat.Bgra8Unorm, inst => WebGpuSwapChainContext.CreateAndroidSurface(inst, aNativeWindow));

	/// <summary>
	/// Browser (canvas) context. Asynchronous because the device is created in JavaScript (navigator.gpu) and
	/// imported into emdawnwebgpu's C handle table — the in-WASM wgpuInstanceProcessEvents pump hangs when driven
	/// from a managed call stack — and the JS thread must not be blocked. Returns null if the device import fails.
	/// </summary>
	public static async Task<ISwapChain> CreateWasmAsync(string canvasId)
	{
		var inst = WebGpuInitDevice.CreateInstanceHandle();
		var devPtr = await WebGpuJsInterop.CreateImportedDeviceAsync((int)inst);
		if (devPtr == 0)
		{
			return null;
		}

		var device = WebGpuInitDevice.FromImported(TextureFormat.Rgba8Unorm, inst, devPtr);
		// The live JS GPUDevice, for the textures the browser context creates in JS.
		device.JsDeviceObject = WebGpuJsInterop.GetJsObject(devPtr);
		return new WebGpuBrowserGraphicsContext(device, canvasId ?? "");
	}
}

/// <summary>
/// The Uno-provided WebGPU device BRING-UP (the host "GPU-API half"): creates instance/adapter/device/queue and owns
/// the present-blit sampler.
/// adopts to build its engine; it names no renderer type. Native path creates the device synchronously; the browser
/// path adopts a device imported from JS.
/// </summary>
internal sealed unsafe class WebGpuInitDevice : IWebGpuDeviceContext
{
	public Instance* Inst;
	public Adapter* Adapter;
	public Device* Dev;
	public Queue* Q;
	public readonly TextureFormat ColorFormat;
	/// <summary>A Vulkan adapter that rasterizes on the CPU (SwiftShader, lavapipe), so one frame can take seconds.</summary>
	public bool IsSoftwareVulkan { get; private set; }
	public Sampler* Smp;                     // present-blit sampler (used by the swapchain/browser contexts)
	public JSObject JsDeviceObject;          // browser only: the live JS GPUDevice, for textures created in JS

	public static nint CreateInstanceHandle() => (nint)CreateInstance();

	private static WebGPU Api => SilkCompatContext.Api;
	private static Wgpu Wgpu => SilkCompatContext.Wgpu;

	/// <summary>
	/// Creates the wgpu instance. <c>UNO_WEBGPU_BACKENDS</c> (comma-separated: vulkan, gl, metal, dx12)
	/// restricts wgpu's adapter enumeration to the named backends — needed e.g. under RenderDoc, whose
	/// hooks crash multi-backend instance enumeration. Unset keeps wgpu's default (all backends).
	/// </summary>
	private static Instance* CreateInstance()
	{
		if (Environment.GetEnvironmentVariable("UNO_WEBGPU_BACKENDS") is not { Length: > 0 } spec)
		{
			return Api.CreateInstance(null);
		}

		var backends = InstanceBackend.All;
		foreach (var name in spec.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
		{
			backends |= name.ToLowerInvariant() switch
			{
				"vulkan" => InstanceBackend.Vulkan,
				"gl" => InstanceBackend.GL,
				"metal" => InstanceBackend.Metal,
				"dx12" => InstanceBackend.DX12,
				_ => InstanceBackend.All,
			};
		}

		var extras = new InstanceExtras
		{
			Chain = new ChainedStruct { SType = (SType)NativeSType.STypeInstanceExtras },
			Backends = backends,
		};
		var descriptor = new InstanceDescriptor { NextInChain = &extras.Chain };
		return Api.CreateInstance(&descriptor);
	}

	private static readonly object _sharedGate = new();
	private static WebGpuInitDevice _shared;

	/// <summary>
	/// The process-wide device, created on first use. One device serves every window on purpose: the drawing
	/// factory built from it is installed as the process-wide <c>DrawingFactory.Current</c>, so a device owned by
	/// one window would die with that window and leave every other window recording against a released device -
	/// which wgpu answers by aborting the process. Windows own their SURFACES; this outlives all of them.
	/// </summary>
	internal static WebGpuInitDevice GetShared(TextureFormat colorFormat)
	{
		lock (_sharedGate)
		{
			return _shared ??= new WebGpuInitDevice(colorFormat);
		}
	}

	public WebGpuInitDevice(TextureFormat colorFormat)
	{
		ColorFormat = colorFormat;
		EnableNativeLog();
		Inst = CreateInstance();

		var abox = new nint[1];
		var ah = GCHandle.Alloc(abox);
		var aopts = new RequestAdapterOptions { PowerPreference = PowerPreference.HighPerformance };
		Api.InstanceRequestAdapter(Inst, &aopts, new PfnRequestAdapterCallback(&OnAdapter), (void*)GCHandle.ToIntPtr(ah));
		for (int i = 0; i < 1000 && abox[0] == 0; i++) { Api.InstanceProcessEvents(Inst); }
		Adapter = (Adapter*)abox[0];
		ah.Free();
		if (Adapter == null) { throw new InvalidOperationException("WebGPU: wgpuInstanceRequestAdapter returned no adapter (no compatible GPU/driver, or the callback never fired)."); }

		var dbox = new nint[1];
		var dh = GCHandle.Alloc(dbox);
		var ddesc = new DeviceDescriptor();
		// A lost device otherwise surfaces only as the next call's panic, with no word on why it was lost.
		ddesc.DeviceLostCallback = new PfnDeviceLostCallback(&OnDeviceLost);
		Api.AdapterRequestDevice(Adapter, &ddesc, new PfnRequestDeviceCallback(&OnDevice), (void*)GCHandle.ToIntPtr(dh));
		for (int i = 0; i < 1000 && dbox[0] == 0; i++) { Api.InstanceProcessEvents(Inst); }
		Dev = (Device*)dbox[0];
		dh.Free();
		if (Dev == null) { throw new InvalidOperationException("WebGPU: wgpuAdapterRequestDevice returned no device."); }

		// Non-fatal uncaptured-error handler (wgpu's default panics the process on any validation/OOM error).
		Api.DeviceSetUncapturedErrorCallback(Dev, new PfnErrorCallback(&OnUncapturedError), null);

		Q = Api.DeviceGetQueue(Dev);
		CreatePresentSampler();
		System.Console.WriteLine($"[webgpu] init device — {DescribeAdapter(Adapter, out var adapterType, out var backend)} colorFormat={ColorFormat}");
		IsSoftwareVulkan = adapterType == AdapterType.Cpu && backend == BackendType.Vulkan;
	}

	private WebGpuInitDevice(TextureFormat colorFormat, Instance* inst, Device* dev)
	{
		ColorFormat = colorFormat;
		Inst = inst;
		Adapter = null;   // JS adapter isn't imported
		Dev = dev;
		Q = Api.DeviceGetQueue(Dev);
		CreatePresentSampler();
	}

	/// <summary>Adopts an instance + a device imported from JS (browser bring-up). No adapter handle.</summary>
	public static WebGpuInitDevice FromImported(TextureFormat colorFormat, nint inst, nint dev)
		=> new WebGpuInitDevice(colorFormat, (Instance*)inst, (Device*)dev);

	private void CreatePresentSampler()
	{
		var sd = new SamplerDescriptor { AddressModeU = AddressMode.ClampToEdge, AddressModeV = AddressMode.ClampToEdge, MagFilter = FilterMode.Linear, MinFilter = FilterMode.Linear, MipmapFilter = MipmapFilterMode.Nearest, MaxAnisotropy = 1 };
		Smp = Api.DeviceCreateSampler(Dev, &sd);
	}

	// --- IWebGpuDeviceContext ---
	WebGPU IWebGpuDeviceContext.Api => Api;
	Instance* IWebGpuDeviceContext.Instance => Inst;
	Adapter* IWebGpuDeviceContext.Adapter => Adapter;
	Device* IWebGpuDeviceContext.Device => Dev;
	Queue* IWebGpuDeviceContext.Queue => Q;
	TextureFormat IWebGpuDeviceContext.ColorFormat => ColorFormat;
	public GraphicsContextKind Kind => GraphicsContextKind.WebGpu;

	public void Dispose()
	{
		if (Smp != null) { Api.SamplerRelease(Smp); Smp = null; }
		if (Q != null) { Api.QueueRelease(Q); Q = null; }
		if (Dev != null) { Api.DeviceRelease(Dev); Dev = null; }
		if (Adapter != null) { Api.AdapterRelease(Adapter); Adapter = null; }
		if (Inst != null) { Api.InstanceRelease(Inst); Inst = null; }
	}

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void OnAdapter(RequestAdapterStatus status, Adapter* adapter, byte* message, void* u1)
		=> ((nint[])GCHandle.FromIntPtr((nint)u1).Target!)[0] = (nint)adapter;

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void OnDevice(RequestDeviceStatus status, Device* device, byte* message, void* u1)
		=> ((nint[])GCHandle.FromIntPtr((nint)u1).Target!)[0] = (nint)device;

	/// <summary>
	/// <c>UNO_WEBGPU_LOG</c> (error, warn, info, debug, trace) routes wgpu's own log to stderr: the only account of
	/// what a driver or the validation layer did before a failure that surfaces later, such as a lost device.
	/// </summary>
	private static void EnableNativeLog()
	{
		var level = Environment.GetEnvironmentVariable("UNO_WEBGPU_LOG")?.ToLowerInvariant() switch
		{
			"error" => LogLevel.Error,
			"warn" => LogLevel.Warn,
			"info" => LogLevel.Info,
			"debug" => LogLevel.Debug,
			"trace" => LogLevel.Trace,
			_ => LogLevel.Off,
		};
		if (level == LogLevel.Off)
		{
			return;
		}

		Wgpu.SetLogCallback(new PfnLogCallback(&OnNativeLog), null);
		Wgpu.SetLogLevel(level);

		if (OperatingSystem.IsAndroid())
		{
			ForwardNativeStderr();
		}
	}

	private static int _stderrForwarded;

	/// <summary>
	/// Android drops a process's native stderr, which is where wgpu writes the message of a panic before aborting.
	/// Points fd 2 at a pipe and copies what arrives to the console, which .NET for Android sends to logcat.
	/// </summary>
	private static void ForwardNativeStderr()
	{
		if (Interlocked.Exchange(ref _stderrForwarded, 1) != 0)
		{
			return;
		}

		var fds = stackalloc int[2];
		if (pipe(fds) != 0 || dup2(fds[1], 2) < 0)
		{
			return;
		}

		var readFd = fds[0];
		var thread = new Thread(() => ForwardLines(readFd))
		{
			IsBackground = true,
			Name = "UnoWebGpuStderr",
		};
		thread.Start();
	}

	private static void ForwardLines(int fd)
	{
		var line = new System.Text.StringBuilder();
		var buffer = new byte[1024];
		while (true)
		{
			nint count;
			fixed (byte* p = buffer)
			{
				count = read(fd, p, buffer.Length);
			}

			if (count <= 0)
			{
				return;
			}

			foreach (var ch in System.Text.Encoding.UTF8.GetString(buffer, 0, (int)count))
			{
				if (ch == '\n')
				{
					System.Console.WriteLine($"[stderr] {line}");
					line.Clear();
				}
				else
				{
					line.Append(ch);
				}
			}
		}
	}

	[DllImport("libc", SetLastError = true)]
	private static extern int pipe(int* fds);

	[DllImport("libc", SetLastError = true)]
	private static extern int dup2(int oldFd, int newFd);

	[DllImport("libc", SetLastError = true)]
	private static extern nint read(int fd, byte* buffer, nint count);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void OnNativeLog(LogLevel level, byte* message, void* userdata)
		=> System.Console.Error.WriteLine($"[wgpu {level}] {Text(message)}");

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void OnDeviceLost(DeviceLostReason reason, byte* message, void* userdata)
		=> System.Console.Error.WriteLine($"[webgpu] device lost ({reason}): {Text(message)}");

	private static string DescribeAdapter(Adapter* adapter, out AdapterType type, out BackendType backend)
	{
		AdapterProperties info = default;
		Api.AdapterGetProperties(adapter, &info);
		type = info.AdapterType;
		backend = info.BackendType;
		return $"adapter='{Text(info.Name)}' backend={info.BackendType} type={info.AdapterType}";
	}

	private static string Text(byte* text)
		=> System.Runtime.InteropServices.Marshal.PtrToStringUTF8((IntPtr)text) ?? "";

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void OnUncapturedError(ErrorType type, byte* message, void* userdata)
		=> System.Console.Error.WriteLine($"[webgpu] uncaptured error ({type}): {Text(message)}");
}
