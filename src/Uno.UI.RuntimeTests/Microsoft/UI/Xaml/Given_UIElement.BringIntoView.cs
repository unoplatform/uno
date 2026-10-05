using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml;

[TestClass]
public class Given_UIElement_BringIntoView
{
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_StartBringIntoView_Before_Loaded_Then_Request_Is_Raised()
	{
		var host = new StackPanel { Width = 100, Height = 100 };
		await UITestHelper.Load(host);

		var requestCount = 0;
		host.BringIntoViewRequested += (_, _) => requestCount++;

		// Like an ItemsRepeater element realized during the current layout pass: in the live tree, not loaded yet.
		var target = new Border { Width = 10, Height = 10 };
		host.Children.Add(target);
		Assert.IsFalse(target.IsLoaded, "Precondition: the element must not have raised Loaded yet.");

		target.StartBringIntoView();

		Assert.AreEqual(1, requestCount);
	}
}
