namespace Microsoft.Web.WebView2.Core;

/// <summary>
/// Event args for the WebMessageReceived event.
/// </summary>
public partial class CoreWebView2WebMessageReceivedEventArgs
{
	internal CoreWebView2WebMessageReceivedEventArgs(string webMessageAsJson)
	{
		WebMessageAsJson = webMessageAsJson;
	}

	/// <summary>
	/// Gets the message posted from the WebView content to the host converted to a JSON string.
	/// </summary>
	public string WebMessageAsJson { get; }

	/// <summary>
	/// Gets the message posted from the WebView content to the host as a string.
	/// </summary>
	/// <returns></returns>
	public string TryGetWebMessageAsString()
	{
		throw new global::System.NotImplementedException("The member string CoreWebView2WebMessageReceivedEventArgs.TryGetWebMessageAsString() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=string%20CoreWebView2WebMessageReceivedEventArgs.TryGetWebMessageAsString%28%29");
	}
}
