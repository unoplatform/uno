// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

// Windows App SDK Reference dev/AppNotifications/AppNotificationBuilder/AppNotificationBuilder.h, commit 6b178e79e59d28efb10ef5c8c68b051d2615c3e6

#nullable enable

using System;
using System.Collections.Generic;

namespace Microsoft.Windows.AppNotifications.Builder;

partial class AppNotificationBuilder
{
	private string m_timeStamp = string.Empty;
	private AppNotificationDuration m_duration = AppNotificationDuration.Default;
	private AppNotificationScenario m_scenario = AppNotificationScenario.Default;
	private bool m_useButtonStyle;
	private readonly List<string> m_textLines = new();
	private string m_attributionText = string.Empty;
	private string m_inlineImage = string.Empty;
	private string m_appLogoOverride = string.Empty;
	private string m_heroImage = string.Empty;
	private string m_audio = string.Empty;
	private readonly SortedDictionary<string, string> m_arguments = new(StringComparer.Ordinal);
	private readonly List<AppNotificationButton> m_buttonList = new();
	private readonly List<AppNotificationProgressBar> m_progressBarList = new();
	private readonly List<string> m_textBoxList = new();
	private readonly List<AppNotificationComboBox> m_comboBoxList = new();
	private string m_tag = string.Empty;
	private string m_group = string.Empty;

	// TODO Uno: AddCameraPreview and m_useCameraPreview belong to the later calling-preview contract and are not projected here.
}
