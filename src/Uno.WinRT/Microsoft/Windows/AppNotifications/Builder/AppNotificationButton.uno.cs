#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.Windows.AppNotifications.Internal;

namespace Microsoft.Windows.AppNotifications.Builder;

partial class AppNotificationButton
{
	// Uno: CLR property setters and mutable dictionaries bypass the native fluent encoders.
	private static string NormalizeXmlAttribute(string? value) => AppNotificationBuilderUtility.EncodeXml(value);

	private string SerializeArguments()
	{
		var encodedArguments = new SortedDictionary<string, string>(StringComparer.Ordinal);
		foreach (var argument in m_arguments)
		{
			encodedArguments[AppNotificationArgumentCodec.EncodeComponent(argument.Key ?? string.Empty)] =
				AppNotificationArgumentCodec.EncodeComponent(argument.Value ?? string.Empty);
		}

		return AppNotificationArgumentCodec.SerializeEncoded(encodedArguments);
	}

	private AppNotificationButton SetInvokeUriCore(Uri protocolUri, string targetAppId)
	{
		_ = AppNotificationBuilderUtility.GetAbsoluteUri(protocolUri, nameof(protocolUri));
		if (m_arguments.Count > 0)
		{
			throw new ArgumentException("Arguments and protocol activation cannot be combined.", nameof(protocolUri));
		}

		m_protocolUri = protocolUri;
		m_targetApplicationPfn = targetAppId;
		return this;
	}

	internal string ToXml() => ToString();
}
