namespace Microsoft.Web.WebView2.Core;

/// <summary>
/// Specifies the process failure kind used in CoreWebView2ProcessFailedEventArgs.
/// </summary>
public enum CoreWebView2ProcessFailedKind
{
	/// <summary>
	/// Indicates that the browser process ended unexpectedly. 
	/// The WebView automatically moves to the Closed state. 
	/// The app has to recreate a new WebView to recover from this failure.
	/// </summary>
	BrowserProcessExited = 0,

	/// <summary>
	/// Indicates that the main frame's render process ended unexpectedly. 
	/// A new render process is created automatically and navigated to an error page. 
	/// You can use the Reload() method to try reload the page that failed.
	/// </summary>
	RenderProcessExited = 1,

	/// <summary>
	/// Indicates that the main frame's render process is unresponsive.
	/// </summary>
	RenderProcessUnresponsive = 2,

	/// <summary>
	/// Indicates that a frame-only render process ended unexpectedly. 
	/// The process exit does not affect the top-level document, only a subset of the subframes within it. 
	/// The content in these frames is replaced with an error page in the frame.
	/// </summary>
	FrameRenderProcessExited = 3,

	/// <summary>
	/// Indicates that a utility process ended unexpectedly.
	/// </summary>
	UtilityProcessExited = 4,

	/// <summary>
	/// Indicates that a sandbox helper process ended unexpectedly.
	/// </summary>
	SandboxHelperProcessExited = 5,

	/// <summary>
	/// Indicates that the GPU process ended unexpectedly.
	/// </summary>
	GpuProcessExited = 6,

	/// <summary>
	/// Indicates that a PPAPI plugin process ended unexpectedly.
	/// </summary>
	PpapiPluginProcessExited = 7,

	/// <summary>
	/// Indicates that a PPAPI plugin broker process ended unexpectedly.
	/// </summary>
	PpapiBrokerProcessExited = 8,

	/// <summary>
	/// Indicates that a process of unspecified kind ended unexpectedly.
	/// </summary>
	UnknownProcessExited = 9
}
