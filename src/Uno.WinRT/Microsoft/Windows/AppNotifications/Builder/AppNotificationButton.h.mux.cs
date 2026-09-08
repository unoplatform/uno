// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

// Windows App SDK Reference dev/AppNotifications/AppNotificationBuilder/AppNotificationButton.h, commit 6b178e79e59d28efb10ef5c8c68b051d2615c3e6

#nullable enable

using System;
using System.Collections.Generic;

namespace Microsoft.Windows.AppNotifications.Builder;

partial class AppNotificationButton
{
	// Properties
	public string Content
	{
		get => m_content;
		set => m_content = value ?? string.Empty;
	}

	public IDictionary<string, string> Arguments
	{
		get => m_arguments;
		set => m_arguments = value ?? new Dictionary<string, string>();
	}

	public Uri? Icon
	{
		get => m_iconUri;
		set => m_iconUri = value;
	}

	public string ToolTip
	{
		get => m_toolTip;
		set => m_toolTip = value ?? string.Empty;
	}

	public bool ContextMenuPlacement
	{
		get => m_useContextMenuPlacement;
		set => m_useContextMenuPlacement = value;
	}

	public AppNotificationButtonStyle ButtonStyle
	{
		get => m_buttonStyle;
		set => m_buttonStyle = value;
	}

	public string InputId
	{
		get => m_inputId;
		set => m_inputId = value ?? string.Empty;
	}

	public Uri? InvokeUri
	{
		get => m_protocolUri;
		set => m_protocolUri = value;
	}

	public string TargetAppId
	{
		get => m_targetApplicationPfn;
		set => m_targetApplicationPfn = value ?? string.Empty;
	}

	private string m_content = string.Empty;
	private IDictionary<string, string> m_arguments = new SortedDictionary<string, string>(StringComparer.Ordinal);
	private Uri? m_iconUri;
	private Uri? m_protocolUri;
	private string m_targetApplicationPfn = string.Empty;
	private string m_toolTip = string.Empty;
	private string m_inputId = string.Empty;
	private bool m_useContextMenuPlacement;
	private AppNotificationButtonStyle m_buttonStyle = AppNotificationButtonStyle.Default;

	// TODO Uno: SetSettingStyle and m_settingType belong to the later calling-preview contract and are not projected here.
}
