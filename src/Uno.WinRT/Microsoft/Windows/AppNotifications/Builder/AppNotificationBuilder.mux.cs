// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

// Windows App SDK Reference dev/AppNotifications/AppNotificationBuilder/AppNotificationBuilder.cpp, commit 6b178e79e59d28efb10ef5c8c68b051d2615c3e6

#nullable enable

using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Windows.AppNotifications.Internal;
using Windows.Foundation.Metadata;

namespace Microsoft.Windows.AppNotifications.Builder;

partial class AppNotificationBuilder
{
	private void ThrowIfMaxInputItemsExceeded()
		=> ThrowIfMaximumReached(
			m_textBoxList.Count + m_comboBoxList.Count,
			AppNotificationBuilderUtility.MaxInputElements,
			"A notification supports at most five input elements.");

	public static bool IsUrgentScenarioSupported()
		=> AppNotificationBuilderUtility.IsWindows10_20H1OrGreater();

	// Adds arguments to the launch attribute to return when AppNotification is clicked.
	public AppNotificationBuilder AddArgument(string key, string value)
	{
		if (string.IsNullOrEmpty(key))
		{
			throw new ArgumentException("An argument key is required.", nameof(key));
		}

		m_arguments[AppNotificationArgumentCodec.EncodeComponent(key)] =
			AppNotificationArgumentCodec.EncodeComponent(value ?? string.Empty);
		return this;
	}

	// Sets the timeStamp of the AppNotification to when it was constructed instead of when it was sent.
	public AppNotificationBuilder SetTimeStamp(DateTimeOffset value)
	{
		m_timeStamp = $" displayTimestamp='{value.ToLocalTime().ToString("yyyy-MM-dd'T'HH:mm:sszzz", CultureInfo.InvariantCulture)}'";
		return this;
	}

	// Sets the scenario of the AppNotification.
	public AppNotificationBuilder SetScenario(AppNotificationScenario value)
	{
		m_scenario = value;
		return this;
	}

	public AppNotificationBuilder SetDuration(AppNotificationDuration duration)
	{
		m_duration = duration;
		return this;
	}

	// Text component APIs
	// Adds text to the AppNotification.
	[Overload("AddText")]
	public AppNotificationBuilder AddText(string text)
	{
		ThrowIfMaximumReached(m_textLines.Count, AppNotificationBuilderUtility.MaxTextElements, "A notification supports at most three text elements.");

		m_textLines.Add($"<text>{AppNotificationBuilderUtility.EncodeXml(text)}</text>");
		return this;
	}

	[Overload("AddText2")]
	public AppNotificationBuilder AddText(string text, AppNotificationTextProperties properties)
	{
		ArgumentNullException.ThrowIfNull(properties);
		ThrowIfMaximumReached(m_textLines.Count, AppNotificationBuilderUtility.MaxTextElements, "A notification supports at most three text elements.");

		m_textLines.Add($"{properties}{AppNotificationBuilderUtility.EncodeXml(text)}</text>");

		if (properties.IncomingCallAlignment)
		{
			m_scenario = AppNotificationScenario.IncomingCall;
		}
		return this;
	}

	[Overload("SetAttributionText")]
	public AppNotificationBuilder SetAttributionText(string text)
	{
		m_attributionText = $"<text placement='attribution'>{AppNotificationBuilderUtility.EncodeXml(text)}</text>";
		return this;
	}

	[Overload("SetAttributionText2")]
	public AppNotificationBuilder SetAttributionText(string text, string language)
	{
		if (string.IsNullOrEmpty(language))
		{
			throw new ArgumentException("A language is required.", nameof(language));
		}

		m_attributionText = $"<text placement='attribution' lang='{AppNotificationBuilderUtility.EncodeXml(language)}'>{AppNotificationBuilderUtility.EncodeXml(text)}</text>";
		return this;
	}

	// Inline image component APIs
	// Sets the full-width inline-image that appears when you expand the AppNotification
	[Overload("SetInlineImage")]
	public AppNotificationBuilder SetInlineImage(Uri imageUri)
	{
		var source = AppNotificationBuilderUtility.GetAbsoluteUri(imageUri, nameof(imageUri));
		m_inlineImage = $"<image src='{AppNotificationBuilderUtility.EncodeXml(source)}'/>";
		return this;
	}

	[Overload("SetInlineImage2")]
	public AppNotificationBuilder SetInlineImage(Uri imageUri, AppNotificationImageCrop imageCrop)
	{
		if (imageCrop == AppNotificationImageCrop.Circle)
		{
			var source = AppNotificationBuilderUtility.GetAbsoluteUri(imageUri, nameof(imageUri));
			m_inlineImage = $"<image src='{AppNotificationBuilderUtility.EncodeXml(source)}' hint-crop='circle'/>";
		}
		else
		{
			SetInlineImage(imageUri);
		}

		return this;
	}

	[Overload("SetInlineImage3")]
	public AppNotificationBuilder SetInlineImage(Uri imageUri, AppNotificationImageCrop imagecrop, string alternateText)
	{
		var source = ValidateImageWithAlternateText(imageUri, alternateText);
		var hintCrop = imagecrop == AppNotificationImageCrop.Circle ? " hint-crop='circle'" : string.Empty;
		m_inlineImage = $"<image src='{AppNotificationBuilderUtility.EncodeXml(source)}' alt='{AppNotificationBuilderUtility.EncodeXml(alternateText)}'{hintCrop}/>";

		return this;
	}

	// AppLogoOverride component APIs
	// Sets the image that replaces the app logo
	[Overload("SetAppLogoOverride")]
	public AppNotificationBuilder SetAppLogoOverride(Uri imageUri)
	{
		var source = AppNotificationBuilderUtility.GetAbsoluteUri(imageUri, nameof(imageUri));
		m_appLogoOverride = $"<image placement='appLogoOverride' src='{AppNotificationBuilderUtility.EncodeXml(source)}'/>";
		return this;
	}

	[Overload("SetAppLogoOverride2")]
	public AppNotificationBuilder SetAppLogoOverride(Uri imageUri, AppNotificationImageCrop imageCrop)
	{
		if (imageCrop == AppNotificationImageCrop.Circle)
		{
			var source = AppNotificationBuilderUtility.GetAbsoluteUri(imageUri, nameof(imageUri));
			m_appLogoOverride = $"<image placement='appLogoOverride' src='{AppNotificationBuilderUtility.EncodeXml(source)}' hint-crop='circle'/>";
		}
		else
		{
			SetAppLogoOverride(imageUri);
		}

		return this;
	}

	[Overload("SetAppLogoOverride3")]
	public AppNotificationBuilder SetAppLogoOverride(Uri imageUri, AppNotificationImageCrop imageCrop, string alternateText)
	{
		var source = ValidateImageWithAlternateText(imageUri, alternateText);
		var hintCrop = imageCrop == AppNotificationImageCrop.Circle ? " hint-crop='circle'" : string.Empty;
		m_appLogoOverride = $"<image placement='appLogoOverride' src='{AppNotificationBuilderUtility.EncodeXml(source)}' alt='{AppNotificationBuilderUtility.EncodeXml(alternateText)}'{hintCrop}/>";

		return this;
	}

	// Hero image component APIs
	// Sets the image that displays within the banner of the AppNotification.
	[Overload("SetHeroImage")]
	public AppNotificationBuilder SetHeroImage(Uri imageUri)
	{
		var source = AppNotificationBuilderUtility.GetAbsoluteUri(imageUri, nameof(imageUri));
		m_heroImage = $"<image placement='hero' src='{AppNotificationBuilderUtility.EncodeXml(source)}'/>";
		return this;
	}

	[Overload("SetHeroImage2")]
	public AppNotificationBuilder SetHeroImage(Uri imageUri, string alternateText)
	{
		var source = ValidateImageWithAlternateText(imageUri, alternateText);
		m_heroImage = $"<image placement='hero' src='{AppNotificationBuilderUtility.EncodeXml(source)}' alt='{AppNotificationBuilderUtility.EncodeXml(alternateText)}'/>";
		return this;
	}

	// SetAudio
	[Overload("SetAudioUri")]
	public AppNotificationBuilder SetAudioUri(Uri audioUri)
	{
		var source = AppNotificationBuilderUtility.GetAbsoluteUri(audioUri, nameof(audioUri));
		m_audio = $"<audio src='{AppNotificationBuilderUtility.EncodeXml(source)}'/>";
		return this;
	}

	[Overload("SetAudioUri2")]
	public AppNotificationBuilder SetAudioUri(Uri audioUri, AppNotificationAudioLooping loop)
	{
		var source = AppNotificationBuilderUtility.GetAbsoluteUri(audioUri, nameof(audioUri));
		m_audio = $"<audio src='{AppNotificationBuilderUtility.EncodeXml(source)}' loop='{(loop == AppNotificationAudioLooping.Loop ? "true" : "false")}'/>";
		return this;
	}

	[Overload("SetAudioEvent")]
	public AppNotificationBuilder SetAudioEvent(AppNotificationSoundEvent appNotificationSoundEvent)
	{
		m_audio = $"<audio src='{AppNotificationBuilderUtility.GetWinSoundEventString(appNotificationSoundEvent)}'/>";
		return this;
	}

	[Overload("SetAudioEvent2")]
	public AppNotificationBuilder SetAudioEvent(AppNotificationSoundEvent appNotificationSoundEvent, AppNotificationAudioLooping loop)
	{
		m_audio = $"<audio src='{AppNotificationBuilderUtility.GetWinSoundEventString(appNotificationSoundEvent)}' loop='{(loop == AppNotificationAudioLooping.Loop ? "true" : "false")}'/>";
		return this;
	}

	public AppNotificationBuilder MuteAudio()
	{
		m_audio = "<audio silent='true'/>";
		return this;
	}

	public AppNotificationBuilder AddProgressBar(AppNotificationProgressBar value)
	{
		ArgumentNullException.ThrowIfNull(value);
		m_progressBarList.Add(value);

		return this;
	}

	[Overload("AddTextBox")]
	public AppNotificationBuilder AddTextBox(string id)
	{
		ThrowIfMaxInputItemsExceeded();
		if (string.IsNullOrEmpty(id))
		{
			throw new ArgumentException("An input ID is required.", nameof(id));
		}

		m_textBoxList.Add($"<input id='{AppNotificationBuilderUtility.EncodeXml(id)}' type='text'/>");
		return this;
	}

	[Overload("AddTextBox2")]
	public AppNotificationBuilder AddTextBox(string id, string placeHolderText, string title)
	{
		ThrowIfMaxInputItemsExceeded();
		if (string.IsNullOrEmpty(id))
		{
			throw new ArgumentException("An input ID is required.", nameof(id));
		}

		m_textBoxList.Add($"<input id='{AppNotificationBuilderUtility.EncodeXml(id)}' type='text' placeHolderContent='{AppNotificationBuilderUtility.EncodeXml(placeHolderText)}' title='{AppNotificationBuilderUtility.EncodeXml(title)}'/>");
		return this;
	}

	// Adds a button to the AppNotificationBuilder
	public AppNotificationBuilder AddButton(AppNotificationButton value)
	{
		ArgumentNullException.ThrowIfNull(value);
		ThrowIfMaximumReached(m_buttonList.Count, AppNotificationBuilderUtility.MaxButtonElements, "A notification supports at most five buttons.");

		m_buttonList.Add(value);
		return this;
	}

	public AppNotificationBuilder AddComboBox(AppNotificationComboBox value)
	{
		ArgumentNullException.ThrowIfNull(value);
		ThrowIfMaxInputItemsExceeded();

		m_comboBoxList.Add(value);

		return this;
	}

	// AppNotification properties
	public AppNotificationBuilder SetTag(string value)
	{
		m_tag = value ?? string.Empty;
		return this;
	}

	public AppNotificationBuilder SetGroup(string group)
	{
		m_group = group ?? string.Empty;
		return this;
	}

	private string GetDuration()
		=> m_duration == AppNotificationDuration.Default ? string.Empty : " duration='long'";

	private string GetScenario()
	{
		// Add scenario attribute if set
		return m_scenario switch
		{
			AppNotificationScenario.Alarm => " scenario='alarm'",
			AppNotificationScenario.Reminder => " scenario='reminder'",
			AppNotificationScenario.IncomingCall => " scenario='incomingCall'",
			AppNotificationScenario.Urgent => " scenario='urgent'",
			_ => string.Empty,
		};
	}

	private string GetArguments()
	{
		// Add launch arguments if given arguments
		if (m_arguments.Count > 0)
		{
			return $" launch='{AppNotificationArgumentCodec.SerializeEncoded(m_arguments)}'";
		}
		else
		{
			return string.Empty;
		}
	}

	private string GetText() => string.Concat(m_textLines);

	private string GetImages() => $"{m_inlineImage}{m_heroImage}{m_appLogoOverride}";

	private string GetActions()
	{
		var result = new StringBuilder();
		foreach (var input in m_textBoxList)
		{
			result.Append(input);
		}

		foreach (var input in m_comboBoxList)
		{
			result.Append(input);
		}

		foreach (var input in m_buttonList)
		{
			if (input.ButtonStyle != AppNotificationButtonStyle.Default)
			{
				m_useButtonStyle = true;
			}

			result.Append(input);
		}

		return result.Length == 0 ? string.Empty : $"<actions>{result}</actions>";
	}

	// You must call GetActions first to retrieve this value.
	private string GetButtonStyle() => m_useButtonStyle ? " useButtonStyle='true'" : string.Empty;

	private string GetProgressBars()
	{
		var result = new StringBuilder();
		foreach (var progressBar in m_progressBarList)
		{
			result.Append(progressBar);
		}

		return result.ToString();
	}

	// Constructs a WindowsAppSDK AppNotification object with the XML payload
	public AppNotification BuildNotification()
	{
		// Build the actions string and fill m_useButtonStyle
		var actions = GetActions();

		var xmlResult = $"<toast{m_timeStamp}{GetDuration()}{GetScenario()}{GetArguments()}{GetButtonStyle()}><visual><binding template='ToastGeneric'>{GetText()}{m_attributionText}{GetImages()}{GetProgressBars()}</binding></visual>{m_audio}{actions}</toast>";

		if (xmlResult.Length > AppNotificationBuilderUtility.MaxPayloadCharacters)
		{
			throw new COMException("Maximum payload size exceeded.", unchecked((int)0x80004005));
		}

		var appNotification = new AppNotification(xmlResult)
		{
			Tag = m_tag,
			Group = m_group,
		};

		return appNotification;
	}

	// TODO Uno: AddCameraPreview and GetCameraPreview require AppNotificationConferencingConfig from the later calling-preview contract.
}
