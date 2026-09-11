#if NET10_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using DirectN;

using Microsoft.Web.WebView2.Core;

using Uno.UI.Xaml.Controls;

namespace Uno.UI.Runtime.Skia.Win32;

/// <remarks>
/// Every member here is affine to the thread owning the WebView2 HWND, like the rest of this class. Calling
/// them from another thread yields RPC_E_WRONG_THREAD.
/// </remarks>
internal sealed partial class Win32NativeAotWebView : ISupportsBrowsingDataClearing, ISupportsWebViewProfile, ISupportsWebViewEnvironmentInfo
{
	private WebView2.ICoreWebView2Profile2? _profile;
	private WebView2.ICoreWebView2Environment11? _environment;

	// The core WebView is obtained as ICoreWebView2_22 (WebView2 SDK 1.0.2792), which is newer than every
	// interface used here - ICoreWebView2Profile2 and ICoreWebView2Environment11 both predate it. A runtime
	// able to create the WebView at all satisfies these casts, so there is no version ladder to walk.
	private WebView2.ICoreWebView2Profile2 NativeProfile
	{
		get
		{
			if (_profile is null)
			{
				_nativeWebView.get_Profile(out var profile).ThrowOnError();
				_profile = (WebView2.ICoreWebView2Profile2)profile;
			}

			return _profile;
		}
	}

	private WebView2.ICoreWebView2Environment11 NativeEnvironment
	{
		get
		{
			if (_environment is null)
			{
				_nativeWebView.get_Environment(out var environment).ThrowOnError();
				_environment = (WebView2.ICoreWebView2Environment11)environment;
			}

			return _environment;
		}
	}

	public uint BrowserProcessId
	{
		get
		{
			uint browserProcessId = default;
			_nativeWebView.get_BrowserProcessId(ref browserProcessId).ThrowOnError();
			return browserProcessId;
		}
	}

	public string BrowserVersionString
	{
		get
		{
			NativeEnvironment.get_BrowserVersionString(out var browserVersionString).ThrowOnError();
			return browserVersionString.ToStringAndDispose()!;
		}
	}

	public string UserDataFolder
	{
		get
		{
			NativeEnvironment.get_UserDataFolder(out var userDataFolder).ThrowOnError();
			return userDataFolder.ToStringAndDispose()!;
		}
	}

	public string FailureReportFolderPath
	{
		get
		{
			NativeEnvironment.get_FailureReportFolderPath(out var failureReportFolderPath).ThrowOnError();
			return failureReportFolderPath.ToStringAndDispose()!;
		}
	}

	public IReadOnlyList<CoreWebView2ProcessInfo> GetProcessInfos()
	{
		NativeEnvironment.GetProcessInfos(out var collection).ThrowOnError();

		uint count = default;
		collection.get_Count(ref count).ThrowOnError();

		var processInfos = new CoreWebView2ProcessInfo[count];
		for (var i = 0u; i < count; i++)
		{
			collection.GetValueAtIndex(i, out var processInfo).ThrowOnError();

			var processId = 0;
			WebView2.COREWEBVIEW2_PROCESS_KIND kind = default;
			processInfo.get_ProcessId(ref processId).ThrowOnError();
			processInfo.get_Kind(ref kind).ThrowOnError();

			processInfos[i] = new CoreWebView2ProcessInfo(processId, (CoreWebView2ProcessKind)(int)kind);
		}

		return processInfos;
	}

	public string ProfileName
	{
		get
		{
			NativeProfile.get_ProfileName(out var profileName).ThrowOnError();
			return profileName.ToStringAndDispose()!;
		}
	}

	public string ProfilePath
	{
		get
		{
			NativeProfile.get_ProfilePath(out var profilePath).ThrowOnError();
			return profilePath.ToStringAndDispose()!;
		}
	}

	public bool IsInPrivateModeEnabled
	{
		get
		{
			BOOL isInPrivateModeEnabled = default;
			NativeProfile.get_IsInPrivateModeEnabled(ref isInPrivateModeEnabled).ThrowOnError();
			return isInPrivateModeEnabled.Value != 0;
		}
	}

	public unsafe string DefaultDownloadFolderPath
	{
		get
		{
			NativeProfile.get_DefaultDownloadFolderPath(out var defaultDownloadFolderPath).ThrowOnError();
			return defaultDownloadFolderPath.ToStringAndDispose()!;
		}
		set
		{
			fixed (char* p_value = value)
			{
				NativeProfile.put_DefaultDownloadFolderPath(new PWSTR(p_value)).ThrowOnError();
			}
		}
	}

	public CoreWebView2PreferredColorScheme PreferredColorScheme
	{
		get
		{
			WebView2.COREWEBVIEW2_PREFERRED_COLOR_SCHEME preferredColorScheme = default;
			NativeProfile.get_PreferredColorScheme(ref preferredColorScheme).ThrowOnError();
			return (CoreWebView2PreferredColorScheme)(int)preferredColorScheme;
		}
		set => NativeProfile
			.put_PreferredColorScheme((WebView2.COREWEBVIEW2_PREFERRED_COLOR_SCHEME)(int)value)
			.ThrowOnError();
	}

	public Task ClearBrowsingDataAsync(CoreWebView2BrowsingDataKinds? dataKinds, DateTimeOffset? startTime, DateTimeOffset? endTime)
	{
		// RunContinuationsAsynchronously: the handler runs while the WebView2 message loop is being pumped, and
		// continuations after clearing routinely call straight back into the WebView (Reload, Navigate).
		var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var handler = new WebView2.Utilities.CoreWebView2ClearBrowsingDataCompletedHandler(errorCode =>
		{
			if (errorCode.IsError)
			{
				tcs.TrySetException(errorCode.GetException() ?? new InvalidOperationException("Failed to clear the browsing data."));
			}
			else
			{
				tcs.TrySetResult();
			}
		});

		var profile = NativeProfile;
		if (dataKinds is not { } kinds)
		{
			profile.ClearBrowsingDataAll(handler).ThrowOnError();
		}
		else if (startTime is null && endTime is null)
		{
			profile.ClearBrowsingData((WebView2.COREWEBVIEW2_BROWSING_DATA_KINDS)(int)kinds, handler).ThrowOnError();
		}
		else
		{
			profile.ClearBrowsingDataInTimeRange(
				(WebView2.COREWEBVIEW2_BROWSING_DATA_KINDS)(int)kinds,
				ToUnixSeconds(startTime ?? DateTimeOffset.UnixEpoch),
				ToUnixSeconds(endTime ?? DateTimeOffset.MaxValue),
				handler).ThrowOnError();
		}

		return tcs.Task;
	}

	// The native time range is expressed in seconds since the UNIX epoch.
	private static double ToUnixSeconds(DateTimeOffset value) => value.ToUnixTimeMilliseconds() / 1000d;
}
#endif
