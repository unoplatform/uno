#nullable enable

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Uno.UI.RuntimeTests.Helpers;
using Windows.System;
using Windows.UI.Core;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation;

[TestClass]
[RunsOnUIThread]
public class Given_HyperlinkAutomation
{
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24719")]
	public async Task When_Button_Focused_Then_AutomationFocusChanged_Raised()
	{
		var other = new Button { Content = "Other" };
		var button = new Button { Content = "Target" };
		await UITestHelper.Load(new StackPanel { Children = { other, button } });
		other.Focus(FocusState.Keyboard);
		await WindowHelper.WaitForIdle();

		var peer = FrameworkElementAutomationPeer.CreatePeerForElement(button);
		var listener = new RecordingListener();
		using (listener.Attach())
		{
			button.Focus(FocusState.Keyboard);
			await WindowHelper.WaitForIdle();
		}

		CollectionAssert.Contains(listener.FocusChangedPeers, peer);
		Assert.IsTrue(peer.HasKeyboardFocus());
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24719")]
	[DataRow(false)]
	[DataRow(true)]
	public async Task When_Hyperlink_Focused_Then_AutomationFocusChanged_Raised(bool inRichTextBlock)
	{
		var other = new Button { Content = "Other" };
		var hyperlink = CreateHyperlink();
		await UITestHelper.Load(new StackPanel { Children = { other, CreateHost(hyperlink, inRichTextBlock) } });
		other.Focus(FocusState.Keyboard);
		await WindowHelper.WaitForIdle();

		var peer = hyperlink.GetOrCreateAutomationPeer();
		Assert.IsNotNull(peer);
		Assert.IsFalse(peer.HasKeyboardFocus(), "HasKeyboardFocus before focusing the Hyperlink");

		var listener = new RecordingListener();
		using (listener.Attach())
		{
			Assert.IsTrue(hyperlink.Focus(FocusState.Keyboard), "Hyperlink.Focus should succeed");
			await WindowHelper.WaitForIdle();
		}

		Assert.AreSame(hyperlink, FocusManager.GetFocusedElement(WindowHelper.XamlRoot));
		var raisedOn = string.Join(", ", listener.FocusChangedPeers.Select(p => p.GetType().Name));
		Assert.IsTrue(listener.FocusChangedPeers.Contains(peer), $"AutomationFocusChanged not raised on the Hyperlink peer (raised on: [{raisedOn}])");
		Assert.AreSame(peer, FocusManager.GetFocusedAutomationPeer());
		Assert.IsTrue(peer.HasKeyboardFocus(), "HyperlinkAutomationPeer.HasKeyboardFocus");

		// Moving focus away must hand the focused automation peer to the new element.
		var otherPeer = FrameworkElementAutomationPeer.CreatePeerForElement(other);
		other.Focus(FocusState.Keyboard);
		await WindowHelper.WaitForIdle();

		Assert.IsFalse(peer.HasKeyboardFocus(), "HasKeyboardFocus after focus moved away");
		Assert.IsTrue(otherPeer.HasKeyboardFocus());
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24719")]
	public async Task When_Hyperlink_Reached_With_Tab_Then_AutomationFocusChanged_Raised()
	{
		var other = new Button { Content = "Other" };
		var hyperlink = CreateHyperlink();
		await UITestHelper.Load(new StackPanel { Children = { other, CreateHost(hyperlink, inRichTextBlock: true) } });
		other.Focus(FocusState.Keyboard);
		await WindowHelper.WaitForIdle();

		var peer = hyperlink.GetOrCreateAutomationPeer();
		var listener = new RecordingListener();
		using (listener.Attach())
		{
			await TapKey(VirtualKey.Tab);
		}

		Assert.AreSame(hyperlink, FocusManager.GetFocusedElement(WindowHelper.XamlRoot), "Tab should move focus to the Hyperlink");
		CollectionAssert.Contains(listener.FocusChangedPeers, peer);
		Assert.IsTrue(peer.HasKeyboardFocus());
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24678")]
	public async Task When_Button_Invoked_Then_InvokePatternOnInvoked_Raised()
	{
		var button = new Button { Content = "Test" };
		await UITestHelper.Load(button);
		var peer = (ButtonAutomationPeer)FrameworkElementAutomationPeer.CreatePeerForElement(button);

		var listener = new RecordingListener();
		using (listener.Attach())
		{
			((IInvokeProvider)peer).Invoke();
		}

		CollectionAssert.Contains(listener.InvokedPeers, peer);
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24678")]
	[DataRow(false)]
	[DataRow(true)]
	public async Task When_Hyperlink_Invoked_Then_InvokePatternOnInvoked_Raised(bool inRichTextBlock)
	{
		var hyperlink = CreateHyperlink();
		var listener = new RecordingListener();
		hyperlink.Click += (_, _) => listener.Log.Add("Click");
		await UITestHelper.Load(CreateHost(hyperlink, inRichTextBlock));

		var peer = hyperlink.GetOrCreateAutomationPeer();
		Assert.IsNotNull(peer);

		using (listener.Attach())
		{
			((IInvokeProvider)peer).Invoke();
		}

		// Like CHyperlink::Navigate: Click is raised first, then Hyperlink::OnClick raises the UIA event.
		CollectionAssert.AreEqual(new[] { "Click", "Invoked:HyperlinkAutomationPeer" }, listener.Log);
		CollectionAssert.Contains(listener.InvokedPeers, peer);
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24678")]
	[DataRow(VirtualKey.Enter)]
	[DataRow(VirtualKey.Space)]
	public async Task When_Hyperlink_Activated_With_Keyboard_Then_InvokePatternOnInvoked_Raised(VirtualKey key)
	{
		var hyperlink = CreateHyperlink();
		var clicks = 0;
		hyperlink.Click += (_, _) => clicks++;
		await UITestHelper.Load(CreateHost(hyperlink, inRichTextBlock: true));

		Assert.IsTrue(hyperlink.Focus(FocusState.Keyboard));
		await WindowHelper.WaitForIdle();

		var peer = hyperlink.GetOrCreateAutomationPeer();
		var listener = new RecordingListener();
		using (listener.Attach())
		{
			await TapKey(key);
		}

		Assert.AreEqual(1, clicks, "Click should be raised");
		CollectionAssert.Contains(listener.InvokedPeers, peer);
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24678")]
	public async Task When_No_Invoke_Listener_Then_Hyperlink_Still_Clicks()
	{
		var hyperlink = CreateHyperlink();
		var clicks = 0;
		hyperlink.Click += (_, _) => clicks++;
		await UITestHelper.Load(CreateHost(hyperlink, inRichTextBlock: false));

		var peer = hyperlink.GetOrCreateAutomationPeer();
		var listener = new RecordingListener { IsListening = false };
		using (listener.Attach())
		{
			((IInvokeProvider)peer).Invoke();
		}

		Assert.AreEqual(1, clicks);
		Assert.AreEqual(0, listener.InvokedPeers.Count);
	}

	private static Hyperlink CreateHyperlink()
	{
		var hyperlink = new Hyperlink();
		hyperlink.Inlines.Add(new Run { Text = "Link" });
		return hyperlink;
	}

	private static FrameworkElement CreateHost(Hyperlink hyperlink, bool inRichTextBlock)
	{
		if (inRichTextBlock)
		{
			var paragraph = new Paragraph();
			paragraph.Inlines.Add(new Run { Text = "Before " });
			paragraph.Inlines.Add(hyperlink);
			var richTextBlock = new RichTextBlock();
			richTextBlock.Blocks.Add(paragraph);
			return richTextBlock;
		}

		var textBlock = new TextBlock();
		textBlock.Inlines.Add(new Run { Text = "Before " });
		textBlock.Inlines.Add(hyperlink);
		return textBlock;
	}

	private static async Task TapKey(VirtualKey key)
	{
		var keyboard = WindowHelper.XamlRoot!.VisualTree.ContentRoot.InputManager.Keyboard;

		keyboard.OnKeyTestingOnly(new KeyEventArgs("test", key, VirtualKeyModifiers.None, new CorePhysicalKeyStatus()), true);
		await WindowHelper.WaitForIdle();

		keyboard.OnKeyTestingOnly(new KeyEventArgs("test", key, VirtualKeyModifiers.None, new CorePhysicalKeyStatus()), false);
		await WindowHelper.WaitForIdle();
	}

	private sealed class RecordingListener : IAutomationPeerListener
	{
		public bool IsListening { get; set; } = true;

		public List<string> Log { get; } = new();

		public List<AutomationPeer> FocusChangedPeers { get; } = new();

		public List<AutomationPeer> InvokedPeers { get; } = new();

		public System.IDisposable Attach()
		{
			var previous = AutomationPeer.TestAutomationPeerListener;
			AutomationPeer.TestAutomationPeerListener = this;
			return Uno.Disposables.Disposable.Create(() => AutomationPeer.TestAutomationPeerListener = previous);
		}

		public void OnAutomationEvent(AutomationPeer peer, AutomationEvents eventId)
		{
			if (eventId == AutomationEvents.AutomationFocusChanged)
			{
				FocusChangedPeers.Add(peer);
			}
			else if (eventId == AutomationEvents.InvokePatternOnInvoked)
			{
				InvokedPeers.Add(peer);
				Log.Add("Invoked:" + peer.GetType().Name);
			}
		}

		public bool ListenerExistsHelper(AutomationEvents eventId) => IsListening;
		public void NotifyNotificationEvent(AutomationPeer peer, AutomationNotificationKind notificationKind, AutomationNotificationProcessing notificationProcessing, string displayString, string activityId) { }
		public void NotifyPropertyChangedEvent(AutomationPeer peer, AutomationProperty automationProperty, object oldValue, object newValue) { }
		public void NotifyAutomationEvent(AutomationPeer peer, AutomationEvents eventId) { }
		public void NotifyStructureChangedEvent(AutomationPeer peer, AutomationStructureChangeType structureChangeType, AutomationPeer? child) { }
		public void NotifyInvalidatePeer(AutomationPeer peer) { }
		public void NotifyTextEditTextChangedEvent(AutomationPeer peer, AutomationTextEditChangeType changeType, IReadOnlyList<string> changedData) { }
	}
}
