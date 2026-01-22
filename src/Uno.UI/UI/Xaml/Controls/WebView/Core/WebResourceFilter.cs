#nullable enable

using System;
using System.Text.RegularExpressions;

namespace Microsoft.Web.WebView2.Core;

/// <summary>
/// Represents a filter for web resource requests.
/// Used to determine which requests should trigger the WebResourceRequested event.
/// </summary>
internal readonly struct WebResourceFilter
{
	private readonly string _uriPattern;
	private readonly CoreWebView2WebResourceContext _resourceContext;
	private readonly CoreWebView2WebResourceRequestSourceKinds _requestSourceKinds;
	private readonly Regex? _regex;

	public WebResourceFilter(string uri, CoreWebView2WebResourceContext resourceContext, CoreWebView2WebResourceRequestSourceKinds requestSourceKinds)
	{
		_uriPattern = uri;
		_resourceContext = resourceContext;
		_requestSourceKinds = requestSourceKinds;

		if (uri == "*")
		{
			_regex = null;
		}
		else
		{
			var pattern = "^" + Regex.Escape(uri)
				.Replace("\\*", ".*")
				.Replace("\\?", ".") + "$";
			_regex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
		}
	}

	/// <summary>
	/// Gets the URI pattern for this filter.
	/// </summary>
	public string UriPattern => _uriPattern;

	/// <summary>
	/// Gets the resource context for this filter.
	/// </summary>
	public CoreWebView2WebResourceContext ResourceContext => _resourceContext;

	/// <summary>
	/// Gets the request source kinds for this filter.
	/// </summary>
	public CoreWebView2WebResourceRequestSourceKinds RequestSourceKinds => _requestSourceKinds;

	/// <summary>
	/// Checks if a request matches this filter.
	/// </summary>
	public bool Matches(string url, CoreWebView2WebResourceContext context, CoreWebView2WebResourceRequestSourceKinds sourceKind)
	{
		return MatchesUrl(url) && MatchesContext(context) && MatchesSourceKind(sourceKind);
	}

	/// <summary>
	/// Checks if a URL matches this filter's pattern.
	/// </summary>
	public bool MatchesUrl(string url)
		=> _regex == null || _regex.IsMatch(url);

	/// <summary>
	/// Checks if a resource context matches this filter.
	/// </summary>
	public bool MatchesContext(CoreWebView2WebResourceContext context)
		=> _resourceContext == CoreWebView2WebResourceContext.All || _resourceContext == context;

	/// <summary>
	/// Checks if a source kind matches this filter.
	/// </summary>
	public bool MatchesSourceKind(CoreWebView2WebResourceRequestSourceKinds sourceKind)
	{
		if (_requestSourceKinds == CoreWebView2WebResourceRequestSourceKinds.All)
		{
			return true;
		}

		return (_requestSourceKinds & sourceKind) != 0;
	}

	/// <summary>
	/// Checks if this filter is equivalent to the specified parameters.
	/// Used for filter removal.
	/// </summary>
	public bool Equals(string uri, CoreWebView2WebResourceContext resourceContext, CoreWebView2WebResourceRequestSourceKinds requestSourceKinds)
	{
		return string.Equals(_uriPattern, uri, StringComparison.OrdinalIgnoreCase) &&
			   _resourceContext == resourceContext &&
			   _requestSourceKinds == requestSourceKinds;
	}
}
