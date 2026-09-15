// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

// Windows App SDK Reference dev/AppNotifications/AppNotificationBuilder/AppNotificationButton.cpp, commit 6b178e79e59d28efb10ef5c8c68b051d2615c3e6

#nullable enable

using System;
using System.Text;
using Windows.Foundation.Metadata;

namespace Microsoft.Windows.AppNotifications.Builder;

partial class AppNotificationButton
{
	public AppNotificationButton()
	{
	}

	public AppNotificationButton(string content)
	{
		m_content = content ?? string.Empty;
	}

	public static bool IsToolTipSupported()
		=> AppNotificationBuilderUtility.IsWindows10_20H1OrGreater();

	public static bool IsButtonStyleSupported()
		=> AppNotificationBuilderUtility.IsWindows10_20H1OrGreater();

	public AppNotificationButton AddArgument(string key, string value)
	{
		if (string.IsNullOrEmpty(key))
		{
			throw new ArgumentException("An argument key is required.", nameof(key));
		}
		if (m_protocolUri is not null)
		{
			throw new ArgumentException("Arguments and protocol activation cannot be combined.", nameof(key));
		}

		// Uno: keep mutable CLR map values raw and encode them once when serializing.
		m_arguments[key] = value ?? string.Empty;
		return this;
	}

	// Sets the Icon for the button.
	public AppNotificationButton SetIcon(Uri value)
	{
		_ = AppNotificationBuilderUtility.GetAbsoluteUri(value, nameof(value));
		m_iconUri = value;
		return this;
	}

	// The tooltip for a button, if the button has an empty content string.
	public AppNotificationButton SetToolTip(string value)
	{
		// Uno: keep the public property raw and encode it once when serializing.
		m_toolTip = value ?? string.Empty;
		return this;
	}

	// Sets the Button as context menu action.
	public AppNotificationButton SetContextMenuPlacement()
	{
		m_useContextMenuPlacement = true;
		return this;
	}

	// Sets the ButtonStyle to Success or Critical
	public AppNotificationButton SetButtonStyle(AppNotificationButtonStyle value)
	{
		m_buttonStyle = value;
		return this;
	}

	// Specifies the ID of an existing TextBox next to which the button will be placed.
	public AppNotificationButton SetInputId(string value)
	{
		// Uno: keep the public property raw and encode it once when serializing.
		m_inputId = value ?? string.Empty;
		return this;
	}

	// Launches the URI passed into the button when activated.
	[Overload("SetInvokeUri")]
	public AppNotificationButton SetInvokeUri(Uri protocolUri)
		=> SetInvokeUriCore(protocolUri, string.Empty);

	[Overload("SetInvokeUri2")]
	public AppNotificationButton SetInvokeUri(Uri protocolUri, string targetAppId)
		=> SetInvokeUriCore(protocolUri, targetAppId ?? string.Empty);

	private string GetActivationArguments()
	{
		if (m_protocolUri is not null)
		{
			var protocolUri = AppNotificationBuilderUtility.GetAbsoluteUri(m_protocolUri, nameof(InvokeUri));
			var protocolTargetPfn = m_targetApplicationPfn.Length > 0
				? $" protocolActivationTargetApplicationPfn='{NormalizeXmlAttribute(m_targetApplicationPfn)}'"
				: string.Empty;
			return $" arguments='{NormalizeXmlAttribute(protocolUri)}' activationType='protocol'{protocolTargetPfn}";
		}
		else
		{
			// Uno: the CLR map is mutable and may be empty, so serialize safely instead of removing a trailing delimiter.
			return $" arguments='{SerializeArguments()}'";
		}
	}

	private string GetButtonStyle()
	{
		if (m_buttonStyle == AppNotificationButtonStyle.Default)
		{
			return string.Empty;
		}

		var style = m_buttonStyle == AppNotificationButtonStyle.Success ? "Success" : "Critical";
		return $" hint-buttonStyle='{style}'";
	}

	public override string ToString()
	{
		var xmlResult = new StringBuilder($"<action content='{NormalizeXmlAttribute(m_content)}'");
		xmlResult.Append(GetActivationArguments());
		if (m_useContextMenuPlacement)
		{
			xmlResult.Append(" placement='contextMenu'");
		}
		if (m_iconUri is not null)
		{
			var iconUri = AppNotificationBuilderUtility.GetAbsoluteUri(m_iconUri, nameof(Icon));
			xmlResult.Append($" imageUri='{NormalizeXmlAttribute(iconUri)}'");
		}
		if (m_inputId.Length > 0)
		{
			xmlResult.Append($" hint-inputId='{NormalizeXmlAttribute(m_inputId)}'");
		}
		xmlResult.Append(GetButtonStyle());
		if (m_toolTip.Length > 0)
		{
			xmlResult.Append($" hint-toolTip='{NormalizeXmlAttribute(m_toolTip)}'");
		}
		xmlResult.Append("/>");

		return xmlResult.ToString();
	}

	// TODO Uno: SetSettingStyle and GetSettingStyle require AppNotificationConferencingConfig from the later calling-preview contract.
}
