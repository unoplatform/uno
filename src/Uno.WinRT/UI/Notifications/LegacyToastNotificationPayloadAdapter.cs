#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Microsoft.Windows.AppNotifications;
using Windows.Data.Xml.Dom;

namespace Windows.UI.Notifications;

internal static class LegacyToastNotificationPayloadAdapter
{
	private const string LegacyTemplateAttribute = "uno-legacy-template";
	private const string LegacySecondTextAttribute = "uno-legacy-second-text";
	private static readonly HashSet<string> _legacyTemplates = new(StringComparer.Ordinal)
	{
		"ToastImageAndText01",
		"ToastImageAndText02",
		"ToastImageAndText03",
		"ToastImageAndText04",
		"ToastText01",
		"ToastText02",
		"ToastText03",
		"ToastText04",
	};

	public static string Normalize(string payload)
	{
		ArgumentNullException.ThrowIfNull(payload);

		var document = XDocument.Parse(payload, LoadOptions.PreserveWhitespace);
		var binding = document.Root?.Element("visual")?.Element("binding");
		if (binding?.Attribute("template") is { Value: var template } attribute && _legacyTemplates.Contains(template))
		{
			var texts = binding.Elements("text").ToArray();
			binding.SetAttributeValue(LegacyTemplateAttribute, template);
			if (template.EndsWith("01", StringComparison.Ordinal) && texts.Length == 1)
			{
				texts[0].AddBeforeSelf(new XElement("text"));
			}
			else if (template.EndsWith("04", StringComparison.Ordinal) && texts.Length == 3)
			{
				binding.SetAttributeValue(LegacySecondTextAttribute, texts[1].Value);
				texts[1].Value = texts[1].Value + "\n" + texts[2].Value;
			}
			attribute.Value = "ToastGeneric";
			return document.ToString(SaveOptions.DisableFormatting);
		}

		return payload;
	}

	public static string Restore(string payload)
	{
		ArgumentNullException.ThrowIfNull(payload);

		var document = XDocument.Parse(payload, LoadOptions.PreserveWhitespace);
		var binding = document.Root?.Element("visual")?.Element("binding");
		if (binding?.Attribute(LegacyTemplateAttribute) is not { Value: var template } marker)
		{
			return payload;
		}

		var texts = binding.Elements("text").ToArray();
		if (template.EndsWith("01", StringComparison.Ordinal) && texts.Length > 1)
		{
			texts[0].Remove();
		}
		else if (template.EndsWith("04", StringComparison.Ordinal) &&
			texts.Length > 1 &&
			binding.Attribute(LegacySecondTextAttribute) is { Value: var secondText })
		{
			texts[1].Value = secondText;
		}

		binding.SetAttributeValue("template", template);
		marker.Remove();
		binding.Attribute(LegacySecondTextAttribute)?.Remove();
		return document.ToString(SaveOptions.DisableFormatting);
	}

	public static AppNotification ToAppNotification(ToastNotification notification)
	{
		ArgumentNullException.ThrowIfNull(notification);

		return new AppNotification(Normalize(notification.Content.GetXml()))
		{
			Tag = notification.Tag,
			Group = notification.Group,
			Expiration = notification.ExpirationTime ?? DateTimeOffset.FromFileTime(0),
			ExpiresOnReboot = notification.ExpiresOnReboot,
			Priority = notification.Priority == ToastNotificationPriority.High
				? AppNotificationPriority.High
				: AppNotificationPriority.Default,
			SuppressDisplay = notification.SuppressPopup,
		};
	}

	public static ToastNotification FromAppNotification(AppNotification notification)
	{
		ArgumentNullException.ThrowIfNull(notification);

		var content = new XmlDocument();
		content.LoadXml(Restore(notification.Payload));
		var toast = new ToastNotification(content)
		{
			Group = notification.Group,
			ExpirationTime = notification.Expiration == DateTimeOffset.FromFileTime(0).ToLocalTime()
				? null
				: notification.Expiration,
			ExpiresOnReboot = notification.ExpiresOnReboot,
			Priority = notification.Priority == AppNotificationPriority.High
				? ToastNotificationPriority.High
				: ToastNotificationPriority.Default,
			SuppressPopup = notification.SuppressDisplay,
			AppNotificationId = notification.Id,
		};
		if (notification.Tag.Length > 0)
		{
			toast.Tag = notification.Tag;
		}
		return toast;
	}
}
