#nullable enable

using System;
using System.Linq;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Windows.AppNotifications.Internal;
using Windows.UI.Notifications;

namespace Uno.UI.Tests.Microsoft_Windows_AppNotifications;

[TestClass]
public class Given_LegacyToastNotificationPayloadAdapter
{
	[TestMethod]
	[DataRow("ToastImageAndText01", 1, true, 2, "", "Text 1")]
	[DataRow("ToastImageAndText02", 2, true, 2, "Text 1", "Text 2")]
	[DataRow("ToastImageAndText03", 2, true, 2, "Text 1", "Text 2")]
	[DataRow("ToastImageAndText04", 3, true, 2, "Text 1", "Text 2\nText 3")]
	[DataRow("ToastText01", 1, false, 2, "", "Text 1")]
	[DataRow("ToastText02", 2, false, 2, "Text 1", "Text 2")]
	[DataRow("ToastText03", 2, false, 2, "Text 1", "Text 2")]
	[DataRow("ToastText04", 3, false, 2, "Text 1", "Text 2\nText 3")]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/22462")]
	public void When_Legacy_Template_Is_Normalized_Hardened_Parser_Accepts_It(
		string template,
		int legacyTextCount,
		bool includesImage,
		int parsedTextCount,
		string expectedTitle,
		string expectedBody)
	{
		var texts = string.Concat(Enumerable.Range(1, legacyTextCount).Select(index => $"<text id='{index}'>Text {index}</text>"));
		var image = includesImage ? "<image id='1' src='ms-appx:///logo.png'/>" : string.Empty;
		var payload = $"<toast><visual><binding template='{template}'>{image}{texts}</binding></visual></toast>";

		var normalized = LegacyToastNotificationPayloadAdapter.Normalize(payload);
		var parsed = AppNotificationPayloadParser.Parse(normalized);

		Assert.AreEqual(parsedTextCount, parsed.Texts.Length);
		Assert.AreEqual(expectedTitle, parsed.Title?.Content);
		Assert.AreEqual(expectedBody, parsed.Body?.Content);
		Assert.AreEqual(includesImage ? 1 : 0, parsed.Images.Length);

		var restored = XDocument.Parse(LegacyToastNotificationPayloadAdapter.Restore(normalized));
		var restoredBinding = restored.Root!.Element("visual")!.Element("binding")!;
		Assert.AreEqual(template, restoredBinding.Attribute("template")?.Value);
		Assert.AreEqual(legacyTextCount, restoredBinding.Elements("text").Count());
		Assert.IsNull(restoredBinding.Attribute("uno-legacy-template"));
		Assert.IsTrue(XNode.DeepEquals(XDocument.Parse(payload), restored));
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/22462")]
	public void When_Third_Line_Has_Attributes_And_Escapes_It_Is_Restored_Losslessly()
	{
		const string payload = "<toast><visual><binding template='ToastText04'><text id='1'>Title</text><text id='2'>Second &amp; line</text><text id='3' xml:lang='fr-FR'>Third &lt;line&gt;</text></binding></visual></toast>";

		var normalized = LegacyToastNotificationPayloadAdapter.Normalize(payload);
		var texts = XDocument.Parse(normalized).Root!.Element("visual")!.Element("binding")!.Elements("text").ToArray();

		Assert.AreEqual(2, texts.Length);
		Assert.AreEqual("Second & line\nThird <line>", texts[1].Value);
		Assert.IsTrue(XNode.DeepEquals(
			XDocument.Parse(payload),
			XDocument.Parse(LegacyToastNotificationPayloadAdapter.Restore(normalized))));
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/22462")]
	public void When_An_Older_Normalized_Payload_Is_Restored_The_Third_Line_Remains()
	{
		const string payload = "<toast><visual><binding template='ToastGeneric' uno-legacy-template='ToastText04' uno-legacy-second-text='Second'><text id='1'>Title</text><text id='2'>Second\nThird</text><text id='3'>Third</text></binding></visual></toast>";
		const string expected = "<toast><visual><binding template='ToastText04'><text id='1'>Title</text><text id='2'>Second</text><text id='3'>Third</text></binding></visual></toast>";

		Assert.IsTrue(XNode.DeepEquals(
			XDocument.Parse(expected),
			XDocument.Parse(LegacyToastNotificationPayloadAdapter.Restore(payload))));
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/22462")]
	public void When_The_Third_Text_Marker_Contains_Another_Element_It_Is_Rejected()
	{
		const string payload = "<toast><visual><binding template='ToastGeneric' uno-legacy-template='ToastText04' uno-legacy-second-text='Second' uno-legacy-third-text='&lt;image/&gt;'><text>Title</text><text>Second\nThird</text></binding></visual></toast>";

		Assert.ThrowsExactly<ArgumentException>(() => LegacyToastNotificationPayloadAdapter.Restore(payload));
	}

	[TestMethod]
	public void When_Template_Is_Not_Legacy_Payload_Is_Unchanged()
	{
		const string payload = "<toast><visual><binding template='ToastGeneric'><text>Title</text></binding></visual></toast>";

		Assert.AreSame(payload, LegacyToastNotificationPayloadAdapter.Normalize(payload));
	}
}
