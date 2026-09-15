#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using DirectN;
using Microsoft.Web.WebView2.Core;
using Uno.UI.Dispatching;
using Uno.UI.Xaml.Controls;
using Windows.Storage;

namespace Uno.UI.Runtime.Skia.Win32;

// This is a projection of the WebView2 engine COM API, not Microsoft UI XAML source.
internal sealed class Win32WebView2Environment : INativeWebViewEnvironment
{
	internal WebView2.ICoreWebView2Environment11 Environment { get; }

	private Win32WebView2Environment(WebView2.ICoreWebView2Environment environment) =>
		Environment = (WebView2.ICoreWebView2Environment11)environment;

	internal static Task<INativeWebViewEnvironment> CreateAsync(CoreWebView2Environment environment)
	{
		var completion = new TaskCompletionSource<INativeWebViewEnvironment>(TaskCreationOptions.RunContinuationsAsynchronously);
		void Create()
		{
			WebView2.CoreWebView2EnvironmentOptions? options = null;
			try
			{
				Win32WebView2Loader.Ensure();
				options = CreateOptions(environment.Options);
				var userDataFolder = string.IsNullOrEmpty(environment.RequestedUserDataFolder)
					? Path.Join(ApplicationData.Current.LocalFolder.Path, "WebView2")
					: environment.RequestedUserDataFolder;
				var handler = new WebView2.Utilities.CoreWebView2CreateCoreWebView2EnvironmentCompletedHandler((error, nativeEnvironment) =>
				{
					options.Dispose();
					if (error.IsError)
					{
						completion.TrySetException(GetException(error));
						return;
					}
					try
					{
						completion.TrySetResult(new Win32WebView2Environment(nativeEnvironment));
					}
					catch (Exception exception)
					{
						completion.TrySetException(exception);
					}
				});
				unsafe
				{
					fixed (char* browser = environment.BrowserExecutableFolder, data = userDataFolder)
					{
						var result = WebView2.Functions.CreateCoreWebView2EnvironmentWithOptions(
							new PWSTR(browser), new PWSTR(data), options, handler);
						if (result.IsError)
						{
							throw GetException(result);
						}
					}
				}
			}
			catch (Exception error)
			{
				options?.Dispose();
				completion.TrySetException(error);
			}
		}
		if (NativeDispatcher.Main.HasThreadAccess)
		{
			Create();
		}
		else
		{
			_ = NativeDispatcher.Main.EnqueueAsync(Create);
		}
		return completion.Task;
	}

	internal static Exception GetException(HRESULT error) =>
		Marshal.GetExceptionForHR(error.Value, new IntPtr(-1))
			?? new InvalidOperationException($"The WebView2 engine returned 0x{error.Value:X8}.");

	private static WebView2.CoreWebView2EnvironmentOptions CreateOptions(CoreWebView2EnvironmentOptions? settings)
	{
		var options = new WebView2.CoreWebView2EnvironmentOptions();
		try
		{
			options.put_AllowSingleSignOnUsingOSPrimaryAccount(
				(settings?.AllowSingleSignOnUsingOSPrimaryAccount
					?? FeatureConfiguration.WebView2.AllowSingleSignOnUsingOSPrimaryAccount)
					? BOOL.TRUE : BOOL.FALSE).ThrowOnError();
			if (settings is not null)
			{
				SetString(settings.AdditionalBrowserArguments, options.put_AdditionalBrowserArguments);
				SetString(settings.Language, options.put_Language);
				SetString(settings.TargetCompatibleBrowserVersion, options.put_TargetCompatibleBrowserVersion);
				options.put_ExclusiveUserDataFolderAccess(settings.ExclusiveUserDataFolderAccess ? BOOL.TRUE : BOOL.FALSE).ThrowOnError();
				options.put_IsCustomCrashReportingEnabled(settings.IsCustomCrashReportingEnabled ? BOOL.TRUE : BOOL.FALSE).ThrowOnError();
			}
			else
			{
				SetString(FeatureConfiguration.WebView2.AdditionalBrowserArguments, options.put_AdditionalBrowserArguments);
			}
			return options;
		}
		catch
		{
			options.Dispose();
			throw;
		}
	}

	private static unsafe void SetString(string? value, Func<PWSTR, HRESULT> setter)
	{
		if (!string.IsNullOrEmpty(value))
		{
			fixed (char* text = value)
			{
				setter(new PWSTR(text)).ThrowOnError();
			}
		}
	}

	public string BrowserVersionString
	{
		get
		{
			Environment.get_BrowserVersionString(out var value).ThrowOnError();
			return value.ToStringAndDispose()!;
		}
	}

	public string UserDataFolder
	{
		get
		{
			Environment.get_UserDataFolder(out var value).ThrowOnError();
			return value.ToStringAndDispose()!;
		}
	}

	public string FailureReportFolderPath
	{
		get
		{
			Environment.get_FailureReportFolderPath(out var value).ThrowOnError();
			return value.ToStringAndDispose()!;
		}
	}

	public IReadOnlyList<CoreWebView2ProcessInfo> GetProcessInfos()
	{
		Environment.GetProcessInfos(out var collection).ThrowOnError();
		uint count = 0;
		collection.get_Count(ref count).ThrowOnError();
		var result = new CoreWebView2ProcessInfo[count];
		for (uint i = 0; i < count; i++)
		{
			collection.GetValueAtIndex(i, out var item).ThrowOnError();
			int processId = 0;
			WebView2.COREWEBVIEW2_PROCESS_KIND kind = default;
			item.get_ProcessId(ref processId).ThrowOnError();
			item.get_Kind(ref kind).ThrowOnError();
			result[i] = new CoreWebView2ProcessInfo(processId, (CoreWebView2ProcessKind)(int)kind);
		}
		return result;
	}
}
