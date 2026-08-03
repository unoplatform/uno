#nullable enable

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Windows.AppNotifications.Internal;

namespace Microsoft.Windows.AppNotifications.Builder;

public sealed class AppNotificationButton
{
	private IDictionary<string, string> _arguments = new SortedDictionary<string, string>(StringComparer.Ordinal);
	private string _content = string.Empty;
	private string _toolTip = string.Empty;
	private string _inputId = string.Empty;
	private string _targetAppId = string.Empty;

	public AppNotificationButton()
	{
	}

	public AppNotificationButton(string content)
	{
		Content = content ?? string.Empty;
	}

	public string Content
	{
		get => _content;
		set => _content = value ?? string.Empty;
	}

	public IDictionary<string, string> Arguments
	{
		get => _arguments;
		set => _arguments = value ?? new Dictionary<string, string>();
	}

	public Uri? Icon { get; set; }

	public string ToolTip
	{
		get => _toolTip;
		set => _toolTip = value ?? string.Empty;
	}

	public bool ContextMenuPlacement { get; set; }

	public AppNotificationButtonStyle ButtonStyle { get; set; }

	public string InputId
	{
		get => _inputId;
		set => _inputId = value ?? string.Empty;
	}

	public Uri? InvokeUri { get; set; }

	public string TargetAppId
	{
		get => _targetAppId;
		set => _targetAppId = value ?? string.Empty;
	}

	public AppNotificationButton AddArgument(string key, string value)
	{
		if (string.IsNullOrEmpty(key))
		{
			throw new ArgumentException("An argument key is required.", nameof(key));
		}
		if (InvokeUri is not null)
		{
			throw new ArgumentException("Arguments and protocol activation cannot be combined.", nameof(key));
		}

		_arguments[AppNotificationArgumentCodec.EncodeComponent(key)] = AppNotificationArgumentCodec.EncodeComponent(value ?? string.Empty);
		return this;
	}

	public AppNotificationButton SetIcon(Uri value)
	{
		Icon = value;
		return this;
	}

	public AppNotificationButton SetToolTip(string value)
	{
		ToolTip = AppNotificationBuilderUtility.EncodeXml(value);
		return this;
	}

	public static bool IsToolTipSupported() => true;

	public AppNotificationButton SetContextMenuPlacement()
	{
		ContextMenuPlacement = true;
		return this;
	}

	public AppNotificationButton SetButtonStyle(AppNotificationButtonStyle value)
	{
		ButtonStyle = value;
		return this;
	}

	public static bool IsButtonStyleSupported() => true;

	public AppNotificationButton SetInputId(string value)
	{
		InputId = AppNotificationBuilderUtility.EncodeXml(value);
		return this;
	}

	public AppNotificationButton SetInvokeUri(Uri protocolUri)
		=> SetInvokeUriCore(protocolUri, string.Empty);

	public AppNotificationButton SetInvokeUri(Uri protocolUri, string targetAppId)
		=> SetInvokeUriCore(protocolUri, targetAppId ?? string.Empty);

	internal string ToXml()
	{
		var xml = new StringBuilder($"<action content='{Content}'");
		if (InvokeUri is not null)
		{
			xml.Append($" arguments='{InvokeUri}' activationType='protocol'");
			if (TargetAppId.Length > 0)
			{
				xml.Append($" protocolActivationTargetApplicationPfn='{TargetAppId}'");
			}
		}
		else
		{
			xml.Append($" arguments='{AppNotificationArgumentCodec.SerializeEncoded(_arguments)}'");
		}

		if (ContextMenuPlacement)
		{
			xml.Append(" placement='contextMenu'");
		}
		if (Icon is not null)
		{
			xml.Append($" imageUri='{Icon}'");
		}
		if (InputId.Length > 0)
		{
			xml.Append($" hint-inputId='{InputId}'");
		}
		if (ButtonStyle != AppNotificationButtonStyle.Default)
		{
			xml.Append($" hint-buttonStyle='{ButtonStyle}'");
		}
		if (ToolTip.Length > 0)
		{
			xml.Append($" hint-toolTip='{ToolTip}'");
		}

		xml.Append("/>");
		return xml.ToString();
	}

	private AppNotificationButton SetInvokeUriCore(Uri protocolUri, string targetAppId)
	{
		ArgumentNullException.ThrowIfNull(protocolUri);
		if (_arguments.Count > 0)
		{
			throw new ArgumentException("Arguments and protocol activation cannot be combined.", nameof(protocolUri));
		}

		InvokeUri = protocolUri;
		TargetAppId = targetAppId;
		return this;
	}
}
