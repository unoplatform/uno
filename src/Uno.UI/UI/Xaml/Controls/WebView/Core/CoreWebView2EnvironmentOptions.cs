#nullable enable

using System.Collections.Generic;

namespace Microsoft.Web.WebView2.Core;

/// <summary>
/// Options used to create a <see cref="CoreWebView2Environment"/>.
/// </summary>
/// <remarks>
/// The values are stored as given, with the WebView2 defaults. They have no effect unless the current platform reads them.
/// </remarks>
public partial class CoreWebView2EnvironmentOptions
{
	private string _additionalBrowserArguments = string.Empty;
	private string _language = string.Empty;
	private string _targetCompatibleBrowserVersion = string.Empty;

	/// <summary>
	/// Initializes a new instance of the <see cref="CoreWebView2EnvironmentOptions"/> class.
	/// </summary>
	public CoreWebView2EnvironmentOptions()
	{
	}

	/// <summary>
	/// Gets or sets additional command line switches passed to the browser process.
	/// </summary>
	public string AdditionalBrowserArguments
	{
		get => _additionalBrowserArguments;
		set => _additionalBrowserArguments = value ?? string.Empty;
	}

	/// <summary>
	/// Gets or sets whether single sign-on with the OS primary account is enabled.
	/// </summary>
	public bool AllowSingleSignOnUsingOSPrimaryAccount { get; set; }

	/// <summary>
	/// Gets or sets whether browser extensions can be installed.
	/// </summary>
	public bool AreBrowserExtensionsEnabled { get; set; }

	/// <summary>
	/// Gets or sets the order in which release channels are searched for a browser.
	/// </summary>
	public CoreWebView2ChannelSearchKind ChannelSearchKind { get; set; } = CoreWebView2ChannelSearchKind.MostStable;

	/// <summary>
	/// Gets or sets the custom schemes registered with the environment.
	/// </summary>
	public IList<CoreWebView2CustomSchemeRegistration> CustomSchemeRegistrations { get; set; } = new List<CoreWebView2CustomSchemeRegistration>();

	/// <summary>
	/// Gets or sets whether tracking prevention is enabled.
	/// </summary>
	public bool EnableTrackingPrevention { get; set; } = true;

	/// <summary>
	/// Gets or sets whether other processes are prevented from creating an environment with the same user data folder.
	/// </summary>
	public bool ExclusiveUserDataFolderAccess { get; set; }

	/// <summary>
	/// Gets or sets whether crash reports are kept locally instead of being sent to Microsoft.
	/// </summary>
	public bool IsCustomCrashReportingEnabled { get; set; }

	/// <summary>
	/// Gets or sets the default display language of the browser.
	/// </summary>
	public string Language
	{
		get => _language;
		set => _language = value ?? string.Empty;
	}

	/// <summary>
	/// Gets or sets the release channels searched for a browser.
	/// </summary>
	public CoreWebView2ReleaseChannels ReleaseChannels { get; set; } =
		CoreWebView2ReleaseChannels.Stable | CoreWebView2ReleaseChannels.Beta | CoreWebView2ReleaseChannels.Dev | CoreWebView2ReleaseChannels.Canary;

	/// <summary>
	/// Gets or sets the scroll bar style used by the browser.
	/// </summary>
	public CoreWebView2ScrollbarStyle ScrollBarStyle { get; set; } = CoreWebView2ScrollbarStyle.Default;

	/// <summary>
	/// Gets or sets the minimum browser version the app is compatible with.
	/// </summary>
	public string TargetCompatibleBrowserVersion
	{
		get => _targetCompatibleBrowserVersion;
		set => _targetCompatibleBrowserVersion = value ?? string.Empty;
	}
}
