#nullable enable

using System.Collections.Generic;
using Microsoft.Web.WebView2.Core;

namespace Uno.UI.Xaml.Controls;

internal interface INativeWebViewEnvironment
{
	string BrowserVersionString { get; }

	string UserDataFolder { get; }

	string FailureReportFolderPath { get; }

	IReadOnlyList<CoreWebView2ProcessInfo> GetProcessInfos();
}

internal interface ISupportsWebViewEnvironmentInfo : INativeWebViewEnvironment
{
	uint BrowserProcessId { get; }
}
