#nullable enable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Uno.Foundation.Extensibility;
using Uno.UI.Xaml.Controls;
using Windows.Foundation;

namespace Microsoft.Web.WebView2.Core;

/// <summary>
/// Represents the browser environment a WebView runs under.
/// </summary>
/// <remarks>
/// Environments created with <see cref="CreateAsync"/> or <see cref="CreateWithOptionsAsync"/> retain their
/// creation options and are attached to the owning WebView during initialization. Environment metadata then
/// resolves through that live WebView. Static browser-version queries resolve through the platform extension.
/// </remarks>
public partial class CoreWebView2Environment
{
	private const string TypeName = "Microsoft.Web.WebView2.Core.CoreWebView2Environment";

	private readonly List<WeakReference<CoreWebView2>> _owners = new();
	private INativeWebViewEnvironment? _nativeEnvironment;
	private Task<INativeWebViewEnvironment>? _nativeEnvironmentTask;
	private string _browserVersionString = string.Empty;
	private readonly string _userDataFolder;

	internal CoreWebView2Environment(string? browserExecutableFolder, string? userDataFolder, CoreWebView2EnvironmentOptions? options)
	{
		BrowserExecutableFolder = browserExecutableFolder;
		_userDataFolder = userDataFolder ?? string.Empty;
		Options = options;
	}

	internal CoreWebView2Environment(CoreWebView2 owner)
	{
		AttachOwner(owner);
		_userDataFolder = string.Empty;
	}

	internal string? BrowserExecutableFolder { get; }

	internal string RequestedUserDataFolder => _userDataFolder;

	internal CoreWebView2EnvironmentOptions? Options { get; }
	internal bool IsDefaultEnvironment { get; set; }

	internal void AttachOwner(CoreWebView2 owner)
	{
		foreach (var reference in _owners)
		{
			if (reference.TryGetTarget(out var current) && ReferenceEquals(current, owner))
			{
				return;
			}
		}
		_owners.RemoveAll(reference => !reference.TryGetTarget(out _));
		_owners.Add(new WeakReference<CoreWebView2>(owner));
	}

	public string BrowserVersionString
	{
		get => FindNativeEnvironment()?.BrowserVersionString ?? _browserVersionString;
		internal set => _browserVersionString = value;
	}

	public string UserDataFolder =>
		FindNativeEnvironment()?.UserDataFolder ?? _userDataFolder;

	public string FailureReportFolderPath => Native(nameof(FailureReportFolderPath)).FailureReportFolderPath;

	public IReadOnlyList<CoreWebView2ProcessInfo> GetProcessInfos() => Native("GetProcessInfos()").GetProcessInfos();

	public static IAsyncOperation<CoreWebView2Environment> CreateAsync() =>
		CreateWithOptionsAsync(browserExecutableFolder: null, userDataFolder: null, options: null);

	public static IAsyncOperation<CoreWebView2Environment> CreateWithOptionsAsync(
		string? browserExecutableFolder,
		string? userDataFolder,
		CoreWebView2EnvironmentOptions? options) =>
		AsyncOperation.FromTask(async ct =>
		{
			var environment = new CoreWebView2Environment(browserExecutableFolder, userDataFolder, options);
			await environment.EnsureNativeEnvironmentAsync();
			return environment;
		});

	internal async Task<INativeWebViewEnvironment?> EnsureNativeEnvironmentAsync()
	{
		if (_nativeEnvironment is not null)
		{
			return _nativeEnvironment;
		}
		if (ApiExtensibility.CreateInstance<ICoreWebView2EnvironmentStaticsExtension>(typeof(CoreWebView2Environment), out var extension))
		{
			_nativeEnvironmentTask ??= extension.CreateEnvironmentAsync(this);
			_nativeEnvironment = await _nativeEnvironmentTask;
		}
		return _nativeEnvironment;
	}

	public static string GetAvailableBrowserVersionString()
		=> Statics("GetAvailableBrowserVersionString()").GetAvailableBrowserVersionString(null);

	public static string GetAvailableBrowserVersionString(string? browserExecutableFolder)
		=> Statics("GetAvailableBrowserVersionString(string browserExecutableFolder)")
			.GetAvailableBrowserVersionString(browserExecutableFolder);

	public static int CompareBrowserVersionString(string browserVersionString1, string browserVersionString2)
	{
		if (browserVersionString1 is null)
		{
			throw new ArgumentNullException(nameof(browserVersionString1));
		}

		if (browserVersionString2 is null)
		{
			throw new ArgumentNullException(nameof(browserVersionString2));
		}

		return Statics("CompareBrowserVersionString(string browserVersionString1, string browserVersionString2)")
			.CompareBrowserVersionString(browserVersionString1, browserVersionString2);
	}

	public CoreWebView2ControllerOptions CreateCoreWebView2ControllerOptions() => new();

	public CoreWebView2PrintSettings CreatePrintSettings() => new();

	private INativeWebViewEnvironment? FindNativeEnvironment()
	{
		if (_nativeEnvironment is not null)
		{
			return _nativeEnvironment;
		}
		foreach (var reference in _owners)
		{
			if (reference.TryGetTarget(out var owner) && owner.NativeWebViewForCookies is INativeWebViewEnvironment native)
			{
				return native;
			}
		}
		return null;
	}

	private INativeWebViewEnvironment Native(string memberName)
		=> FindNativeEnvironment()
			?? throw global::Windows.Foundation.Metadata.ApiInformation.CreateNotImplementedException(TypeName, memberName);

	private static ICoreWebView2EnvironmentStaticsExtension Statics(string memberName)
		=> ApiExtensibility.CreateInstance<ICoreWebView2EnvironmentStaticsExtension>(typeof(CoreWebView2Environment), out var extension)
			? extension
			: throw global::Windows.Foundation.Metadata.ApiInformation.CreateNotImplementedException(TypeName, memberName);
}
