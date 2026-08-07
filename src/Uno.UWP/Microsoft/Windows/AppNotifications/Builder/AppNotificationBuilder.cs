#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Windows.AppNotifications.Internal;

namespace Microsoft.Windows.AppNotifications.Builder;

public sealed class AppNotificationBuilder
{
	private readonly List<string> _textLines = new();
	private readonly List<AppNotificationButton> _buttons = new();
	private readonly List<AppNotificationProgressBar> _progressBars = new();
	private readonly List<string> _textBoxes = new();
	private readonly List<AppNotificationComboBox> _comboBoxes = new();
	private readonly SortedDictionary<string, string> _arguments = new(StringComparer.Ordinal);

	private string _timeStamp = string.Empty;
	private AppNotificationDuration _duration;
	private AppNotificationScenario _scenario;
	private string _attributionText = string.Empty;
	private string _inlineImage = string.Empty;
	private string _appLogoOverride = string.Empty;
	private string _heroImage = string.Empty;
	private string _audio = string.Empty;
	private string _tag = string.Empty;
	private string _group = string.Empty;

	public AppNotificationBuilder AddArgument(string key, string value)
	{
		if (string.IsNullOrEmpty(key))
		{
			throw new ArgumentException("An argument key is required.", nameof(key));
		}

		_arguments[AppNotificationArgumentCodec.EncodeComponent(key)] = AppNotificationArgumentCodec.EncodeComponent(value ?? string.Empty);
		return this;
	}

	public AppNotificationBuilder SetTimeStamp(DateTimeOffset value)
	{
		_timeStamp = $" displayTimestamp='{value.ToLocalTime().ToString("yyyy-MM-dd'T'HH:mm:sszzz", CultureInfo.InvariantCulture)}'";
		return this;
	}

	public AppNotificationBuilder SetScenario(AppNotificationScenario value)
	{
		_scenario = value;
		return this;
	}

	public static bool IsUrgentScenarioSupported() => true;

	public AppNotificationBuilder SetDuration(AppNotificationDuration duration)
	{
		_duration = duration;
		return this;
	}

	public AppNotificationBuilder AddText(string text)
	{
		ThrowIfMaximumReached(_textLines.Count, AppNotificationBuilderUtility.MaxTextElements, "A notification supports at most three text elements.");
		_textLines.Add($"<text>{AppNotificationBuilderUtility.EncodeXml(text)}</text>");
		return this;
	}

	public AppNotificationBuilder AddText(string text, AppNotificationTextProperties properties)
	{
		ArgumentNullException.ThrowIfNull(properties);
		ThrowIfMaximumReached(_textLines.Count, AppNotificationBuilderUtility.MaxTextElements, "A notification supports at most three text elements.");
		_textLines.Add($"{properties.ToXml()}{AppNotificationBuilderUtility.EncodeXml(text)}</text>");
		if (properties.IncomingCallAlignment)
		{
			_scenario = AppNotificationScenario.IncomingCall;
		}
		return this;
	}

	public AppNotificationBuilder SetAttributionText(string text)
	{
		_attributionText = $"<text placement='attribution'>{AppNotificationBuilderUtility.EncodeXml(text)}</text>";
		return this;
	}

	public AppNotificationBuilder SetAttributionText(string text, string language)
	{
		if (string.IsNullOrEmpty(language))
		{
			throw new ArgumentException("A language is required.", nameof(language));
		}

		_attributionText = $"<text placement='attribution' lang='{AppNotificationBuilderUtility.EncodeXml(language)}'>{AppNotificationBuilderUtility.EncodeXml(text)}</text>";
		return this;
	}

	public AppNotificationBuilder SetInlineImage(Uri imageUri)
	{
		ArgumentNullException.ThrowIfNull(imageUri);
		_inlineImage = $"<image src='{AppNotificationBuilderUtility.EncodeXml(imageUri.ToString())}'/>";
		return this;
	}

	public AppNotificationBuilder SetInlineImage(Uri imageUri, AppNotificationImageCrop imageCrop)
	{
		ArgumentNullException.ThrowIfNull(imageUri);
		var crop = imageCrop == AppNotificationImageCrop.Circle ? " hint-crop='circle'" : string.Empty;
		_inlineImage = $"<image src='{AppNotificationBuilderUtility.EncodeXml(imageUri.ToString())}'{crop}/>";
		return this;
	}

	public AppNotificationBuilder SetInlineImage(Uri imageUri, AppNotificationImageCrop imagecrop, string alternateText)
	{
		ValidateImageWithAlternateText(imageUri, alternateText);
		var crop = imagecrop == AppNotificationImageCrop.Circle ? " hint-crop='circle'" : string.Empty;
		_inlineImage = $"<image src='{AppNotificationBuilderUtility.EncodeXml(imageUri.ToString())}' alt='{AppNotificationBuilderUtility.EncodeXml(alternateText)}'{crop}/>";
		return this;
	}

	public AppNotificationBuilder SetAppLogoOverride(Uri imageUri)
	{
		ArgumentNullException.ThrowIfNull(imageUri);
		_appLogoOverride = $"<image placement='appLogoOverride' src='{AppNotificationBuilderUtility.EncodeXml(imageUri.ToString())}'/>";
		return this;
	}

	public AppNotificationBuilder SetAppLogoOverride(Uri imageUri, AppNotificationImageCrop imageCrop)
	{
		ArgumentNullException.ThrowIfNull(imageUri);
		var crop = imageCrop == AppNotificationImageCrop.Circle ? " hint-crop='circle'" : string.Empty;
		_appLogoOverride = $"<image placement='appLogoOverride' src='{AppNotificationBuilderUtility.EncodeXml(imageUri.ToString())}'{crop}/>";
		return this;
	}

	public AppNotificationBuilder SetAppLogoOverride(Uri imageUri, AppNotificationImageCrop imageCrop, string alternateText)
	{
		ValidateImageWithAlternateText(imageUri, alternateText);
		var crop = imageCrop == AppNotificationImageCrop.Circle ? " hint-crop='circle'" : string.Empty;
		_appLogoOverride = $"<image placement='appLogoOverride' src='{AppNotificationBuilderUtility.EncodeXml(imageUri.ToString())}' alt='{AppNotificationBuilderUtility.EncodeXml(alternateText)}'{crop}/>";
		return this;
	}

	public AppNotificationBuilder SetHeroImage(Uri imageUri)
	{
		ArgumentNullException.ThrowIfNull(imageUri);
		_heroImage = $"<image placement='hero' src='{AppNotificationBuilderUtility.EncodeXml(imageUri.ToString())}'/>";
		return this;
	}

	public AppNotificationBuilder SetHeroImage(Uri imageUri, string alternateText)
	{
		ValidateImageWithAlternateText(imageUri, alternateText);
		_heroImage = $"<image placement='hero' src='{AppNotificationBuilderUtility.EncodeXml(imageUri.ToString())}' alt='{AppNotificationBuilderUtility.EncodeXml(alternateText)}'/>";
		return this;
	}

	public AppNotificationBuilder SetAudioUri(Uri audioUri)
	{
		ArgumentNullException.ThrowIfNull(audioUri);
		_audio = $"<audio src='{AppNotificationBuilderUtility.EncodeXml(audioUri.ToString())}'/>";
		return this;
	}

	public AppNotificationBuilder SetAudioUri(Uri audioUri, AppNotificationAudioLooping loop)
	{
		ArgumentNullException.ThrowIfNull(audioUri);
		_audio = $"<audio src='{AppNotificationBuilderUtility.EncodeXml(audioUri.ToString())}' loop='{(loop == AppNotificationAudioLooping.Loop ? "true" : "false")}'/>";
		return this;
	}

	public AppNotificationBuilder SetAudioEvent(AppNotificationSoundEvent appNotificationSoundEvent)
	{
		_audio = $"<audio src='{AppNotificationBuilderUtility.GetSoundEventUri(appNotificationSoundEvent)}'/>";
		return this;
	}

	public AppNotificationBuilder SetAudioEvent(AppNotificationSoundEvent appNotificationSoundEvent, AppNotificationAudioLooping loop)
	{
		_audio = $"<audio src='{AppNotificationBuilderUtility.GetSoundEventUri(appNotificationSoundEvent)}' loop='{(loop == AppNotificationAudioLooping.Loop ? "true" : "false")}'/>";
		return this;
	}

	public AppNotificationBuilder MuteAudio()
	{
		_audio = "<audio silent='true'/>";
		return this;
	}

	public AppNotificationBuilder AddTextBox(string id)
	{
		ValidateInput(id);
		_textBoxes.Add($"<input id='{AppNotificationBuilderUtility.EncodeXml(id)}' type='text'/>");
		return this;
	}

	public AppNotificationBuilder AddTextBox(string id, string placeHolderText, string title)
	{
		ValidateInput(id);
		_textBoxes.Add($"<input id='{AppNotificationBuilderUtility.EncodeXml(id)}' type='text' placeHolderContent='{AppNotificationBuilderUtility.EncodeXml(placeHolderText)}' title='{AppNotificationBuilderUtility.EncodeXml(title)}'/>");
		return this;
	}

	public AppNotificationBuilder AddButton(AppNotificationButton value)
	{
		ArgumentNullException.ThrowIfNull(value);
		ThrowIfMaximumReached(_buttons.Count, AppNotificationBuilderUtility.MaxButtonElements, "A notification supports at most five buttons.");
		_buttons.Add(value);
		return this;
	}

	public AppNotificationBuilder AddProgressBar(AppNotificationProgressBar value)
	{
		ArgumentNullException.ThrowIfNull(value);
		_progressBars.Add(value);
		return this;
	}

	public AppNotificationBuilder AddComboBox(AppNotificationComboBox value)
	{
		ArgumentNullException.ThrowIfNull(value);
		ThrowIfInputMaximumReached();
		_comboBoxes.Add(value);
		return this;
	}

	public AppNotificationBuilder SetTag(string value)
	{
		_tag = value ?? string.Empty;
		return this;
	}

	public AppNotificationBuilder SetGroup(string group)
	{
		_group = group ?? string.Empty;
		return this;
	}

	public AppNotification BuildNotification()
	{
		var actions = GetActions();
		var useButtonStyle = _buttons.Exists(button => button.ButtonStyle != AppNotificationButtonStyle.Default)
			? " useButtonStyle='true'"
			: string.Empty;
		var duration = _duration == AppNotificationDuration.Default ? string.Empty : " duration='long'";
		var scenario = _scenario switch
		{
			AppNotificationScenario.Alarm => " scenario='alarm'",
			AppNotificationScenario.Reminder => " scenario='reminder'",
			AppNotificationScenario.IncomingCall => " scenario='incomingCall'",
			AppNotificationScenario.Urgent => " scenario='urgent'",
			_ => string.Empty,
		};
		var launch = _arguments.Count > 0
			? $" launch='{AppNotificationArgumentCodec.SerializeEncoded(_arguments)}'"
			: string.Empty;
		var payload = $"<toast{_timeStamp}{duration}{scenario}{launch}{useButtonStyle}><visual><binding template='ToastGeneric'>{string.Concat(_textLines)}{_attributionText}{_inlineImage}{_heroImage}{_appLogoOverride}{GetProgressBars()}</binding></visual>{_audio}{actions}</toast>";

		if (payload.Length > AppNotificationBuilderUtility.MaxPayloadCharacters)
		{
			throw new COMException("Maximum payload size exceeded.", unchecked((int)0x80004005));
		}

		return new AppNotification(payload)
		{
			Tag = _tag,
			Group = _group,
		};
	}

	private string GetActions()
	{
		if (_textBoxes.Count == 0 && _comboBoxes.Count == 0 && _buttons.Count == 0)
		{
			return string.Empty;
		}

		var actions = new StringBuilder("<actions>");
		foreach (var textBox in _textBoxes)
		{
			actions.Append(textBox);
		}
		foreach (var comboBox in _comboBoxes)
		{
			actions.Append(comboBox.ToXml());
		}
		foreach (var button in _buttons)
		{
			actions.Append(button.ToXml());
		}
		actions.Append("</actions>");
		return actions.ToString();
	}

	private string GetProgressBars()
	{
		var progressBars = new StringBuilder();
		foreach (var progressBar in _progressBars)
		{
			progressBars.Append(progressBar.ToXml());
		}
		return progressBars.ToString();
	}

	private void ValidateInput(string id)
	{
		ThrowIfInputMaximumReached();
		if (string.IsNullOrEmpty(id))
		{
			throw new ArgumentException("An input ID is required.", nameof(id));
		}
	}

	private void ThrowIfInputMaximumReached()
		=> ThrowIfMaximumReached(_textBoxes.Count + _comboBoxes.Count, AppNotificationBuilderUtility.MaxInputElements, "A notification supports at most five input elements.");

	private static void ThrowIfMaximumReached(int count, int maximum, string message)
	{
		if (count >= maximum)
		{
			throw new ArgumentException(message);
		}
	}

	private static void ValidateImageWithAlternateText(Uri imageUri, string alternateText)
	{
		ArgumentNullException.ThrowIfNull(imageUri);
		if (string.IsNullOrEmpty(alternateText))
		{
			throw new ArgumentException("Alternate text is required.", nameof(alternateText));
		}
	}
}
