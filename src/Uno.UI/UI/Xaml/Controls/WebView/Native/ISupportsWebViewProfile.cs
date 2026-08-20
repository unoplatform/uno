#nullable enable

using Microsoft.Web.WebView2.Core;

namespace Uno.UI.Xaml.Controls;

internal interface ISupportsWebViewProfile
{
	string ProfileName { get; }

	string ProfilePath { get; }

	bool IsInPrivateModeEnabled { get; }

	string DefaultDownloadFolderPath { get; set; }

	CoreWebView2PreferredColorScheme PreferredColorScheme { get; set; }
}
