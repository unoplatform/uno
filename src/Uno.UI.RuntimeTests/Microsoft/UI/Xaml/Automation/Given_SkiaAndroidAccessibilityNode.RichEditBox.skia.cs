#nullable enable

using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation;

public partial class Given_SkiaAndroidAccessibilityNode
{
	private static AccessibilityNativeNodeSnapshot? GetRichEditSnapshot(UIElement element)
		=> AccessibilityPeerHelper.AndroidAccessibilityNodeSnapshotAccessor?.Invoke(element);

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_RichEditBox_Is_Queried_Then_It_Is_Editable_Multiline_Text()
	{
		var richEditBox = new RichEditBox { Width = 320, Height = 120 };
		await UITestHelper.Load(richEditBox);
		richEditBox.Document.SetText(TextSetOptions.None, "Alpha beta");
		await UITestHelper.WaitForIdle();

		var snapshot = GetRichEditSnapshot(richEditBox);

		Assert.IsNotNull(snapshot);
		Assert.AreEqual("Alpha beta", snapshot.Value);
		Assert.IsNotNull(snapshot.Details?.TextState);
		Assert.IsTrue(snapshot.Details.TextState.IsEditable);
		Assert.IsFalse(snapshot.Details.TextState.IsReadOnly);
		Assert.IsTrue(snapshot.Details.TextState.IsMultiline);
		CollectionAssert.DoesNotContain(snapshot.Details.SupportedActions.ToArray(), AccessibilityNativeAction.SetValue);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_RichEditBox_Is_ReadOnly_Then_Node_Is_Not_Editable()
	{
		var richEditBox = new RichEditBox { Width = 320, Height = 120, IsReadOnly = true };
		await UITestHelper.Load(richEditBox);

		var snapshot = GetRichEditSnapshot(richEditBox);

		Assert.IsNotNull(snapshot?.Details?.TextState);
		Assert.IsFalse(snapshot.Details.TextState.IsEditable);
		Assert.IsTrue(snapshot.Details.TextState.IsReadOnly);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_RichEditBox_Selection_Is_Backward_Then_Node_Reports_Its_Direction()
	{
		var richEditBox = new RichEditBox { Width = 320, Height = 120 };
		await UITestHelper.Load(richEditBox);
		richEditBox.Document.SetText(TextSetOptions.None, "Alpha beta gamma");
		Assert.IsTrue(richEditBox.ApplyAccessibilitySelection(6, 10, isBackward: true));
		await UITestHelper.WaitForIdle();

		var snapshot = GetRichEditSnapshot(richEditBox);

		Assert.IsNotNull(snapshot);
		Assert.AreEqual(10, snapshot.TextSelectionStart);
		Assert.AreEqual(6, snapshot.TextSelectionEnd);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_RichEditBox_Hyperlink_Is_HitTested_Then_Its_Node_Is_Returned()
	{
		var richEditBox = new RichEditBox { Width = 320, Height = 120 };
		await UITestHelper.Load(richEditBox);
		richEditBox.Document.SetText(TextSetOptions.None, "prefix link suffix");
		richEditBox.Document.GetRange(7, 11).Link = "\"https://example.com\"";
		await UITestHelper.WaitForIdle();

		var peer = FrameworkElementAutomationPeer.CreatePeerForElement(richEditBox);
		var linkPeer = peer.GetChildren()!.Single(child => child.GetAutomationControlType() == AutomationControlType.Hyperlink);
		_ = GetRichEditSnapshot(richEditBox);
		var linkId = AccessibilityPeerHelper.AndroidAccessibilityPeerVirtualIdAccessor?.Invoke(linkPeer);
		Assert.IsNotNull(linkId, "The hyperlink must be its own virtual node.");

		var bounds = linkPeer.GetBoundingRectangle();
		var scale = richEditBox.XamlRoot!.RasterizationScale;
		var hitId = AccessibilityPeerHelper.AndroidAccessibilityHitTestAccessor?.Invoke(
			richEditBox.XamlRoot!,
			(bounds.X + bounds.Width / 2) * scale,
			(bounds.Y + bounds.Height / 2) * scale);

		Assert.AreEqual(linkId, hitId, "Explore-by-touch over the link must reach the link, not the editor.");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Focused_RichEditBox_Has_Selection_Then_Copy_Is_Advertised()
	{
		var richEditBox = new RichEditBox { Width = 320, Height = 120 };
		await UITestHelper.Load(richEditBox);
		richEditBox.Document.SetText(TextSetOptions.None, "Alpha beta");
		richEditBox.Focus(FocusState.Programmatic);
		richEditBox.Document.Selection.SetRange(0, 5);
		await UITestHelper.WaitForIdle();

		var snapshot = GetRichEditSnapshot(richEditBox);

		Assert.IsNotNull(snapshot);
		CollectionAssert.Contains(snapshot.NativeActionIds.ToArray(), 0x4000, "ACTION_COPY");
		CollectionAssert.Contains(snapshot.NativeActionIds.ToArray(), 0x10000, "ACTION_CUT");
	}
}
