using System;

namespace Microsoft.Web.WebView2.Core;

/// <summary>
/// Event args for the WebMessageReceived event.
/// </summary>
public partial class CoreWebView2WebMessageReceivedEventArgs : EventArgs
{
	/// <summary>
	/// Gets the URI of the document that sent this web message.
	/// </summary>
	public string Source { get; }

	/// <summary>
	/// Gets the message posted from the WebView content to the host converted to a JSON string.
	/// </summary>
	public string WebMessageAsJson { get; }

	/// <summary>
	/// Gets the message posted from the WebView content to the host as a string.
	/// </summary>
	public bool TryGetWebMessageAsString(out string webMessageAsString)
	{
		// TODO:MZ: Implement
	}
}
