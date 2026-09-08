#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Windows.AppNotifications.Builder;
using Microsoft.Windows.AppNotifications.Internal;

namespace Uno.UI.Tests.Microsoft_Windows_AppNotifications;

[TestClass]
public class Given_AppNotificationBuilderComponents
{
	[TestMethod]
	public void When_Text_Properties_Are_Set_Xml_Matches_Windows_App_Sdk()
	{
		var properties = new AppNotificationTextProperties()
			.SetLanguage("en-US")
			.SetMaxLines(2)
			.SetIncomingCallAlignment();

		Assert.AreEqual("<text lang='en-US' hint-maxLines='2' hint-callScenarioCenterAlign='true'>", properties.ToString());
		Assert.AreEqual(properties.ToString(), properties.ToXml());
	}

	[TestMethod]
	public void When_Progress_Bar_Uses_Defaults_Xml_Matches_Windows_App_Sdk()
	{
		var progressBar = new AppNotificationProgressBar();

		Assert.AreEqual("<progress status='{progressStatus}' value='{progressValue}'/>", progressBar.ToString());
		Assert.AreEqual(progressBar.ToString(), progressBar.ToXml());
		Assert.ThrowsExactly<ArgumentException>(() => progressBar.Value = -0.1);
		Assert.ThrowsExactly<ArgumentException>(() => progressBar.SetValue(1.01));
	}

	[TestMethod]
	public void When_Progress_Bindings_And_Values_Change_Last_Call_Wins()
	{
		var progressBar = new AppNotificationProgressBar()
			.BindTitle()
			.SetTitle("Specific title")
			.SetStatus("Still downloading...")
			.SetValue(0.8)
			.BindValueStringOverride();

		Assert.AreEqual("<progress title='Specific title' status='Still downloading...' value='0.8' valueStringOverride='{progressValueString}'/>", progressBar.ToString());
	}

	[TestMethod]
	public void When_Combo_Box_Is_Built_Xml_Matches_Windows_App_Sdk()
	{
		var comboBox = new AppNotificationComboBox("comboBox1")
			.AddItem("item1", "item1 text")
			.AddItem("item2", "item2 text")
			.AddItem("item1", "replacement")
			.SetTitle("ComboBox Title")
			.SetSelectedItem("item2");

		Assert.AreEqual("<input id='comboBox1' type='selection' title='ComboBox Title' defaultInput='item2'><selection id='item1' content='replacement'/><selection id='item2' content='item2 text'/></input>", comboBox.ToString());
		Assert.AreEqual(comboBox.ToString(), comboBox.ToXml());
	}

	[TestMethod]
	public void When_Fluent_Maps_Are_Unsorted_Xml_Uses_WinRt_Key_Order()
	{
		var comboBox = new AppNotificationComboBox("combo")
			.AddItem("z", "last")
			.AddItem("a", "first")
			.AddItem("0", "digit")
			.AddItem("<", "encoded first");
		var button = new AppNotificationButton()
			.AddArgument("z", "last")
			.AddArgument("a", "first")
			.AddArgument("0", "digit")
			.AddArgument("=", "encoded first");

		Assert.AreEqual("<input id='combo' type='selection'><selection id='&lt;' content='encoded first'/><selection id='0' content='digit'/><selection id='a' content='first'/><selection id='z' content='last'/></input>", comboBox.ToString());
		Assert.AreEqual("<action content='' arguments='%3D=encoded first;0=digit;a=first;z=last'/>", button.ToString());
	}

	[TestMethod]
	public void When_String_Properties_Are_Set_To_Null_They_Become_Empty()
	{
		var button = new AppNotificationButton
		{
			Content = null!,
			ToolTip = null!,
			InputId = null!,
			TargetAppId = null!,
		};
		var properties = new AppNotificationTextProperties
		{
			Language = null!,
		};
		var comboBox = new AppNotificationComboBox("combo")
		{
			Items = null!,
			Title = null!,
			SelectedItem = null!,
		};
		var progressBar = new AppNotificationProgressBar
		{
			Title = null!,
			Status = null!,
			ValueStringOverride = null!,
		};

		Assert.AreEqual(string.Empty, button.Content);
		Assert.AreEqual(string.Empty, button.ToolTip);
		Assert.AreEqual(string.Empty, button.InputId);
		Assert.AreEqual(string.Empty, button.TargetAppId);
		Assert.AreEqual(string.Empty, properties.Language);
		Assert.AreEqual(0, comboBox.Items.Count);
		Assert.AreEqual(string.Empty, comboBox.Title);
		Assert.AreEqual(string.Empty, comboBox.SelectedItem);
		Assert.AreEqual(string.Empty, progressBar.Title);
		Assert.AreEqual(string.Empty, progressBar.Status);
		Assert.AreEqual(string.Empty, progressBar.ValueStringOverride);
		Assert.AreEqual("<action content='' arguments=''/>", button.ToString());
		Assert.AreEqual("<text>", properties.ToString());
	}

	[TestMethod]
	public void When_Button_Is_Built_Xml_Matches_Windows_App_Sdk()
	{
		var button = new AppNotificationButton("content")
			.AddArgument("key", "value")
			.SetContextMenuPlacement()
			.SetIcon(new Uri("http://www.microsoft.com/"))
			.SetInputId("inputId")
			.SetButtonStyle(AppNotificationButtonStyle.Success)
			.SetToolTip("toolTip");

		Assert.AreEqual("<action content='content' arguments='key=value' placement='contextMenu' imageUri='http://www.microsoft.com/' hint-inputId='inputId' hint-buttonStyle='Success' hint-toolTip='toolTip'/>", button.ToString());
		Assert.AreEqual(button.ToString(), button.ToXml());
	}

	[TestMethod]
	public void When_Button_Uses_Protocol_Arguments_Are_Rejected()
	{
		var uri = new Uri("http://www.microsoft.com/");
		var button = new AppNotificationButton("content").SetInvokeUri(uri, "Contoso.App_123");

		Assert.AreEqual("<action content='content' arguments='http://www.microsoft.com/' activationType='protocol' protocolActivationTargetApplicationPfn='Contoso.App_123'/>", button.ToString());
		Assert.ThrowsExactly<ArgumentException>(() => button.AddArgument("key", "value"));
		Assert.ThrowsExactly<ArgumentException>(() => new AppNotificationButton().AddArgument("key", "value").SetInvokeUri(uri));
	}

	[TestMethod]
	public void When_Component_Attributes_Contain_Xml_Syntax_They_Are_Encoded_Once()
	{
		const string content = "Safe' /><action content='Injected";
		var button = new AppNotificationButton(content)
		{
			Arguments = new Dictionary<string, string> { ["key' name="] = "value&<" },
			InputId = "input' id",
			ToolTip = "tip&'",
		};
		var comboBox = new AppNotificationComboBox("choice' id")
		{
			Items = new Dictionary<string, string> { ["item' id"] = "content&<" },
			Title = "title' value",
			SelectedItem = "item' id",
		};

		var payload = $"<toast><visual><binding template='ToastGeneric'/></visual><actions>{comboBox}{button}</actions></toast>";
		var parsed = AppNotificationPayloadParser.Parse(payload);

		Assert.AreEqual(1, parsed.Actions.Length);
		Assert.AreEqual(content, parsed.Actions[0].Content);
		Assert.AreEqual("value&<", parsed.Actions[0].Arguments["key' name="]);
		Assert.AreEqual("input' id", parsed.Actions[0].InputId);
		Assert.AreEqual("tip&'", parsed.Actions[0].ToolTip);
		Assert.AreEqual("choice' id", parsed.Inputs[0].Id);
		Assert.AreEqual("content&<", parsed.Inputs[0].Selections[0].Content);
	}

	[TestMethod]
	public void When_Fluent_Component_Values_Contain_Xml_Syntax_They_Are_Encoded_Once()
	{
		var textProperties = new AppNotificationTextProperties().SetLanguage("en&'US");
		var progressBar = new AppNotificationProgressBar().SetTitle("A&B").SetStatus("C<D").SetValueStringOverride("E'F");
		var comboBox = new AppNotificationComboBox("choice&1").AddItem("item&1", "A<B").SetTitle("C'D").SetSelectedItem("item&1");
		var button = new AppNotificationButton("A&B").SetInputId("input&1").SetToolTip("C'D");

		Assert.AreEqual("<text lang='en&amp;&apos;US'>", textProperties.ToString());
		Assert.AreEqual("<progress title='A&amp;B' status='C&lt;D' value='{progressValue}' valueStringOverride='E&apos;F'/>", progressBar.ToString());
		Assert.AreEqual("<input id='choice&amp;1' type='selection' title='C&apos;D' defaultInput='item&amp;1'><selection id='item&amp;1' content='A&lt;B'/></input>", comboBox.ToString());
		Assert.AreEqual("<action content='A&amp;B' arguments='' hint-inputId='input&amp;1' hint-toolTip='C&apos;D'/>", button.ToString());
	}

	[TestMethod]
	public void When_Components_Are_Used_As_IStringable_They_Return_Their_Xml()
	{
		Windows.Foundation.IStringable textProperties = new AppNotificationTextProperties();
		Windows.Foundation.IStringable progressBar = new AppNotificationProgressBar();
		Windows.Foundation.IStringable comboBox = new AppNotificationComboBox("combo");
		Windows.Foundation.IStringable button = new AppNotificationButton();

		Assert.AreEqual("<text>", textProperties.ToString());
		Assert.AreEqual("<progress status='{progressStatus}' value='{progressValue}'/>", progressBar.ToString());
		Assert.AreEqual("<input id='combo' type='selection'></input>", comboBox.ToString());
		Assert.AreEqual("<action content='' arguments=''/>", button.ToString());
	}
}
