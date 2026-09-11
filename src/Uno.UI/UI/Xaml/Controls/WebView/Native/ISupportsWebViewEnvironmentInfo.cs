#nullable enable

using System.Collections.Generic;
using Microsoft.Web.WebView2.Core;

namespace Uno.UI.Xaml.Controls;

/// <remarks>
/// Bundles two portable members (<see cref="BrowserVersionString"/>, <see cref="UserDataFolder"/>) with three
/// that only a multi-process Chromium host can answer. Kept as one interface while Win32 is the sole
/// implementer; split it the moment a WebKit-based host wants the portable half.
/// </remarks>
internal interface ISupportsWebViewEnvironmentInfo
{
	string BrowserVersionString { get; }

	string UserDataFolder { get; }

	string FailureReportFolderPath { get; }

	uint BrowserProcessId { get; }

	IReadOnlyList<CoreWebView2ProcessInfo> GetProcessInfos();
}
