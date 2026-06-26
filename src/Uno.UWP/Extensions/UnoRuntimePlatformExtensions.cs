// This file is compiled in two forms (mirroring UnoRuntimePlatform.cs):
// - internal, operating on the internal "RuntimePlatform" enum, in the Uno.WinRT projects
// - public, operating on the public "UnoRuntimePlatform" enum, when linked into Uno.UI.Toolkit
// The "RuntimePlatform" alias below unifies the enum type name so the method bodies are shared.

#if !IS_UNO_WINRT_PROJECT
using RuntimePlatform = Uno.UI.Toolkit.UnoRuntimePlatform;
#endif

#if IS_UNO_WINRT_PROJECT
namespace Uno.Helpers;
#else
namespace Uno.UI.Toolkit;
#endif

#if IS_UNO_WINRT_PROJECT
internal static class RuntimePlatformExtensions
#else
public static class UnoRuntimePlatformExtensions
#endif
{
	/// <summary>
	/// Gets a value indicating whether the platform uses the Skia rendering backend.
	/// </summary>
	public static bool IsSkia(this RuntimePlatform platform) =>
		platform is RuntimePlatform.SkiaWin32
		or RuntimePlatform.SkiaX11
		or RuntimePlatform.SkiaMacOS
		or RuntimePlatform.SkiaIslands
		or RuntimePlatform.SkiaWasm
		or RuntimePlatform.SkiaAndroid
		or RuntimePlatform.SkiaIOS
		or RuntimePlatform.SkiaMacCatalyst
		or RuntimePlatform.SkiaTvOS
		or RuntimePlatform.SkiaFrameBuffer;

	/// <summary>
	/// Gets a value indicating whether the platform is iOS (native or Skia).
	/// </summary>
	public static bool IsIOS(this RuntimePlatform platform) => platform is RuntimePlatform.NativeIOS or RuntimePlatform.SkiaIOS;

	/// <summary>
	/// Gets a value indicating whether the platform is the Windows App SDK (WinUI on Windows).
	/// </summary>
	public static bool IsWindowsAppSdk(this RuntimePlatform platform) => platform is RuntimePlatform.NativeWinUI;

	/// <summary>
	/// Gets a value indicating whether the platform is Android (native or Skia).
	/// </summary>
	public static bool IsAndroid(this RuntimePlatform platform) => platform is RuntimePlatform.NativeAndroid or RuntimePlatform.SkiaAndroid;

	/// <summary>
	/// Gets a value indicating whether the platform is Mac Catalyst (native or Skia).
	/// </summary>
	public static bool IsMacCatalyst(this RuntimePlatform platform) => platform is RuntimePlatform.NativeMacCatalyst or RuntimePlatform.SkiaMacCatalyst;

	/// <summary>
	/// Gets a value indicating whether the platform is WebAssembly (native or Skia).
	/// </summary>
	public static bool IsWasm(this RuntimePlatform platform) => platform is RuntimePlatform.NativeWasm or RuntimePlatform.SkiaWasm;

	/// <summary>
	/// Gets a value indicating whether the platform is tvOS (native or Skia).
	/// </summary>
	public static bool IsTvOS(this RuntimePlatform platform) => platform is RuntimePlatform.NativeTvOS or RuntimePlatform.SkiaTvOS;
}
