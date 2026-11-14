using Microsoft.UI.Xaml.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests.Tests.HotReload.Frame.Pages;

namespace Uno.UI.RuntimeTests.Tests.HotReload.Frame.HRApp.Tests;

/// <summary>
/// Hot reload swaps a reloaded element for a new instance that inherits its local values, Name included.
/// ElementName bindings made afterwards must find that new instance, not the one that left.
/// </summary>
[TestClass]
[RunsOnUIThread]
public class Given_ElementName : BaseTestClass
{
	private const string ControlOriginalText = "Control 1";
	private const string ControlChangedText = ControlOriginalText + " (changed)";
	private const string CodeName = "CodeNamedControl";

	[TestMethod]
	public async Task When_Code_Named_Panel_Child_Is_Reloaded()
	{
		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

		var panel = new StackPanel();
		var original = new HR_Frame_Pages_UC1 { Name = CodeName };
		panel.Children.Add(original);
		UnitTestsUIContentHelper.Content = panel;
		await UnitTestsUIContentHelper.WaitForLoaded(original);

		Assert.AreSame(original, await ResolveElementName(panel, CodeName));

		await HotReloadHelper.UpdateServerFileAndRevert<HR_Frame_Pages_UC1>(
			ControlOriginalText,
			ControlChangedText,
			async () =>
			{
				var replacement = (FrameworkElement)panel.Children[0];
				Assert.AreNotSame(original, replacement, "the control should have been replaced by the hot reload");
				Assert.AreEqual(CodeName, replacement.Name, "the replacement should inherit the name");

				Assert.AreSame(replacement, await ResolveElementName(panel, CodeName));
			},
			cts.Token);

		Assert.AreSame(panel.Children[0], await ResolveElementName(panel, CodeName), "after the revert");
	}

	[TestMethod]
	public async Task When_Code_Named_ContentControl_Content_Is_Reloaded()
	{
		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

		var original = new HR_Frame_Pages_UC1 { Name = CodeName };
		var contentControl = new ContentControl { Content = original };
		var panel = new StackPanel { Children = { contentControl } };
		UnitTestsUIContentHelper.Content = panel;
		await UnitTestsUIContentHelper.WaitForLoaded(original);

		// Resolved from a sibling of the ContentControl, so through the scope the ContentControl sits in.
		Assert.AreSame(original, await ResolveElementName(panel, CodeName));

		await HotReloadHelper.UpdateServerFileAndRevert<HR_Frame_Pages_UC1>(
			ControlOriginalText,
			ControlChangedText,
			async () =>
			{
				var replacement = (FrameworkElement)contentControl.Content;
				Assert.AreNotSame(original, replacement, "the content should have been replaced by the hot reload");
				Assert.AreEqual(CodeName, replacement.Name, "the replacement should inherit the name");

				Assert.AreSame(replacement, await ResolveElementName(panel, CodeName));
			},
			cts.Token);

		Assert.AreSame(contentControl.Content, await ResolveElementName(panel, CodeName), "after the revert");
	}

	private static async Task<object> ResolveElementName(Panel panel, string elementName)
	{
		var probe = new TextBlock { Text = "probe" };
		probe.SetBinding(FrameworkElement.TagProperty, new Binding { ElementName = elementName });
		panel.Children.Add(probe);

		try
		{
			await UnitTestsUIContentHelper.WaitForLoaded(probe);
			return probe.Tag;
		}
		finally
		{
			panel.Children.Remove(probe);
		}
	}
}
