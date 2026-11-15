#nullable enable

using System.Threading.Tasks;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation;

public partial class Given_SkiaIOSAccessibilityElement
{
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_RichEditBox_Then_AccessibilityValue_Is_Document_Text()
	{
		var richEditBox = new RichEditBox { Width = 320, Height = 120 };
		await UITestHelper.Load(richEditBox);
		richEditBox.Document.SetText(TextSetOptions.None, "Alpha beta");
		await UITestHelper.WaitForIdle();

		var snapshot = GetSnapshot(richEditBox);

		Assert.IsNotNull(snapshot);
		Assert.AreEqual("Alpha beta", snapshot.Value);
		Assert.IsNotNull(snapshot.Details?.TextState);
		Assert.IsTrue(snapshot.Details.TextState.IsEditable);
		Assert.IsTrue(snapshot.Details.TextState.IsMultiline);
	}
}
