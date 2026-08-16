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

	private CoreWebView2? _owner;
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
		_owner = owner;
		_userDataFolder = string.Empty;
	}

	internal string? BrowserExecutableFolder { get; }

	internal string RequestedUserDataFolder => _userDataFolder;

	internal CoreWebView2EnvironmentOptions? Options { get; }

	internal void AttachOwner(CoreWebView2 owner)
	{
		if (_owner is not null && !ReferenceEquals(_owner, owner))
		{
			throw new NotSupportedException("Reusing a CoreWebView2Environment across multiple WebView2 controls is not supported.");
		}

		_owner = owner;
	}

	public string BrowserVersionString
	{
		get => _owner is null ? _browserVersionString : Native(nameof(BrowserVersionString)).BrowserVersionString;
		internal set => _browserVersionString = value;
	}

	public string UserDataFolder =>
		_owner is null ? _userDataFolder : Native(nameof(UserDataFolder)).UserDataFolder;

	public string FailureReportFolderPath => Native(nameof(FailureReportFolderPath)).FailureReportFolderPath;

	public IReadOnlyList<CoreWebView2ProcessInfo> GetProcessInfos() => Native("GetProcessInfos()").GetProcessInfos();

	public static IAsyncOperation<CoreWebView2Environment> CreateAsync() =>
		CreateWithOptionsAsync(browserExecutableFolder: null, userDataFolder: null, options: null);

	public static IAsyncOperation<CoreWebView2Environment> CreateWithOptionsAsync(
		string? browserExecutableFolder,
		string? userDataFolder,
		CoreWebView2EnvironmentOptions? options) =>
		AsyncOperation.FromTask(
			ct => Task.FromResult(new CoreWebView2Environment(browserExecutableFolder, userDataFolder, options)));

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

	private ISupportsWebViewEnvironmentInfo Native(string memberName)
		=> _owner?.RequireCapability<ISupportsWebViewEnvironmentInfo>(TypeName, memberName)
			?? throw global::Windows.Foundation.Metadata.ApiInformation.CreateNotImplementedException(TypeName, memberName);

	private static ICoreWebView2EnvironmentStaticsExtension Statics(string memberName)
		=> ApiExtensibility.CreateInstance<ICoreWebView2EnvironmentStaticsExtension>(typeof(CoreWebView2Environment), out var extension)
			? extension
			: throw global::Windows.Foundation.Metadata.ApiInformation.CreateNotImplementedException(TypeName, memberName);
}
