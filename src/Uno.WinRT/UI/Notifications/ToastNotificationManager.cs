#nullable enable

using System;
using Windows.Data.Xml.Dom;

namespace Windows.UI.Notifications;

public static partial class ToastNotificationManager
{
	private static readonly ToastNotificationHistory _history = new();

	public static ToastNotificationHistory History => _history;

	public static ToastNotifier CreateToastNotifier() => new();

	public static XmlDocument GetTemplateContent(ToastTemplateType type)
	{
		var (textCount, includesImage) = type switch
		{
			ToastTemplateType.ToastImageAndText01 => (1, true),
			ToastTemplateType.ToastImageAndText02 => (2, true),
			ToastTemplateType.ToastImageAndText03 => (2, true),
			ToastTemplateType.ToastImageAndText04 => (3, true),
			ToastTemplateType.ToastText01 => (1, false),
			ToastTemplateType.ToastText02 => (2, false),
			ToastTemplateType.ToastText03 => (2, false),
			ToastTemplateType.ToastText04 => (3, false),
			_ => throw new ArgumentOutOfRangeException(nameof(type)),
		};

		var document = new XmlDocument();
		var toast = document.CreateElement("toast");
		document.AppendChild(toast);
		var visual = document.CreateElement("visual");
		toast.AppendChild(visual);
		var binding = document.CreateElement("binding");
		binding.SetAttribute("template", type.ToString());
		visual.AppendChild(binding);

		if (includesImage)
		{
			var image = document.CreateElement("image");
			image.SetAttribute("id", "1");
			image.SetAttribute("src", string.Empty);
			binding.AppendChild(image);
		}

		for (var index = 1; index <= textCount; index++)
		{
			var text = document.CreateElement("text");
			text.SetAttribute("id", index.ToString(global::System.Globalization.CultureInfo.InvariantCulture));
			binding.AppendChild(text);
		}

		return document;
	}
}
