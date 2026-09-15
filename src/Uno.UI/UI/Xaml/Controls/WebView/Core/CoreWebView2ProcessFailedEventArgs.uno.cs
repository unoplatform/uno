#nullable enable

namespace Microsoft.Web.WebView2.Core;

// WebView2 engine event data; the engine projection is not part of Microsoft UI XAML.
public partial class CoreWebView2ProcessFailedEventArgs
{
	internal CoreWebView2ProcessFailedEventArgs(
		CoreWebView2ProcessFailedKind kind, CoreWebView2ProcessFailedReason reason, int exitCode, string description)
	{
		ProcessFailedKind = kind;
		Reason = reason;
		ExitCode = exitCode;
		ProcessDescription = description;
	}

	/// <summary>Gets the kind of process failure that occurred.</summary>
	public CoreWebView2ProcessFailedKind ProcessFailedKind { get; }

	/// <summary>Gets the reason for the process failure.</summary>
	public CoreWebView2ProcessFailedReason Reason { get; }

	/// <summary>Gets the exit code of the failed process.</summary>
	public int ExitCode { get; }

	/// <summary>Gets a description of the failed process.</summary>
	public string ProcessDescription { get; } = string.Empty;
}
