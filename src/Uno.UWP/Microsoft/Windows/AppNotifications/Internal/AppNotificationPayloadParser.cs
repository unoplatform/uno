#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Windows.AppNotifications.Builder;

namespace Microsoft.Windows.AppNotifications.Internal;

internal static class AppNotificationPayloadParser
{
	private const int MaxPayloadCharacters = 5120;

	public static AppNotificationPayload Parse(string payload)
	{
		ArgumentNullException.ThrowIfNull(payload);
		if (payload.Length > MaxPayloadCharacters)
		{
			throw new FormatException("The app notification payload exceeds 5120 characters.");
		}

		using var textReader = new StringReader(payload);
		using var xmlReader = XmlReader.Create(textReader, new XmlReaderSettings
		{
			DtdProcessing = DtdProcessing.Prohibit,
			MaxCharactersInDocument = MaxPayloadCharacters,
			XmlResolver = null,
		});
		var document = XDocument.Load(xmlReader, LoadOptions.None);
		var toast = document.Root;
		if (toast is null || toast.Name != "toast")
		{
			throw new FormatException("The app notification payload must have a toast root element.");
		}
		ValidateChildElements(toast, "visual", "audio", "actions", "header", "commands");

		var visual = GetSingleRequiredElement(toast, "visual");
		ValidateChildElements(visual, "binding");
		var binding = GetSingleRequiredElement(visual, "binding");
		ValidateChildElements(binding, "text", "image", "group", "progress");
		if (GetAttribute(binding, "template") != "ToastGeneric")
		{
			throw new FormatException("The app notification payload must contain a ToastGeneric binding.");
		}
		if (binding.Element("group") is not null)
		{
			throw new NotSupportedException("Adaptive notification groups are not supported by this payload model.");
		}
		if (toast.Element("header") is not null || toast.Element("commands") is not null)
		{
			throw new NotSupportedException("Notification headers and commands are not supported by this payload model.");
		}
		GetSingleOptionalElement(toast, "actions");
		GetSingleOptionalElement(toast, "audio");

		var language = GetOptionalAttribute(binding, "lang") ?? GetAttribute(visual, "lang");
		var baseUri = GetOptionalAttribute(binding, "baseUri") ?? GetAttribute(visual, "baseUri");
		var addImageQuery = GetOptionalBooleanAttribute(binding, "addImageQuery")
			?? GetOptionalBooleanAttribute(visual, "addImageQuery")
			?? false;

		var activationType = ParseActivationType(GetAttribute(toast, "activationType"));
		var launchArgument = GetAttribute(toast, "launch");
		var texts = ImmutableArray.CreateBuilder<AppNotificationTextData>();
		AppNotificationTextData? attribution = null;
		foreach (var text in GetElements(binding, "text"))
		{
			var parsedText = ParseText(text, language);
			var placement = GetAttribute(text, "placement");
			if (placement == "attribution")
			{
				if (attribution is not null)
				{
					throw new FormatException("A notification can contain only one attribution text element.");
				}
				attribution = parsedText;
			}
			else if (placement.Length > 0)
			{
				throw new FormatException($"Unsupported text placement '{placement}'.");
			}
			else
			{
				texts.Add(parsedText);
			}
		}

		return new AppNotificationPayload(
			launchArgument,
			DecodeArguments(launchArgument, activationType),
			ParseScenario(GetAttribute(toast, "scenario")),
			ParseDuration(GetAttribute(toast, "duration")),
			ParseTimestamp(GetAttribute(toast, "displayTimestamp")),
			GetOptionalBooleanAttribute(toast, "useButtonStyle") ?? false,
			activationType,
			GetAttribute(toast, "protocolActivationTargetApplicationPfn"),
			language,
			baseUri,
			addImageQuery,
			texts.ToImmutable(),
			attribution,
			ParseImages(binding, baseUri, addImageQuery),
			ParseProgressBars(binding),
			ParseInputs(toast),
			ParseActions(toast),
			ParseAudio(toast));
	}

	private static AppNotificationTextData ParseText(XElement text, string inheritedLanguage)
	{
		ValidateChildElements(text);
		var maxLinesValue = GetAttribute(text, "hint-maxLines");
		return new AppNotificationTextData(
			text.Value,
			GetOptionalAttribute(text, "lang") ?? inheritedLanguage,
			int.TryParse(maxLinesValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var maxLines) ? maxLines : null,
			GetOptionalBooleanAttribute(text, "hint-callScenarioCenterAlign") ?? false);
	}

	private static ImmutableArray<AppNotificationImageData> ParseImages(XElement binding, string baseUri, bool inheritedAddImageQuery)
	{
		var images = ImmutableArray.CreateBuilder<AppNotificationImageData>();
		foreach (var image in GetElements(binding, "image"))
		{
			ValidateChildElements(image);
			var placementValue = GetAttribute(image, "placement");
			var placement = placementValue switch
			{
				"" => AppNotificationImagePlacement.Inline,
				"hero" => AppNotificationImagePlacement.Hero,
				"appLogoOverride" => AppNotificationImagePlacement.AppLogoOverride,
				_ => throw new FormatException($"Unsupported image placement '{placementValue}'."),
			};
			var cropValue = GetAttribute(image, "hint-crop");
			var crop = cropValue switch
			{
				"" => AppNotificationImageCrop.Default,
				"circle" => AppNotificationImageCrop.Circle,
				_ => throw new FormatException($"Unsupported image crop '{cropValue}'."),
			};
			images.Add(new AppNotificationImageData(
				ResolveUri(GetRequiredAttribute(image, "src"), baseUri),
				GetAttribute(image, "alt"),
				placement,
				crop,
				GetOptionalBooleanAttribute(image, "addImageQuery") ?? inheritedAddImageQuery));
		}
		return images.ToImmutable();
	}

	private static ImmutableArray<AppNotificationProgressData> ParseProgressBars(XElement binding)
	{
		var progressBars = ImmutableArray.CreateBuilder<AppNotificationProgressData>();
		foreach (var progress in GetElements(binding, "progress"))
		{
			ValidateChildElements(progress);
			progressBars.Add(new AppNotificationProgressData(
				GetOptionalAttribute(progress, "title"),
				GetRequiredAttribute(progress, "status"),
				GetRequiredAttribute(progress, "value"),
				GetOptionalAttribute(progress, "valueStringOverride")));
		}
		return progressBars.ToImmutable();
	}

	private static ImmutableArray<AppNotificationInputData> ParseInputs(XElement toast)
	{
		var inputs = ImmutableArray.CreateBuilder<AppNotificationInputData>();
		var actions = GetSingleOptionalElement(toast, "actions");
		if (actions is null)
		{
			return inputs.ToImmutable();
		}
		ValidateChildElements(actions, "input", "action");
		var inputElements = GetElements(actions, "input").Take(AppNotificationBuilderUtility.MaxInputElements + 1).ToArray();
		if (inputElements.Length > AppNotificationBuilderUtility.MaxInputElements)
		{
			throw new FormatException("A notification can contain at most five input elements.");
		}

		foreach (var input in inputElements)
		{
			ValidateChildElements(input, "selection");
			var type = GetRequiredAttribute(input, "type");
			var kind = type switch
			{
				"text" => AppNotificationInputKind.Text,
				"selection" => AppNotificationInputKind.Selection,
				_ => throw new FormatException($"Unsupported input type '{type}'."),
			};
			var selectionElements = GetElements(input, "selection").Take(AppNotificationBuilderUtility.MaxSelectionElements + 1).ToArray();
			if (selectionElements.Length > AppNotificationBuilderUtility.MaxSelectionElements)
			{
				throw new FormatException("A selection input can contain at most five selections.");
			}
			if (kind == AppNotificationInputKind.Text && selectionElements.Length > 0)
			{
				throw new FormatException("A text input cannot contain selection elements.");
			}
			var selections = selectionElements
				.Select(selection => new AppNotificationSelectionData(
					GetRequiredAttribute(selection, "id"),
					GetRequiredAttribute(selection, "content")))
				.ToImmutableArray();
			foreach (var selection in selectionElements)
			{
				ValidateChildElements(selection);
			}
			inputs.Add(new AppNotificationInputData(
				GetRequiredAttribute(input, "id"),
				kind,
				GetAttribute(input, "title"),
				GetAttribute(input, "placeHolderContent"),
				GetAttribute(input, "defaultInput"),
				selections));
		}
		return inputs.ToImmutable();
	}

	private static ImmutableArray<AppNotificationActionData> ParseActions(XElement toast)
	{
		var parsedActions = ImmutableArray.CreateBuilder<AppNotificationActionData>();
		var actions = GetSingleOptionalElement(toast, "actions");
		if (actions is null)
		{
			return parsedActions.ToImmutable();
		}
		var actionElements = GetElements(actions, "action").Take(AppNotificationBuilderUtility.MaxButtonElements + 1).ToArray();
		if (actionElements.Length > AppNotificationBuilderUtility.MaxButtonElements)
		{
			throw new FormatException("A notification can contain at most five action elements.");
		}

		foreach (var action in actionElements)
		{
			ValidateChildElements(action);
			var activationType = ParseActivationType(GetAttribute(action, "activationType"));
			var rawArguments = GetRequiredAttribute(action, "arguments");
			var placement = GetAttribute(action, "placement");
			if (placement.Length > 0 && placement != "contextMenu")
			{
				throw new FormatException($"Unsupported action placement '{placement}'.");
			}
			var afterActivationBehavior = GetAttribute(action, "afterActivationBehavior");
			if (afterActivationBehavior.Length > 0 && afterActivationBehavior is not "default" and not "pendingUpdate")
			{
				throw new FormatException($"Unsupported after-activation behavior '{afterActivationBehavior}'.");
			}
			parsedActions.Add(new AppNotificationActionData(
				GetRequiredAttribute(action, "content"),
				rawArguments,
				DecodeArguments(rawArguments, activationType),
				activationType,
				GetAttribute(action, "protocolActivationTargetApplicationPfn"),
				placement == "contextMenu",
				GetAttribute(action, "imageUri"),
				GetAttribute(action, "hint-inputId"),
				ParseButtonStyle(GetAttribute(action, "hint-buttonStyle")),
				GetAttribute(action, "hint-toolTip"),
				afterActivationBehavior == "pendingUpdate"));
		}
		return parsedActions.ToImmutable();
	}

	private static AppNotificationAudioData? ParseAudio(XElement toast)
	{
		var audio = GetSingleOptionalElement(toast, "audio");
		if (audio is not null)
		{
			ValidateChildElements(audio);
		}
		return audio is null
			? null
			: new AppNotificationAudioData(
				GetAttribute(audio, "src"),
				GetOptionalBooleanAttribute(audio, "loop") ?? false,
				GetOptionalBooleanAttribute(audio, "silent") ?? false);
	}

	private static ImmutableDictionary<string, string> DecodeArguments(string value, string activationType)
		=> activationType == "protocol"
			? ImmutableDictionary<string, string>.Empty
			: AppNotificationArgumentCodec.Decode(value).ToImmutableDictionary();

	private static AppNotificationScenario ParseScenario(string value)
	{
		if (value.Length == 0)
		{
			return AppNotificationScenario.Default;
		}
		if (value == "reminder")
		{
			return AppNotificationScenario.Reminder;
		}
		if (value == "alarm")
		{
			return AppNotificationScenario.Alarm;
		}
		if (value == "incomingCall")
		{
			return AppNotificationScenario.IncomingCall;
		}
		if (value == "urgent")
		{
			return AppNotificationScenario.Urgent;
		}
		throw new FormatException($"Unsupported notification scenario '{value}'.");
	}

	private static AppNotificationDuration ParseDuration(string value)
		=> value switch
		{
			"" or "short" => AppNotificationDuration.Default,
			"long" => AppNotificationDuration.Long,
			_ => throw new FormatException($"Unsupported notification duration '{value}'."),
		};

	private static AppNotificationButtonStyle ParseButtonStyle(string value)
	{
		if (value.Length == 0)
		{
			return AppNotificationButtonStyle.Default;
		}
		if (value == "Success")
		{
			return AppNotificationButtonStyle.Success;
		}
		if (value == "Critical")
		{
			return AppNotificationButtonStyle.Critical;
		}
		throw new FormatException($"Unsupported notification button style '{value}'.");
	}

	private static DateTimeOffset? ParseTimestamp(string value)
	{
		if (value.Length == 0)
		{
			return null;
		}
		if (DateTimeOffset.TryParseExact(value, "yyyy-MM-dd'T'HH:mm:ss.FFFFFFFK", CultureInfo.InvariantCulture, DateTimeStyles.None, out var timestamp))
		{
			return timestamp;
		}
		throw new FormatException($"Invalid notification display timestamp '{value}'.");
	}

	private static string ParseActivationType(string value)
	{
		return value switch
		{
			"" or "foreground" => "foreground",
			"protocol" => "protocol",
			"background" => "background",
			"system" => "system",
			_ => throw new FormatException($"Unsupported activation type '{value}'."),
		};
	}

	private static bool? GetOptionalBooleanAttribute(XElement element, string name)
	{
		var value = GetOptionalAttribute(element, name);
		if (value is null)
		{
			return null;
		}
		if (value is "1" or "true")
		{
			return true;
		}
		if (value is "0" or "false")
		{
			return false;
		}
		throw new FormatException($"Invalid boolean value '{value}' for '{name}'.");
	}

	private static string GetAttribute(XElement element, string name)
		=> GetOptionalAttribute(element, name) ?? string.Empty;

	private static string? GetOptionalAttribute(XElement element, string name)
		=> element.Attribute(name)?.Value;

	private static string GetRequiredAttribute(XElement element, string name)
		=> GetOptionalAttribute(element, name) is { } value
			? value
			: throw new FormatException($"The '{element.Name}' element requires a '{name}' attribute.");

	private static IEnumerable<XElement> GetElements(XContainer container, string name)
		=> container.Elements(name);

	private static XElement GetSingleRequiredElement(XContainer container, string name)
		=> GetSingleOptionalElement(container, name)
			?? throw new FormatException($"The app notification payload requires a '{name}' element.");

	private static XElement? GetSingleOptionalElement(XContainer container, string name)
	{
		var elements = container.Elements(name).Take(2).ToArray();
		if (elements.Length > 1)
		{
			throw new FormatException($"The app notification payload can contain only one '{name}' element.");
		}
		return elements.FirstOrDefault();
	}

	private static string ResolveUri(string source, string baseUri)
	{
		if (baseUri.Length == 0 || !Uri.TryCreate(source, UriKind.RelativeOrAbsolute, out var sourceUri) || sourceUri.IsAbsoluteUri)
		{
			return source;
		}
		return Uri.TryCreate(baseUri, UriKind.Absolute, out var parsedBaseUri)
			? new Uri(parsedBaseUri, sourceUri).ToString()
			: throw new FormatException($"Invalid notification base URI '{baseUri}'.");
	}

	private static void ValidateChildElements(XContainer container, params string[] supportedNames)
	{
		foreach (var element in container.Elements())
		{
			if (element.Name.NamespaceName.Length > 0 || !supportedNames.Contains(element.Name.LocalName, StringComparer.Ordinal))
			{
				throw new NotSupportedException($"The '{element.Name}' notification element is not supported by this payload model.");
			}
		}
	}
}
