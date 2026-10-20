#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Uno.Foundation.Extensibility;
using Uno.UI.Xaml.Controls;
using Windows.Foundation;
using Windows.Storage.Streams;

namespace Microsoft.Web.WebView2.Core;

/// <summary>
/// Represents the browser environment a WebView runs under.
/// </summary>
/// <remarks>
/// The members fall into two groups with different requirements. The statics report which browser is installed
/// and must answer before any WebView exists, so they resolve through <see cref="ICoreWebView2EnvironmentStaticsExtension"/>.
/// The instance members describe the environment of a live WebView and resolve through the owning
/// <see cref="CoreWebView2"/>. An environment created by <see cref="CreateAsync"/> or <see cref="CreateWithOptionsAsync"/>
/// has no owner until a WebView2 adopts it through <c>EnsureCoreWebView2Async(CoreWebView2Environment)</c>; until then
/// it only reports the inputs it was created with.
/// </remarks>
public partial class CoreWebView2Environment
{
	private const string TypeName = "Microsoft.Web.WebView2.Core.CoreWebView2Environment";

	private readonly bool _isDetached;
	private readonly string? _userDataFolder;
	private CoreWebView2? _owner;
	private string _browserVersionString = string.Empty;

	internal CoreWebView2Environment(CoreWebView2 owner) => _owner = owner;

	internal CoreWebView2Environment(string? browserExecutableFolder, string? userDataFolder, CoreWebView2EnvironmentOptions? options)
	{
		_isDetached = true;
		BrowserExecutableFolder = browserExecutableFolder;
		_userDataFolder = userDataFolder;
		Options = options;
	}

	internal string? BrowserExecutableFolder { get; }

	internal CoreWebView2EnvironmentOptions? Options { get; }

	/// <summary>
	/// Gets the version of the browser currently used by this environment.
	/// </summary>
	public string BrowserVersionString
		=> UseDetachedValues ? _browserVersionString : Native(nameof(BrowserVersionString)).BrowserVersionString;

	/// <summary>
	/// Gets the user data folder this environment was created with.
	/// </summary>
	public string UserDataFolder
		=> UseDetachedValues ? _userDataFolder ?? string.Empty : Native(nameof(UserDataFolder)).UserDataFolder;

	/// <summary>
	/// Gets the folder crash dumps are written to. The folder is created lazily and may not exist yet.
	/// </summary>
	public string FailureReportFolderPath => Native(nameof(FailureReportFolderPath)).FailureReportFolderPath;

	/// <summary>
	/// Gets a snapshot of the processes backing this environment.
	/// </summary>
	public IReadOnlyList<CoreWebView2ProcessInfo> GetProcessInfos() => Native("GetProcessInfos()").GetProcessInfos();

	/// <summary>
	/// Creates an environment that uses the installed browser and the default user data folder.
	/// </summary>
	/// <exception cref="System.IO.FileNotFoundException">The platform reports that no browser runtime is installed.</exception>
	public static IAsyncOperation<CoreWebView2Environment> CreateAsync()
		=> CreateWithOptionsAsync(null, null, null);

	/// <summary>
	/// Creates an environment that records <paramref name="browserExecutableFolder"/>, <paramref name="userDataFolder"/>
	/// and <paramref name="options"/>.
	/// </summary>
	/// <remarks>
	/// The environment does not start a browser. Platforms that can report the installed browser are probed first, so
	/// a missing runtime fails here as it does on Windows; other platforms skip the probe. The options are only honored
	/// by platforms that read them.
	/// </remarks>
	/// <exception cref="System.IO.FileNotFoundException">No browser runtime was found in <paramref name="browserExecutableFolder"/>, or none is installed.</exception>
	public static IAsyncOperation<CoreWebView2Environment> CreateWithOptionsAsync(
		string? browserExecutableFolder,
		string? userDataFolder,
		CoreWebView2EnvironmentOptions? options)
		=> AsyncOperation.FromTask(ct =>
		{
			var environment = new CoreWebView2Environment(browserExecutableFolder, userDataFolder, options);

			if (TryGetStatics(out var statics))
			{
				environment._browserVersionString = statics.GetAvailableBrowserVersionString(browserExecutableFolder) ?? string.Empty;
			}

			return Task.FromResult(environment);
		});

	/// <summary>
	/// Creates a response that can be assigned to <see cref="CoreWebView2WebResourceRequestedEventArgs.Response"/>.
	/// </summary>
	/// <param name="Content">The response body, or null for an empty body.</param>
	/// <param name="StatusCode">The HTTP status code.</param>
	/// <param name="ReasonPhrase">The HTTP reason phrase.</param>
	/// <param name="Headers">CRLF-separated <c>Name: Value</c> pairs.</param>
	// The PascalCase parameter names match the WinUI signature.
	public CoreWebView2WebResourceResponse CreateWebResourceResponse(
		IRandomAccessStream? Content,
		int StatusCode,
		string? ReasonPhrase,
		string? Headers)
		=> new(new ManagedWebResourceResponse(Content, StatusCode, ReasonPhrase, ManagedHttpResponseHeaders.Parse(Headers)));

	/// <summary>
	/// Gets the version of the installed browser, or of the browser in the default location.
	/// </summary>
	/// <exception cref="NotImplementedException">The current platform cannot report a browser version.</exception>
	public static string GetAvailableBrowserVersionString()
		=> Statics("GetAvailableBrowserVersionString()").GetAvailableBrowserVersionString(null);

	/// <summary>
	/// Gets the version of the browser in <paramref name="browserExecutableFolder"/>, or of the installed
	/// browser when it is null or empty.
	/// </summary>
	/// <remarks>
	/// A non-empty <paramref name="browserExecutableFolder"/> is authoritative: when no browser is found there,
	/// the call fails rather than falling back to the installed one.
	/// </remarks>
	public static string GetAvailableBrowserVersionString(string? browserExecutableFolder)
		=> Statics("GetAvailableBrowserVersionString(string browserExecutableFolder)")
			.GetAvailableBrowserVersionString(browserExecutableFolder);

	/// <summary>
	/// Gets the version of the browser in <paramref name="browserExecutableFolder"/>, or of the installed
	/// browser when it is null or empty.
	/// </summary>
	/// <remarks>The release channel selection in <paramref name="options"/> is not honored.</remarks>
	public static string GetAvailableBrowserVersionString(string? browserExecutableFolder, CoreWebView2EnvironmentOptions? options)
		=> Statics("GetAvailableBrowserVersionString(string browserExecutableFolder, CoreWebView2EnvironmentOptions options)")
			.GetAvailableBrowserVersionString(browserExecutableFolder);

	/// <summary>
	/// Compares two browser version strings, returning a negative value, zero, or a positive value when
	/// <paramref name="browserVersionString1"/> is respectively older than, the same as, or newer than
	/// <paramref name="browserVersionString2"/>.
	/// </summary>
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

	/// <summary>
	/// Binds a detached environment to the first WebView that adopts it, so the instance members report that WebView.
	/// </summary>
	internal void Adopt(CoreWebView2 owner) => _owner ??= owner;

	/// <summary>
	/// A detached environment answers from its inputs until its owner has a native WebView that reports them.
	/// </summary>
	private bool UseDetachedValues
		=> _isDetached && _owner?.TryGetCapability<ISupportsWebViewEnvironmentInfo>() is null;

	private ISupportsWebViewEnvironmentInfo Native(string memberName)
		=> _owner is not null
			? _owner.RequireCapability<ISupportsWebViewEnvironmentInfo>(TypeName, memberName)
			: throw global::Windows.Foundation.Metadata.ApiInformation.CreateNotImplementedException(TypeName, memberName);

	private static bool TryGetStatics([NotNullWhen(true)] out ICoreWebView2EnvironmentStaticsExtension? statics)
		=> ApiExtensibility.CreateInstance(typeof(CoreWebView2Environment), out statics);

	private static ICoreWebView2EnvironmentStaticsExtension Statics(string memberName)
		=> TryGetStatics(out var extension)
			? extension
			: throw global::Windows.Foundation.Metadata.ApiInformation.CreateNotImplementedException(TypeName, memberName);
}
