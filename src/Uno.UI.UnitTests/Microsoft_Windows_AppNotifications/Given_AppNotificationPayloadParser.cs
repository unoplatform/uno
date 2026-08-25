#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Windows.AppNotifications.Builder;
using Microsoft.Windows.AppNotifications.Internal;

namespace Uno.UI.Tests.Microsoft_Windows_AppNotifications;

[TestClass]
public class Given_AppNotificationPayloadParser
{
	private static readonly Uri ImageUri = new("https://example.com/image.png");

	[TestMethod]
	public void When_Raw_ToastGeneric_Is_Parsed_All_Fields_Are_Normalized()
	{
		const string payload = """
			<toast launch="action=open%3Bitem;id=42" duration="long" scenario="incomingCall" displayTimestamp="2026-08-03T10:20:30+05:30" useButtonStyle="true">
			  <visual>
			    <binding template="ToastGeneric">
			      <text lang="en-US" hint-maxLines="1" hint-callScenarioCenterAlign="true">Title &amp; more</text>
			      <text>Body</text>
			      <text placement="attribution" lang="fr-FR">Source</text>
			      <image src="https://example.com/inline.png" alt="Inline" hint-crop="circle" />
			      <image placement="hero" src="https://example.com/hero.png" alt="Hero" />
			      <progress title="Download" status="Running" value="0.5" valueStringOverride="50%" />
			    </binding>
			  </visual>
			  <audio src="ms-winsoundevent:Notification.Reminder" loop="true" />
			  <actions>
			    <input id="reply" type="text" placeHolderContent="Reply" title="Response" />
			    <input id="choice" type="selection" title="Choice" defaultInput="yes">
			      <selection id="yes" content="Yes" />
			    </input>
			    <action content="Send" arguments="action=reply%3Bnow" placement="contextMenu" imageUri="https://example.com/send.png" hint-inputId="reply" hint-buttonStyle="Success" hint-toolTip="Send reply" />
			  </actions>
			</toast>
			""";

		var result = AppNotificationPayloadParser.Parse(payload);

		Assert.AreEqual("Title & more", result.Title?.Content);
		Assert.AreEqual("Body", result.Body?.Content);
		Assert.AreEqual("Source", result.Attribution?.Content);
		Assert.AreEqual("open;item", result.LaunchArguments["action"]);
		Assert.AreEqual("42", result.LaunchArguments["id"]);
		Assert.AreEqual(AppNotificationScenario.IncomingCall, result.Scenario);
		Assert.AreEqual(AppNotificationDuration.Long, result.Duration);
		Assert.AreEqual(new DateTimeOffset(2026, 8, 3, 10, 20, 30, TimeSpan.FromHours(5.5)), result.DisplayTimestamp);
		Assert.IsTrue(result.UseButtonStyle);
		Assert.AreEqual(2, result.Images.Length);
		Assert.AreEqual(AppNotificationImagePlacement.Inline, result.Images[0].Placement);
		Assert.AreEqual(AppNotificationImageCrop.Circle, result.Images[0].Crop);
		Assert.AreEqual("0.5", result.ProgressBars[0].Value);
		Assert.AreEqual(2, result.Inputs.Length);
		Assert.AreEqual(AppNotificationInputKind.Text, result.Inputs[0].Kind);
		Assert.AreEqual("yes", result.Inputs[1].Selections[0].Id);
		Assert.AreEqual("reply;now", result.Actions[0].Arguments["action"]);
		Assert.IsTrue(result.Actions[0].ContextMenuPlacement);
		Assert.AreEqual(AppNotificationButtonStyle.Success, result.Actions[0].ButtonStyle);
		Assert.AreEqual("ms-winsoundevent:Notification.Reminder", result.Audio?.Source);
		Assert.IsTrue(result.Audio?.Loop);
	}

	[TestMethod]
	public void When_Builder_And_Raw_Xml_Are_Equivalent_Models_Match()
	{
		var builderPayload = new AppNotificationBuilder()
			.AddArgument("action", "open;item")
			.AddArgument("id", "42")
			.SetDuration(AppNotificationDuration.Long)
			.SetScenario(AppNotificationScenario.IncomingCall)
			.AddText("Title & more", new AppNotificationTextProperties().SetLanguage("en-US").SetMaxLines(1).SetIncomingCallAlignment())
			.AddText("Body")
			.SetAttributionText("Source", "fr-FR")
			.SetInlineImage(ImageUri, AppNotificationImageCrop.Circle, "Inline")
			.SetHeroImage(ImageUri, "Hero")
			.AddProgressBar(new AppNotificationProgressBar().SetTitle("Download").SetStatus("Running").SetValue(0.5).SetValueStringOverride("50%"))
			.SetAudioEvent(AppNotificationSoundEvent.Reminder, AppNotificationAudioLooping.Loop)
			.AddTextBox("reply", "Reply", "Response")
			.AddComboBox(new AppNotificationComboBox("choice").AddItem("yes", "Yes").SetTitle("Choice").SetSelectedItem("yes"))
			.AddButton(new AppNotificationButton("Send").AddArgument("action", "reply;now").SetContextMenuPlacement().SetIcon(ImageUri).SetInputId("reply").SetButtonStyle(AppNotificationButtonStyle.Success).SetToolTip("Send reply"))
			.BuildNotification()
			.Payload;
		const string rawPayload = """
			<toast useButtonStyle="true" scenario="incomingCall" duration="long" launch="action=open%3Bitem;id=42">
			  <visual><binding template="ToastGeneric">
			    <text hint-callScenarioCenterAlign="true" hint-maxLines="1" lang="en-US">Title &amp; more</text>
			    <text>Body</text>
			    <text lang="fr-FR" placement="attribution">Source</text>
			    <image hint-crop="circle" alt="Inline" src="https://example.com/image.png" />
			    <image alt="Hero" src="https://example.com/image.png" placement="hero" />
			    <progress valueStringOverride="50%" value="0.5" status="Running" title="Download" />
			  </binding></visual>
			  <audio loop="true" src="ms-winsoundevent:Notification.Reminder" />
			  <actions>
			    <input title="Response" placeHolderContent="Reply" type="text" id="reply" />
			    <input defaultInput="yes" title="Choice" type="selection" id="choice"><selection content="Yes" id="yes" /></input>
			    <action hint-toolTip="Send reply" hint-buttonStyle="Success" hint-inputId="reply" imageUri="https://example.com/image.png" placement="contextMenu" arguments="action=reply%3Bnow" content="Send" />
			  </actions>
			</toast>
			""";

		var builderResult = AppNotificationPayloadParser.Parse(builderPayload);
		var rawResult = AppNotificationPayloadParser.Parse(rawPayload);

		AssertPayloadsAreEquivalent(builderResult, rawResult);
	}

	[TestMethod]
	public void When_Protocol_Activation_Is_Parsed_Uri_Is_Not_Decoded_As_Arguments()
	{
		const string payload = """
			<toast launch="https://example.com/open?a=b" activationType="protocol" protocolActivationTargetApplicationPfn="Target.App">
			  <visual><binding template="ToastGeneric" /></visual>
			  <actions><action content="Open" arguments="https://example.com/item?a=b" activationType="protocol" /></actions>
			</toast>
			""";

		var result = AppNotificationPayloadParser.Parse(payload);

		Assert.AreEqual("protocol", result.ActivationType);
		Assert.AreEqual("Target.App", result.ProtocolActivationTargetApplicationPfn);
		Assert.AreEqual(0, result.LaunchArguments.Count);
		Assert.AreEqual("https://example.com/item?a=b", result.Actions[0].RawArguments);
		Assert.AreEqual(0, result.Actions[0].Arguments.Count);
	}

	[TestMethod]
	public void When_Visual_Context_Is_Inherited_It_Is_Normalized()
	{
		const string payload = """
			<toast>
			  <visual lang="en-US" baseUri="https://cdn.example/assets/" addImageQuery="true">
			    <binding template="ToastGeneric" lang="fr-FR">
			      <text>Title</text>
			      <text lang="de-DE">Body</text>
			      <image src="hero.png" />
			      <image src="logo.png" addImageQuery="false" />
			    </binding>
			  </visual>
			  <actions><action content="Wait" arguments="action=wait" afterActivationBehavior="pendingUpdate" /></actions>
			</toast>
			""";

		var result = AppNotificationPayloadParser.Parse(payload);

		Assert.AreEqual("fr-FR", result.Language);
		Assert.AreEqual("https://cdn.example/assets/", result.BaseUri);
		Assert.IsTrue(result.AddImageQuery);
		Assert.AreEqual("fr-FR", result.Texts[0].Language);
		Assert.AreEqual("de-DE", result.Texts[1].Language);
		Assert.AreEqual("https://cdn.example/assets/hero.png", result.Images[0].Source);
		Assert.IsTrue(result.Images[0].AddImageQuery);
		Assert.IsFalse(result.Images[1].AddImageQuery);
		Assert.IsTrue(result.Actions[0].PendingUpdate);
	}

	[TestMethod]
	public void When_Schema_Values_Are_Invalid_Parser_Throws()
	{
		Assert.ThrowsExactly<FormatException>(() => AppNotificationPayloadParser.Parse("<toast scenario='future'><visual><binding template='ToastGeneric'/></visual></toast>"));
		Assert.ThrowsExactly<FormatException>(() => AppNotificationPayloadParser.Parse("<toast duration='medium'><visual><binding template='ToastGeneric'/></visual></toast>"));
		Assert.ThrowsExactly<FormatException>(() => AppNotificationPayloadParser.Parse("<toast useButtonStyle='yes'><visual><binding template='ToastGeneric'/></visual></toast>"));
		Assert.ThrowsExactly<FormatException>(() => AppNotificationPayloadParser.Parse("<toast displayTimestamp='August 3, 2026'><visual><binding template='ToastGeneric'/></visual></toast>"));
		Assert.ThrowsExactly<FormatException>(() => AppNotificationPayloadParser.Parse("<toast><visual><binding template='ToastGeneric'><image src='image.png' placement='banner'/></binding></visual></toast>"));
		Assert.ThrowsExactly<FormatException>(() => AppNotificationPayloadParser.Parse("<toast><visual><binding template='ToastGeneric'/></visual><actions><action content='Open' arguments='' hint-buttonStyle='success'/></actions></toast>"));
	}

	[TestMethod]
	public void When_Namespaces_Or_Singleton_Cardinality_Are_Invalid_Parser_Throws()
	{
		const string namespaced = "<x:toast xmlns:x='urn:test'><x:visual><x:binding template='ToastGeneric'/></x:visual></x:toast>";
		const string duplicateVisual = "<toast><visual><binding template='ToastGeneric'/></visual><visual><binding template='ToastGeneric'/></visual></toast>";

		Assert.ThrowsExactly<FormatException>(() => AppNotificationPayloadParser.Parse(namespaced));
		Assert.ThrowsExactly<FormatException>(() => AppNotificationPayloadParser.Parse(duplicateVisual));
	}

	[TestMethod]
	public void When_Unmodeled_ToastGeneric_Features_Are_Used_Parser_Throws()
	{
		const string grouped = "<toast><visual><binding template='ToastGeneric'><group><subgroup><text>Grouped</text></subgroup></group></binding></visual></toast>";
		const string header = "<toast><visual><binding template='ToastGeneric'/></visual><header id='group' title='Title' arguments='open'/></toast>";
		const string unknown = "<toast><visual><binding template='ToastGeneric'><cameraPreview/></binding></visual></toast>";

		Assert.ThrowsExactly<NotSupportedException>(() => AppNotificationPayloadParser.Parse(grouped));
		Assert.ThrowsExactly<NotSupportedException>(() => AppNotificationPayloadParser.Parse(header));
		Assert.ThrowsExactly<NotSupportedException>(() => AppNotificationPayloadParser.Parse(unknown));
	}

	[TestMethod]
	public void When_Required_Attributes_Or_Collection_Limits_Are_Invalid_Parser_Throws()
	{
		const string missingImageSource = "<toast><visual><binding template='ToastGeneric'><image/></binding></visual></toast>";
		const string missingActionArguments = "<toast><visual><binding template='ToastGeneric'/></visual><actions><action content='Open'/></actions></toast>";
		const string missingProgressStatus = "<toast><visual><binding template='ToastGeneric'><progress value='0.5'/></binding></visual></toast>";
		var tooManyActions = $"<toast><visual><binding template='ToastGeneric'/></visual><actions>{string.Concat(Enumerable.Repeat("<action content='Open' arguments='' />", 6))}</actions></toast>";

		Assert.ThrowsExactly<FormatException>(() => AppNotificationPayloadParser.Parse(missingImageSource));
		Assert.ThrowsExactly<FormatException>(() => AppNotificationPayloadParser.Parse(missingActionArguments));
		Assert.ThrowsExactly<FormatException>(() => AppNotificationPayloadParser.Parse(missingProgressStatus));
		Assert.ThrowsExactly<FormatException>(() => AppNotificationPayloadParser.Parse(tooManyActions));
	}

	[TestMethod]
	public void When_Payload_Contains_A_Dtd_Parser_Throws()
	{
		const string payload = "<!DOCTYPE toast [<!ENTITY content 'expanded'>]><toast><visual><binding template='ToastGeneric'><text>&content;</text></binding></visual></toast>";

		Assert.ThrowsExactly<XmlException>(() => AppNotificationPayloadParser.Parse(payload));
	}

	[TestMethod]
	public void When_Raw_Payload_Exceeds_Builder_Limit_It_Is_Parsed()
	{
		const string prefix = "<toast><visual><binding template='ToastGeneric'><text>";
		const string suffix = "</text></binding></visual></toast>";
		var content = new string('A', 12_000);
		var payload = prefix + content + suffix;

		var result = AppNotificationPayloadParser.Parse(payload);

		Assert.AreEqual(content.Length, result.Title?.Content.Length);
	}

	[TestMethod]
	public void When_ToastGeneric_Binding_Is_Missing_Parser_Throws()
	{
		Assert.ThrowsExactly<FormatException>(() => AppNotificationPayloadParser.Parse("<toast><visual><binding template='ToastText01'/></visual></toast>"));
	}

	private static void AssertPayloadsAreEquivalent(AppNotificationPayload expected, AppNotificationPayload actual)
	{
		Assert.AreEqual(expected.LaunchArgument, actual.LaunchArgument);
		AssertDictionariesAreEquivalent(expected.LaunchArguments, actual.LaunchArguments);
		Assert.AreEqual(expected.Scenario, actual.Scenario);
		Assert.AreEqual(expected.Duration, actual.Duration);
		Assert.AreEqual(expected.DisplayTimestamp, actual.DisplayTimestamp);
		Assert.AreEqual(expected.UseButtonStyle, actual.UseButtonStyle);
		Assert.AreEqual(expected.ActivationType, actual.ActivationType);
		Assert.AreEqual(expected.ProtocolActivationTargetApplicationPfn, actual.ProtocolActivationTargetApplicationPfn);
		Assert.AreEqual(expected.Language, actual.Language);
		Assert.AreEqual(expected.BaseUri, actual.BaseUri);
		Assert.AreEqual(expected.AddImageQuery, actual.AddImageQuery);
		CollectionAssert.AreEqual(expected.Texts.ToArray(), actual.Texts.ToArray());
		Assert.AreEqual(expected.Attribution, actual.Attribution);
		CollectionAssert.AreEqual(expected.Images.ToArray(), actual.Images.ToArray());
		CollectionAssert.AreEqual(expected.ProgressBars.ToArray(), actual.ProgressBars.ToArray());
		Assert.AreEqual(expected.Audio, actual.Audio);
		Assert.AreEqual(expected.Inputs.Length, actual.Inputs.Length);
		for (var index = 0; index < expected.Inputs.Length; index++)
		{
			Assert.AreEqual(expected.Inputs[index] with { Selections = default }, actual.Inputs[index] with { Selections = default });
			CollectionAssert.AreEqual(expected.Inputs[index].Selections.ToArray(), actual.Inputs[index].Selections.ToArray());
		}
		Assert.AreEqual(expected.Actions.Length, actual.Actions.Length);
		for (var index = 0; index < expected.Actions.Length; index++)
		{
			Assert.AreEqual(expected.Actions[index] with { Arguments = default! }, actual.Actions[index] with { Arguments = default! });
			AssertDictionariesAreEquivalent(expected.Actions[index].Arguments, actual.Actions[index].Arguments);
		}
	}

	private static void AssertDictionariesAreEquivalent(IReadOnlyDictionary<string, string> expected, IReadOnlyDictionary<string, string> actual)
	{
		Assert.AreEqual(expected.Count, actual.Count);
		foreach (var pair in expected)
		{
			Assert.IsTrue(actual.TryGetValue(pair.Key, out var value));
			Assert.AreEqual(pair.Value, value);
		}
	}
}
