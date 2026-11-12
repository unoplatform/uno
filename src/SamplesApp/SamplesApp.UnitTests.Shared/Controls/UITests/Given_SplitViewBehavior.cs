#nullable enable

using System.Threading.Tasks;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.UI.RuntimeTests;
using Uno.UI.Samples.Behaviors;

namespace SamplesApp.Tests;

[TestClass]
[RunsOnUIThread]
public class Given_SplitViewBehavior
{
	[TestMethod]
	[DataRow(SplitViewDisplayMode.Overlay, false)]
	[DataRow(SplitViewDisplayMode.CompactOverlay, false)]
	[DataRow(SplitViewDisplayMode.Inline, true)]
	[DataRow(SplitViewDisplayMode.CompactInline, true)]
	public async Task When_CloseOnClick(SplitViewDisplayMode mode, bool expectedPaneOpen)
	{
		var button = new Button { Content = "Pane button" };
		SplitViewBehavior.SetCloseOnClick(button, true);

		var splitView = new SplitView
		{
			DisplayMode = mode,
			IsPaneOpen = true,
			Pane = button,
			Content = new Grid(),
			Width = 400,
			Height = 300,
		};

		TestServices.WindowHelper.WindowContent = splitView;
		await TestServices.WindowHelper.WaitForLoaded(button);

		var peer = new ButtonAutomationPeer(button);
		var invoke = (IInvokeProvider)peer.GetPattern(PatternInterface.Invoke)!;
		invoke.Invoke();

		Assert.AreEqual(expectedPaneOpen, splitView.IsPaneOpen);
	}
}
