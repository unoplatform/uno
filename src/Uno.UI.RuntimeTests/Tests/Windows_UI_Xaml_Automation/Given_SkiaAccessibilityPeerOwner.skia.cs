#nullable enable

using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.RuntimeTests.Helpers;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation;

[TestClass]
[RunsOnUIThread]
public class Given_SkiaAccessibilityPeerOwner
{
	[TestMethod]
	public void When_Unrealized_Item_Does_Not_Use_Generic_Ancestor_Fallback()
	{
		var item = new object();
		var itemsControl = new ItemsControl();
		itemsControl.Items.Add(item);
		var itemsControlPeer = new ItemsControlAutomationPeer(itemsControl);
		var itemPeer = new ItemAutomationPeer(item, itemsControlPeer);
		var virtualParent = new VirtualAutomationPeer();
		virtualParent.SetParent(itemsControlPeer);
		itemPeer.SetParent(virtualParent);

		Assert.IsNull(itemPeer.GetContainer());
		Assert.IsTrue(itemPeer.TryGetProviderOwner(out var ancestorOwner));
		Assert.AreSame(itemsControl, ancestorOwner);

		Assert.IsFalse(TryGetPeerOwner(itemPeer, out var owner));
		Assert.IsNull(owner);
	}

	[TestMethod]
	public async Task When_Realized_Item_Resolves_To_Its_Container()
	{
		var item = new object();
		var itemsControl = new ListView { Width = 240, Height = 120 };
		itemsControl.Items.Add(item);

		try
		{
			await UITestHelper.Load(itemsControl);
			await WindowHelper.WaitFor(() => itemsControl.ContainerFromItem(item) is UIElement);
			var container = itemsControl.ContainerFromItem(item);
			var itemPeer = new ItemAutomationPeer(item, new ItemsControlAutomationPeer(itemsControl));

			Assert.IsNotNull(container);
			Assert.IsTrue(TryGetPeerOwner(itemPeer, out var owner));
			Assert.AreSame(container, owner);
			Assert.AreNotSame(itemsControl, owner);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public void When_RichEditBox_Virtual_Child_Resolves_To_Its_Editor()
	{
		var editor = new RichEditBox();
		editor.Document.SetText(TextSetOptions.None, "link");
		editor.Document.GetRange(0, 4).Link = "\"https://example.com\"";
		var editorPeer = FrameworkElementAutomationPeer.CreatePeerForElement(editor);

		Assert.IsNotNull(editorPeer);
		Assert.IsNull(editorPeer.GetPattern(PatternInterface.Value));
		var children = editorPeer.GetChildren();
		Assert.IsNotNull(children);
		Assert.HasCount(1, children);
		var child = children[0];
		Assert.AreEqual(AutomationControlType.Hyperlink, child.GetAutomationControlType());
		Assert.IsFalse(child is FrameworkElementAutomationPeer or ItemAutomationPeer);

		Assert.IsTrue(TryGetPeerOwner(child, out var owner));
		Assert.AreSame(editor, owner);
	}

	private static bool TryGetPeerOwner(AutomationPeer peer, out UIElement? owner)
	{
		var type = Type.GetType(
			"Uno.UI.Runtime.Skia.SkiaAccessibilityBase, Uno.UI.Runtime.Skia",
			throwOnError: true)!;
		var method = type.GetMethod(
			"TryGetPeerOwner",
			BindingFlags.Static | BindingFlags.NonPublic,
			binder: null,
			types: new[] { typeof(AutomationPeer), typeof(UIElement).MakeByRefType() },
			modifiers: null);
		Assert.IsNotNull(method);

		object?[] arguments = { peer, null };
		var result = (bool)method.Invoke(null, arguments)!;
		owner = (UIElement?)arguments[1];
		return result;
	}

	private sealed class VirtualAutomationPeer : AutomationPeer
	{
	}
}
