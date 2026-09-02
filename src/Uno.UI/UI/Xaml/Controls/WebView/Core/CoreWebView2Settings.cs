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
	/// Determines the User Agent string sent with HTTP requests and exposed via navigator.userAgent.
	/// The platform browser User Agent is used by default.
	/// </summary>
	/// <remarks>
	/// If the value is null or empty, the User Agent is not updated and the current value remains.
	/// </remarks>
	public string? UserAgent
	{
		get => _userAgent;
		set
		{
			if (!string.IsNullOrEmpty(value) && !string.Equals(_userAgent, value, StringComparison.Ordinal))
			{
				var previous = _userAgent;
				_userAgent = value;
				try
				{
					UserAgentChanged?.Invoke(this, EventArgs.Empty);
				}
				catch
				{
					_userAgent = previous;
					throw;
				}
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
				var previous = _isScriptEnabled;
				_isScriptEnabled = value;
				try
				{
					IsScriptEnabledChanged?.Invoke(this, EventArgs.Empty);
				}
				catch
				{
					_isScriptEnabled = previous;
					throw;
				}
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
				var previous = _isZoomControlEnabled;
				_isZoomControlEnabled = value;
				try
				{
					IsZoomControlEnabledChanged?.Invoke(this, EventArgs.Empty);
				}
				catch
				{
					_isZoomControlEnabled = previous;
					throw;
				}
			}
		}
	}
}
