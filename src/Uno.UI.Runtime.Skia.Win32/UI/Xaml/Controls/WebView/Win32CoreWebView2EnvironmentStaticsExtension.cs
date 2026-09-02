#if NET10_0_OR_GREATER
using System;
using System.Runtime.InteropServices;

using DirectN;

using Microsoft.Web.WebView2.Core;

namespace Uno.UI.Runtime.Skia.Win32;

/// <remarks>
/// Intentionally holds no reference to <see cref="Win32NativeAotWebView"/>: these members exist to answer
/// whether a browser is installed, so a missing WebView2 runtime must surface as a failure of the individual
/// call rather than of anything created earlier.
/// </remarks>
internal class Win32CoreWebView2EnvironmentStaticsExtension : ICoreWebView2EnvironmentStaticsExtension
{
	public static Win32CoreWebView2EnvironmentStaticsExtension Instance { get; } = new();

	private Win32CoreWebView2EnvironmentStaticsExtension() { }

	public unsafe string GetAvailableBrowserVersionString(string? browserExecutableFolder)
	{
		Win32WebView2Loader.Ensure();

		PWSTR versionInfo;
		// A null pointer selects the installed browser; a non-empty folder is authoritative and the loader
		// reports ERROR_FILE_NOT_FOUND rather than falling back when no browser is found there.
		fixed (char* p_browserExecutableFolder = browserExecutableFolder)
		{
			Throw(WebView2.Functions
				.GetAvailableCoreWebView2BrowserVersionString(new PWSTR(p_browserExecutableFolder), out versionInfo));
		}

		return versionInfo.ToStringAndDispose()!;
	}

	public unsafe int CompareBrowserVersionString(string browserVersionString1, string browserVersionString2)
	{
		Win32WebView2Loader.Ensure();

		var result = 0;
		fixed (char* p_browserVersionString1 = browserVersionString1, p_browserVersionString2 = browserVersionString2)
		{
			Throw(WebView2.Functions
				.CompareBrowserVersions(new PWSTR(p_browserVersionString1), new PWSTR(p_browserVersionString2), ref result));
		}

		return result;
	}

	/// <summary>
	/// Throws the CLR's exception for a failed <paramref name="hresult"/>, rather than DirectN's
	/// <see cref="System.ComponentModel.Win32Exception"/>.
	/// </summary>
	/// <remarks>
	/// These two members are how an app asks whether a browser is installed, so the exception type is part of the
	/// contract rather than an implementation detail: WinAppSDK surfaces the CLR mapping, which turns
	/// ERROR_FILE_NOT_FOUND into <see cref="System.IO.FileNotFoundException"/>. Keeping that mapping is what makes
	/// a <c>catch (FileNotFoundException)</c> written against Windows behave the same here. The -1 ignores any
	/// IErrorInfo left on the thread by an unrelated COM call; these are plain C exports and set none.
	/// </remarks>
	private static void Throw(HRESULT hresult)
	{
		if (hresult.IsError)
		{
			Marshal.ThrowExceptionForHR(hresult.Value, new IntPtr(-1));
		}
	}
}
#endif
