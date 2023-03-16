using System;

namespace Microsoft.Web.WebView2.Core;

/// <summary>
/// Event args for the NavigationCompleted event.
/// </summary>
public class CoreWebView2NavigationCompletedEventArgs : EventArgs
{
	internal CoreWebView2NavigationCompletedEventArgs(
		int httpStatusCode,
		bool isSuccess,
		ulong navigationId,
		CoreWebView2WebErrorStatus webErrorStatus)
	{
		HttpStatusCode = httpStatusCode;
		IsSuccess = isSuccess;
		NavigationId = navigationId;
		WebErrorStatus = webErrorStatus;
	}

	/// <summary>
	/// The HTTP status code of the navigation if it involved an HTTP request. For instance, 
	/// this will usually be 200 if the request was successful, 404 if a page was not found, etc. 
	/// See https://developer.mozilla.org/docs/Web/HTTP/Status for a list of common status codes.
	/// </summary>
	public int HttpStatusCode { get; }

	/// <summary>
	/// true when the navigation is successful; false for a navigation that ended up in an error page 
	/// (failures due to no network, DNS lookup failure, HTTP server responds with 4xx). Note that 
	/// WebView2 will report the navigation as 'unsuccessful' if the load for the navigation did not 
	/// reach the expected completion for any reason. Such reasons include potentially catastrophic 
	/// issues such network and certificate issues, but can also be the result of intended actions such 
	/// as the app canceling a navigation or navigating away before the original navigation completed. 
	/// Applications should not just rely on this flag, but also consider the reported WebErrorStatus 
	/// to determine whether the failure is indeed catastrophic in their context.
	/// </summary>
	public bool IsSuccess { get; }

	/// <summary>
	/// Gets the ID of the navigation.
	/// </summary>
	public ulong NavigationId { get; }

	/// <summary>
	/// Gets the error code if the navigation failed.
	/// </summary>
	public CoreWebView2WebErrorStatus WebErrorStatus { get; }
}
