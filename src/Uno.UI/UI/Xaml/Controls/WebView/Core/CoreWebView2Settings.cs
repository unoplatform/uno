#nullable enable

using System;

namespace Microsoft.Web.WebView2.Core;

/// <summary>
/// Defines properties that enable, disable, or modify WebView features.
/// </summary>
public partial class CoreWebView2Settings
{
	private string? _userAgent;
	private bool _isScriptEnabled = true;
	private bool _isZoomControlEnabled = true;

	internal CoreWebView2Settings()
	{
	}

	internal event EventHandler? UserAgentChanged;
	internal event EventHandler? IsScriptEnabledChanged;
	internal event EventHandler? IsZoomControlEnabledChanged;

	/// <summary>
	/// Determines whether communication from the host
	/// to the top-level HTML document of the WebView is allowed.
	/// </summary>
	public bool IsWebMessageEnabled { get; set; } = true;

	/// <summary>
	/// The User Agent string sent with HTTP requests and exposed via navigator.userAgent.
	/// Setting to null restores the platform default. Not supported on the WebAssembly browser host.
	/// </summary>
	public string? UserAgent
	{
		get => _userAgent;
		set
		{
			if (!string.Equals(_userAgent, value, StringComparison.Ordinal))
			{
				_userAgent = value;
				UserAgentChanged?.Invoke(this, EventArgs.Empty);
			}
		}
	}

	/// <summary>
	/// Determines whether running JavaScript is enabled in all future navigations
	/// in the WebView. Default is true.
	/// </summary>
	public bool IsScriptEnabled
	{
		get => _isScriptEnabled;
		set
		{
			if (_isScriptEnabled != value)
			{
				_isScriptEnabled = value;
				IsScriptEnabledChanged?.Invoke(this, EventArgs.Empty);
			}
		}
	}

	/// <summary>
	/// Determines whether the user can zoom the WebView via Ctrl+/Ctrl-
	/// or pinch gestures. Default is true. Not supported on every platform.
	/// </summary>
	public bool IsZoomControlEnabled
	{
		get => _isZoomControlEnabled;
		set
		{
			if (_isZoomControlEnabled != value)
			{
				_isZoomControlEnabled = value;
				IsZoomControlEnabledChanged?.Invoke(this, EventArgs.Empty);
			}
		}
	}
}
