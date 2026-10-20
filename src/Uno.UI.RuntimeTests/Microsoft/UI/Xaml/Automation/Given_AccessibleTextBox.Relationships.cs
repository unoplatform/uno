#nullable enable

using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;

#if HAS_UNO
using static Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation.WasmSemanticDomHelper;
#endif

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation;

partial class Given_AccessibleTextBox
{
	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/3848")]
	[DataRow(false)]
	[DataRow(true)]
	public async Task When_RichEditBox_DescribedBy_Query_Adds_Placeholder(bool loadAfterEnablingAccessibility)
	{
#if __SKIA__
		var richEditBox = new RichEditBox { PlaceholderText = "Empty document hint" };
		var description = new TextBlock { Text = "Document help" };
		var targets = new DependencyObjectCollection { description };
		richEditBox.SetValue(AutomationProperties.DescribedByProperty, targets);
		var panel = new StackPanel { Children = { richEditBox, description } };

		try
		{
			if (loadAfterEnablingAccessibility)
			{
				EnableAccessibilityThroughDom();
			}

			await UITestHelper.Load(panel);
			if (!loadAfterEnablingAccessibility)
			{
				EnableAccessibilityThroughDom();
			}

			var peer = richEditBox.GetOrCreateAutomationPeer();
			Assert.IsNotNull(peer);
			peer.GetDescribedBy();
			await UITestHelper.WaitFor(
				() => SemanticElementExists(richEditBox)
					&& SemanticElementExists(description)
					&& GetSemanticAttribute(richEditBox, "aria-describedby") == GetSemanticElementId(description),
				timeoutMS: 5000,
				message: "Adding the placeholder during a relation query must not interrupt semantic node creation.");
			Assert.AreEqual(2, targets.Count, "The native-source getter adds the visible placeholder to DescribedBy.");
			Assert.IsFalse(SemanticElementExists((UIElement)targets[1]),
				"The native textarea's template must not leak standalone semantic nodes.");
			Assert.AreEqual("0", GetSemanticTextControlChildCount(richEditBox));
			Assert.IsNull(peer.GetPattern(PatternInterface.Value));
			Assert.AreEqual("Empty document hint", GetSemanticAttribute(richEditBox, "placeholder"));

			richEditBox.Document.SetText(Microsoft.UI.Text.TextSetOptions.None, "abcdef");
			richEditBox.Document.Selection.SetRange(5, 1);
			richEditBox.PlaceholderText = "Updated document hint";
			await UITestHelper.WaitFor(
				() => targets.Count == 1
					&& GetSemanticAttribute(richEditBox, "placeholder") == "Updated document hint"
					&& GetSemanticAttribute(richEditBox, "aria-describedby") == GetSemanticElementId(description),
				timeoutMS: 5000,
				message: "Hiding the placeholder must retain the authored relation and update the textarea hint.");
			Assert.AreSame(description, targets[0]);
			Assert.AreEqual("abcdef", GetSemanticTextControlValue(richEditBox));
			Assert.AreEqual("1", GetSemanticTextControlSelectionStart(richEditBox));
			Assert.AreEqual("5", GetSemanticTextControlSelectionEnd(richEditBox));
			Assert.AreEqual("backward", GetSemanticTextControlSelectionDirection(richEditBox));
		}
		finally
		{
			TestServices.WindowHelper.WindowContent = null;
		}
#endif
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/3848")]
	[DataRow(false)]
	[DataRow(true)]
	public async Task When_Text_Input_Loaded_After_Accessibility_Then_Template_Is_Not_Semantic(bool isPassword)
	{
#if __SKIA__
		Control textControl = isPassword
			? new PasswordBox { PlaceholderText = "Password hint" }
			: new TextBox { PlaceholderText = "Text hint" };

		try
		{
			EnableAccessibilityThroughDom();
			await UITestHelper.Load(textControl);
			var peer = textControl.GetOrCreateAutomationPeer();
			Assert.IsNotNull(peer);
			peer.GetDescribedBy();
			await UITestHelper.WaitFor(
				() => SemanticElementExists(textControl),
				timeoutMS: 5000,
				message: "The native input must be created when loaded after accessibility is enabled.");

			var descriptions = AutomationProperties.GetDescribedBy(textControl);
			Assert.IsNotNull(descriptions);
			Assert.AreEqual(1, descriptions.Count);
			Assert.IsFalse(SemanticElementExists((UIElement)descriptions[0]),
				"The native input's placeholder must not become a standalone semantic node.");
			Assert.AreEqual("0", GetSemanticTextControlChildCount(textControl));
			Assert.IsFalse(SemanticElementHasAttribute(textControl, "aria-describedby"),
				"The native placeholder must not also be referenced through an invalid template IDREF.");
		}
		finally
		{
			TestServices.WindowHelper.WindowContent = null;
		}
#endif
	}
}
