#nullable enable

namespace Microsoft.Web.WebView2.Core;

/// <summary>
/// Provides a snapshot of a process backing a WebView. The values are captured when the instance is created
/// and are never refreshed, matching the WinUI behaviour of CoreWebView2Environment.GetProcessInfos.
/// </summary>
public partial class CoreWebView2ProcessInfo
{
	internal CoreWebView2ProcessInfo(int processId, CoreWebView2ProcessKind kind)
	{
		ProcessId = processId;
		Kind = kind;
	}

	/// <summary>
	/// Gets the kind of the process.
	/// </summary>
	public CoreWebView2ProcessKind Kind { get; }

	/// <summary>
	/// Gets the process ID of the process.
	/// </summary>
	public int ProcessId { get; }
}
