#nullable enable

using System;
using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;
using Uno.Disposables;
using Uno.Foundation.Logging;
using Uno.Graphics;

namespace Uno.UI.Runtime.WebAssembly.Browser.Graphics;

internal class WasmNativeOpenGLWrapper : INativeOpenGLWrapper
{
	// Created by the native shim on the calling thread: emscripten keeps a GL table per thread, and
	// the native GL calls only see the one on theirs.
	private readonly int _glCtxHandle;
	private bool _disposed;

	public WasmNativeOpenGLWrapper(XamlRoot xamlRoot)
	{
		_glCtxHandle = NativeMethods.uno_gl_create_context();

		if (_glCtxHandle == 0)
		{
			throw new InvalidOperationException("Failed to create an offscreen WebGL context.");
		}

		this.LogInfo()?.Info($"Created a WasmNativeOpenGLWrapper instance using WebGL (ctx={_glCtxHandle}).");
	}

	public IDisposable MakeCurrent()
	{
		var previousHandle = NativeMethods.uno_gl_make_current(_glCtxHandle);

		if (previousHandle < 0)
		{
			throw new InvalidOperationException($"WebGL context {_glCtxHandle} is not available on this thread.");
		}

		return Disposable.Create(() => _ = NativeMethods.uno_gl_make_current(previousHandle));
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

		NativeMethods.uno_gl_destroy_context(_glCtxHandle);
	}

	private static class NativeMethods
	{
		[DllImport("uno_gl_shim", CallingConvention = CallingConvention.Cdecl)]
		internal static extern int uno_gl_create_context();

		[DllImport("uno_gl_shim", CallingConvention = CallingConvention.Cdecl)]
		internal static extern int uno_gl_make_current(int handle);

		[DllImport("uno_gl_shim", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void uno_gl_destroy_context(int handle);
	}
}
