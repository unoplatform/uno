namespace Microsoft.Web.WebView2.Core;

/// <summary>
/// Kind of cross-origin resource access allowed for host resources during download.
/// </summary>
public enum CoreWebView2HostResourceAccessKind
{
	/// <summary>
	/// All cross-origin resource access is denied, including normal sub-resource access as src of a script or image element.
	/// </summary>
	Deny = 0,

	/// <summary>
	/// All cross-origin resource access is allowed, including accesses that are subject to Cross-Origin Resource Sharing(CORS) check. 
	/// The behavior is similar to a website sending back the HTTP header Access-Control-Allow-Origin: *.
	/// </summary>
	Allow = 1,

	/// <summary>
	/// Cross-origin resource access is allowed for normal sub-resource access like as src of a script or image element, 
	/// while any access that subjects to CORS check will be denied. See Cross-Origin Resource Sharing for more information.
	/// </summary>
	DenyCors = 2,
}
