#nullable enable

namespace Microsoft.Windows.AppNotifications.Builder;

public sealed class AppNotificationTextProperties
{
	private string _language = string.Empty;

	public string Language
	{
		get => _language;
		set => _language = value ?? string.Empty;
	}

	public bool IncomingCallAlignment { get; set; }

	public int MaxLines { get; set; }

	public AppNotificationTextProperties SetLanguage(string value)
	{
		Language = value ?? string.Empty;
		return this;
	}

	public AppNotificationTextProperties SetIncomingCallAlignment()
	{
		IncomingCallAlignment = true;
		return this;
	}

	public AppNotificationTextProperties SetMaxLines(int value)
	{
		MaxLines = value;
		return this;
	}

	internal string ToXml()
	{
		var language = Language.Length > 0 ? $" lang='{Language}'" : string.Empty;
		var maxLines = MaxLines != 0 ? $" hint-maxLines='{MaxLines}'" : string.Empty;
		var incomingCallAlignment = IncomingCallAlignment ? " hint-callScenarioCenterAlign='true'" : string.Empty;
		return $"<text{language}{maxLines}{incomingCallAlignment}>";
	}
}
