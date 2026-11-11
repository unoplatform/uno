#nullable enable

using System;
using Microsoft.UI.Xaml;

namespace Uno.UI.Helpers;

/// <summary>
/// Provides the platform the app is currently running on, and the renderer drawing its windows.
/// </summary>
public static class RuntimePlatformHelper
{
#if HAS_UNO
	private static UnoRuntimePlatform _hostPlatform;

	/// <summary>
	/// Gets the platform the app is currently running on.
	/// </summary>
	/// <remarks>
	/// Returns <see cref="UnoRuntimePlatform.Unknown"/> until an Uno Platform host is created.
	/// </remarks>
	public static UnoRuntimePlatform Current
	{
		get
		{
			if (_hostPlatform != UnoRuntimePlatform.Unknown)
			{
				return _hostPlatform;
			}

			// Islands run without an Uno Platform host.
			return Application.Current is { Host: null }
				? UnoRuntimePlatform.Islands
				: UnoRuntimePlatform.Unknown;
		}
	}

	/// <summary>
	/// Gets a value indicating whether the window of <paramref name="xamlRoot"/> is drawn by the Skia renderer.
	/// </summary>
	/// <remarks>
	/// Returns <see langword="false"/> until the window has picked its renderer, which happens before its first frame.
	/// </remarks>
	public static bool IsSkia(XamlRoot xamlRoot) => GetRendererAssemblyName(xamlRoot) == SkiaAssemblyName;

	/// <summary>
	/// Gets a value indicating whether the window of <paramref name="xamlRoot"/> is drawn by the WebGPU renderer.
	/// </summary>
	/// <remarks>
	/// Returns <see langword="false"/> until the window has picked its renderer, which happens before its first frame.
	/// It also returns <see langword="false"/> when WebGPU was requested but the window fell back to another renderer.
	/// </remarks>
	public static bool IsWebGpu(XamlRoot xamlRoot) => GetRendererAssemblyName(xamlRoot) == WebGpuAssemblyName;

	private const string SkiaAssemblyName = "Uno.UI.Composition.Skia";
	private const string WebGpuAssemblyName = "Uno.UI.Composition.WebGpu";

	internal static void SetHostPlatform(UnoRuntimePlatform platform) => _hostPlatform = platform;

	private static string? GetRendererAssemblyName(XamlRoot xamlRoot)
	{
		ArgumentNullException.ThrowIfNull(xamlRoot);

		try
		{
			return xamlRoot.VisualTree.RootElement?.Visual.CompositionTarget?.Renderer?.GetType().Assembly.GetName().Name;
		}
		catch (InvalidOperationException)
		{
			// The renderer getter throws while the window is still negotiating its backend.
			return null;
		}
	}
#else
	/// <summary>
	/// Gets the platform the app is currently running on.
	/// </summary>
	public static UnoRuntimePlatform Current => UnoRuntimePlatform.WindowsAppSdk;

	/// <summary>
	/// Gets a value indicating whether the window of <paramref name="xamlRoot"/> is drawn by the Skia renderer.
	/// </summary>
	public static bool IsSkia(XamlRoot xamlRoot) => false;

	/// <summary>
	/// Gets a value indicating whether the window of <paramref name="xamlRoot"/> is drawn by the WebGPU renderer.
	/// </summary>
	public static bool IsWebGpu(XamlRoot xamlRoot) => false;
#endif
}
