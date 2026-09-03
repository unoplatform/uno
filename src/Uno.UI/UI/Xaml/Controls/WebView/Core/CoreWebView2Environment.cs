#nullable enable

using System;
using System.Collections.Generic;
using Uno.Foundation.Extensibility;
using Uno.UI.Xaml.Controls;

namespace Microsoft.Web.WebView2.Core;

/// <summary>
/// Represents the browser environment a WebView runs under.
/// </summary>
/// <remarks>
/// The members fall into two groups with different requirements. The statics report which browser is installed
/// and must answer before any WebView exists, so they resolve through <see cref="ICoreWebView2EnvironmentStaticsExtension"/>.
/// The instance members describe the environment of a live WebView and resolve through the owning
/// <see cref="CoreWebView2"/>. Because <see cref="CreateAsync"/> is not implemented, the only way to obtain an
/// instance is <see cref="CoreWebView2.Environment"/>.
/// </remarks>
public partial class CoreWebView2Environment
{
	private const string TypeName = "Microsoft.Web.WebView2.Core.CoreWebView2Environment";

	private readonly CoreWebView2 _owner;

	internal CoreWebView2Environment(CoreWebView2 owner) => _owner = owner;

	/// <summary>
	/// Gets the version of the browser currently used by this environment.
	/// </summary>
	public string BrowserVersionString => Native(nameof(BrowserVersionString)).BrowserVersionString;

	/// <summary>
	/// Gets the user data folder this environment was created with.
	/// </summary>
	public string UserDataFolder => Native(nameof(UserDataFolder)).UserDataFolder;

	/// <summary>
	/// Gets the folder crash dumps are written to. The folder is created lazily and may not exist yet.
	/// </summary>
	public string FailureReportFolderPath => Native(nameof(FailureReportFolderPath)).FailureReportFolderPath;

	/// <summary>
	/// Gets a snapshot of the processes backing this environment.
	/// </summary>
	public IReadOnlyList<CoreWebView2ProcessInfo> GetProcessInfos() => Native("GetProcessInfos()").GetProcessInfos();

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

	private ISupportsWebViewEnvironmentInfo Native(string memberName)
		=> _owner.RequireCapability<ISupportsWebViewEnvironmentInfo>(TypeName, memberName);

	private static ICoreWebView2EnvironmentStaticsExtension Statics(string memberName)
		=> ApiExtensibility.CreateInstance<ICoreWebView2EnvironmentStaticsExtension>(typeof(CoreWebView2Environment), out var extension)
			? extension
			: throw global::Windows.Foundation.Metadata.ApiInformation.CreateNotImplementedException(TypeName, memberName);
}
