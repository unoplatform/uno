#nullable enable

namespace Microsoft.Windows.AppNotifications.Builder;

partial class AppNotificationComboBox
{
	// Uno: CLR property setters and mutable dictionaries bypass the native fluent encoders.
	private static string NormalizeXmlAttribute(string? value) => AppNotificationBuilderUtility.EncodeXml(value);

	internal string ToXml() => ToString();
}
