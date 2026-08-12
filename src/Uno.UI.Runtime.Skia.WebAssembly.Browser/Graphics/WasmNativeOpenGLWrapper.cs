#nullable enable

using System;
using System.Runtime.InteropServices.JavaScript;
using Microsoft.UI.Xaml;
using Uno.Disposables;
using Uno.Foundation.Logging;
using Uno.Graphics;

namespace Uno.UI.Runtime.Skia.WebAssembly.Browser.Graphics;

internal partial class WasmNativeOpenGLWrapper : INativeOpenGLWrapper
{
	// Emscripten's GL.makeContextCurrent takes a numeric handle. We extract it from the JSObject
	// returned by tryCreateInstance and pass plain ints to the per-call JSImports so we don't
	// rely on JSObject-parameter trampolines being available in interpreter mode.
	private readonly int _glCtxHandle;
	private bool _disposed;

	public WasmNativeOpenGLWrapper(XamlRoot xamlRoot)
	{
		var jsObject = NativeMethods.TryCreateInstance();
		if (!jsObject.GetPropertyAsBoolean("success"))
		{
			var error = jsObject.GetPropertyAsString("error");
			jsObject.Dispose();
			throw new InvalidOperationException($"Failed to create offscreen WebGL context: {error}");
		}

		_glCtxHandle = jsObject.GetPropertyAsInt32("glCtxHandle");
		jsObject.Dispose();

		if (_glCtxHandle == 0)
		{
			throw new InvalidOperationException("WasmNativeOpenGLWrapper.tryCreateInstance returned success but no glCtxHandle.");
		}

		this.LogInfo()?.Info($"Created a WasmNativeOpenGLWrapper instance using WebGL (ctx={_glCtxHandle}).");
	}

	public IDisposable MakeCurrent()
	{
		var previousHandle = NativeMethods.MakeCurrent(_glCtxHandle);
		return Disposable.Create(() => NativeMethods.RestoreContext(previousHandle));
	}

	public IntPtr GetProcAddress(string proc)
	{
		if (TryGetProcAddress(proc, out var addr))
		{
			return addr;
		}

		throw new InvalidOperationException($"No function was found with the name '{proc}'.");
	}

	public bool TryGetProcAddress(string proc, out IntPtr addr)
	{
		// dotnet.wasm doesn't link emscripten's WebGL bindings, so Silk.NET can't dispatch GL
		// through native function pointers from this module. Instead WasmGLFunctions exposes
		// [UnmanagedCallersOnly] managed methods whose addresses live in dotnet.wasm's own
		// function table; their bodies trampoline to a JS WebGL2 shim via [JSImport].
		addr = WasmGLFunctions.GetProcAddress(proc);
		return addr != IntPtr.Zero;
	}

	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}
		_disposed = true;

		NativeMethods.Destroy(_glCtxHandle);
	}

	private static partial class NativeMethods
	{
		[JSImport($"globalThis.Uno.UI.Runtime.Skia.{nameof(WasmNativeOpenGLWrapper)}.tryCreateInstance")]
		internal static partial JSObject TryCreateInstance();

		[JSImport($"globalThis.Uno.UI.Runtime.Skia.{nameof(WasmNativeOpenGLWrapper)}.makeCurrent")]
		internal static partial int MakeCurrent(int glCtxHandle);

		[JSImport($"globalThis.Uno.UI.Runtime.Skia.{nameof(WasmNativeOpenGLWrapper)}.restoreContext")]
		internal static partial void RestoreContext(int previousHandle);

		[JSImport($"globalThis.Uno.UI.Runtime.Skia.{nameof(WasmNativeOpenGLWrapper)}.destroy")]
		internal static partial void Destroy(int glCtxHandle);
	}
}
