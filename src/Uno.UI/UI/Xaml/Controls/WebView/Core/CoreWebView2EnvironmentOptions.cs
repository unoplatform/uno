#nullable enable

namespace Microsoft.Web.WebView2.Core;

public partial class CoreWebView2EnvironmentOptions
{
	public CoreWebView2EnvironmentOptions()
	{
	}

	public string? AdditionalBrowserArguments { get; set; }

	public bool AllowSingleSignOnUsingOSPrimaryAccount { get; set; }

	public bool ExclusiveUserDataFolderAccess { get; set; }

	public bool IsCustomCrashReportingEnabled { get; set; }

	public string? Language { get; set; }

	public string? TargetCompatibleBrowserVersion { get; set; }

	internal bool HasNonDefaultValues =>
		!string.IsNullOrEmpty(AdditionalBrowserArguments)
		|| AllowSingleSignOnUsingOSPrimaryAccount
		|| ExclusiveUserDataFolderAccess
		|| IsCustomCrashReportingEnabled
		|| !string.IsNullOrEmpty(Language)
		|| !string.IsNullOrEmpty(TargetCompatibleBrowserVersion);
}