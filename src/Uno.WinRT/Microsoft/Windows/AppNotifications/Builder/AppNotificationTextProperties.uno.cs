#nullable enable

namespace Microsoft.Windows.AppNotifications.Builder;

partial class AppNotificationTextProperties
{
	// Uno: normalize managed property values at serialization time so raw CLR setters remain safe.
	private static string NormalizeXmlAttribute(string? value) => AppNotificationBuilderUtility.EncodeXml(value);

	internal string ToXml() => ToString();
}
