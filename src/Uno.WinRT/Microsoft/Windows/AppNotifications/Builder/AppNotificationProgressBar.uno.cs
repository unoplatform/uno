#nullable enable

using System;

namespace Microsoft.Windows.AppNotifications.Builder;

partial class AppNotificationProgressBar
{
	// Uno: normalize managed property values at serialization time so raw CLR setters remain safe.
	private static string NormalizeXmlAttribute(string? value) => AppNotificationBuilderUtility.EncodeXml(value);

	private void SetTitleValue(string value)
	{
		m_title = value ?? string.Empty;
		m_titleBindMode = BindMode.Value;
	}

	private void SetStatusValue(string value)
	{
		m_status = value ?? string.Empty;
		m_statusBindMode = BindMode.Value;
	}

	private void SetValueCore(double value)
	{
		if (value < 0.0 || value > 1.0)
		{
			throw new ArgumentException("The progress value must be between zero and one.", nameof(value));
		}

		m_value = value;
		m_valueBindMode = BindMode.Value;
	}

	private void SetValueStringOverrideValue(string value)
	{
		m_valueStringOverride = value ?? string.Empty;
		m_valueStringOverrideBindMode = BindMode.Value;
	}

	internal string ToXml() => ToString();
}
