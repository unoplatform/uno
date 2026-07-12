#nullable enable

namespace Microsoft.Web.WebView2.Core;

public partial class CoreWebView2ControllerOptions
{
	internal CoreWebView2ControllerOptions()
	{
	}

	public bool IsInPrivateModeEnabled { get; set; }

	public string? ProfileName { get; set; }

	public string? ScriptLocale { get; set; }

	internal bool HasUnsupportedWebKitOptions =>
		!string.IsNullOrEmpty(ProfileName)
		|| !string.IsNullOrEmpty(ScriptLocale);
}