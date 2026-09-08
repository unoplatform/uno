#nullable enable

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation;

public partial class Given_SkiaIOSAccessibilityElement
{
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Ownerless_Peers_Have_Independent_Native_Elements()
	{
		var host = new VirtualPeerHost();
		try
		{
			await UITestHelper.Load(host);
			var root = host.XamlRoot;
			Assert.IsNotNull(root);
			var snapshots = GetOrderedSnapshots(root)
				.Where(node => node.AutomationId is "virtual-first" or "virtual-second")
				.ToArray();

			CollectionAssert.AreEqual(
				new[] { "virtual-first", "virtual-second" },
				snapshots.Select(node => node.AutomationId).ToArray());
			Assert.AreNotSame(snapshots[0].NativeNode, snapshots[1].NativeNode);
			Assert.AreEqual(37, snapshots[0].Bounds.Width);
			Assert.AreEqual(41, snapshots[0].Bounds.Height);
			Assert.IsTrue(ActivateNativeElement(snapshots[0].NativeNode));
			Assert.AreEqual(1, host.First.InvokeCount);
			Assert.AreEqual(0, host.Second.InvokeCount);

			host.Peers.Remove(host.First);
			var added = new VirtualInvokePeer("virtual-third");
			host.Peers.Add(added);
			host.GetOrCreateAutomationPeer()!.InvalidatePeer();
			Assert.IsFalse(ActivateNativeElement(snapshots[0].NativeNode));
			await UITestHelper.WaitForIdle();

			var updated = GetOrderedSnapshots(root);
			CollectionAssert.AreEqual(
				new[] { "virtual-second", "virtual-third" },
				updated.Where(node => node.AutomationId is "virtual-first" or "virtual-second" or "virtual-third")
					.Select(node => node.AutomationId).ToArray());
			var remaining = updated
				.Single(node => node.AutomationId == "virtual-second");
			Assert.AreSame(snapshots[1].NativeNode, remaining.NativeNode);
			Assert.IsTrue(ActivateNativeElement(remaining.NativeNode));
			Assert.AreEqual(1, host.Second.InvokeCount);
			Assert.IsTrue(ActivateNativeElement(
				updated.Single(node => node.AutomationId == "virtual-third").NativeNode));
			Assert.AreEqual(1, added.InvokeCount);
		}
		finally
		{
			TestServices.WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_EventsSource_Is_Rebound_Then_Retained_Native_Node_Is_Retired()
	{
		var host = new ReboundPeerHost { Width = 100, Height = 100 };
		try
		{
			await UITestHelper.Load(host);
			var original = GetSnapshot(host);
			Assert.IsNotNull(original);
			Assert.AreEqual("virtual-first", original.AutomationId);

			host.GetOrCreateAutomationPeer()!.EventsSource = host.Second;
			host.GetOrCreateAutomationPeer()!.InvalidatePeer();

			Assert.IsFalse(ActivateNativeElement(original.NativeNode));
			await UITestHelper.WaitForIdle();
			var replacement = GetSnapshot(host);
			Assert.IsNotNull(replacement);
			Assert.AreNotSame(original.NativeNode, replacement.NativeNode);
			Assert.AreEqual("virtual-second", replacement.AutomationId);
			Assert.IsFalse(ActivateNativeElement(original.NativeNode));
			Assert.IsTrue(ActivateNativeElement(replacement.NativeNode));
			Assert.AreEqual(0, host.First.InvokeCount);
			Assert.AreEqual(1, host.Second.InvokeCount);
		}
		finally
		{
			TestServices.WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Ownerless_Peer_Host_Is_Unloaded_Then_Retained_Node_Is_Unavailable()
	{
		var host = new VirtualPeerHost();
		try
		{
			await UITestHelper.Load(host);
			var root = host.XamlRoot;
			Assert.IsNotNull(root);
			var native = GetOrderedSnapshots(root)
				.Single(node => node.AutomationId == "virtual-first").NativeNode;

			TestServices.WindowHelper.WindowContent = null;
			Assert.IsFalse(ActivateNativeElement(native));
			await UITestHelper.WaitForIdle();
			Assert.IsFalse(ActivateNativeElement(native));
			Assert.AreEqual(0, host.First.InvokeCount);
		}
		finally
		{
			TestServices.WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Unparented_Popup_Is_Open_Then_Its_Native_Peer_Can_Dismiss_It()
	{
		var root = new Grid { Width = 200, Height = 200 };
		var child = new Button { Content = "Popup content", Width = 100, Height = 40 };
		var popup = new Popup { Child = child, IsLightDismissEnabled = true };
		try
		{
			await UITestHelper.Load(root);
			popup.XamlRoot = root.XamlRoot;
			popup.IsOpen = true;
			await TestServices.WindowHelper.WaitForLoaded(child);
			await UITestHelper.WaitForIdle();

			Assert.IsFalse(popup.IsLoaded);
			Assert.IsNotNull(GetSnapshot(popup));
			Assert.IsTrue(InvokeAction(popup, AccessibilityNativeAction.Dismiss));
			Assert.IsFalse(popup.IsOpen);
		}
		finally
		{
			popup.IsOpen = false;
			TestServices.WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Identifier_Uses_FrameworkElement_Name_Fallback()
	{
		var button = new Button { Name = "namedButton", Content = "Spoken name" };
		try
		{
			await UITestHelper.Load(button);
			var snapshot = GetSnapshot(button);
			Assert.IsNotNull(snapshot);
			Assert.AreEqual("namedButton", snapshot.AutomationId);
			Assert.AreEqual("Spoken name", snapshot.Name);
		}
		finally
		{
			TestServices.WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Peer_Overrides_Identifier_And_Culture_Then_Native_Properties_Use_Overrides()
	{
		var button = new ContractPeerButton { Content = "Custom peer", Language = "fr-FR" };
		AutomationProperties.SetAutomationId(button, "attached-id");
		try
		{
			await UITestHelper.Load(button);
			var snapshot = GetSnapshot(button);
			Assert.IsNotNull(snapshot);
			Assert.AreEqual("peer-id", snapshot.AutomationId);
			Assert.AreEqual("ja-JP", GetNativeLanguage(snapshot.NativeNode));
		}
		finally
		{
			TestServices.WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	[DataRow(false, 0, "fr-FR")]
	[DataRow(true, 0, null)]
	[DataRow(true, 1033, "en-US")]
	public async Task When_Language_Is_Used_Only_If_Culture_Is_Unset(
		bool setCulture, int culture, string? expectedLanguage)
	{
		var button = new Button { Content = "Language", Language = "fr-FR" };
		if (setCulture)
		{
			AutomationProperties.SetCulture(button, culture);
		}

		try
		{
			await UITestHelper.Load(button);
			var snapshot = GetSnapshot(button);
			Assert.IsNotNull(snapshot);
			Assert.AreEqual(expectedLanguage, GetNativeLanguage(snapshot.NativeNode));
		}
		finally
		{
			TestServices.WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	[DataRow(false)]
	[DataRow(true)]
	public async Task When_Child_Scroll_Gesture_Routes_To_Ancestor(bool rawScrollViewer)
	{
		var button = new Button { Content = "Scroll from here", Height = 44 };
		var scroller = new ScrollViewer
		{
			Width = 150,
			Height = 100,
			Content = new StackPanel
			{
				Children =
				{
					button,
					new Border { Height = 300 },
				},
			},
		};
		if (rawScrollViewer)
		{
			AutomationProperties.SetAccessibilityView(scroller, AccessibilityView.Raw);
		}

		try
		{
			await UITestHelper.Load(scroller);
			Assert.AreEqual(0, scroller.VerticalOffset);
			Assert.IsTrue(InvokeAction(button, AccessibilityNativeAction.ScrollForward));
			await TestServices.WindowHelper.WaitFor(() => scroller.VerticalOffset > 0);
		}
		finally
		{
			TestServices.WindowHelper.WindowContent = null;
		}
	}

	private static bool ActivateNativeElement(object element)
	{
		var activate = element.GetType().GetMethod("AccessibilityActivate");
		Assert.IsNotNull(activate);
		return activate.Invoke(element, null) is true;
	}

	private static string? GetNativeLanguage(object element)
	{
		var language = element.GetType().GetProperty("AccessibilityLanguage");
		Assert.IsNotNull(language);
		return language.GetValue(element) as string;
	}

	private sealed partial class VirtualPeerHost : Grid
	{
		internal VirtualPeerHost()
		{
			Width = 100;
			Height = 100;
			Peers = new() { First, Second };
		}

		internal VirtualInvokePeer First { get; } = new("virtual-first");
		internal VirtualInvokePeer Second { get; } = new("virtual-second");
		internal List<AutomationPeer> Peers { get; }

		protected override AutomationPeer OnCreateAutomationPeer() => new VirtualHostPeer(this);
	}

	private sealed class VirtualHostPeer : FrameworkElementAutomationPeer
	{
		private readonly VirtualPeerHost _host;

		internal VirtualHostPeer(VirtualPeerHost host) : base(host) => _host = host;

		protected override IList<AutomationPeer> GetChildrenCore() => _host.Peers;
		protected override bool IsControlElementCore() => false;
		protected override bool IsContentElementCore() => false;
	}

	private sealed class VirtualInvokePeer : AutomationPeer, IInvokeProvider
	{
		private readonly string _id;

		internal VirtualInvokePeer(string id) => _id = id;

		internal int InvokeCount { get; private set; }

		public void Invoke() => InvokeCount++;

		protected override string GetNameCore() => _id;
		protected override string GetAutomationIdCore() => _id;
		protected override Windows.Foundation.Rect GetBoundingRectangleCore() => new(11, 13, 37, 41);
		protected override bool IsControlElementCore() => true;
		protected override bool IsContentElementCore() => true;
		protected override object? GetPatternCore(PatternInterface patternInterface)
			=> patternInterface == PatternInterface.Invoke ? this : base.GetPatternCore(patternInterface);
	}

	private sealed partial class ReboundPeerHost : Grid
	{
		internal VirtualInvokePeer First { get; } = new("virtual-first");
		internal VirtualInvokePeer Second { get; } = new("virtual-second");

		protected override AutomationPeer OnCreateAutomationPeer()
			=> new ReboundHostPeer(this) { EventsSource = First };
	}

	private sealed class ReboundHostPeer : FrameworkElementAutomationPeer
	{
		internal ReboundHostPeer(FrameworkElement owner) : base(owner)
		{
		}

		protected override bool IsControlElementCore() => true;
		protected override bool IsContentElementCore() => true;
	}

	private sealed partial class ContractPeerButton : Button
	{
		protected override AutomationPeer OnCreateAutomationPeer() => new ContractPeer(this);
	}

	private sealed class ContractPeer : ButtonAutomationPeer
	{
		internal ContractPeer(Button owner) : base(owner)
		{
		}

		protected override string GetAutomationIdCore() => "peer-id";
		protected override int GetCultureCore() => 1041;
	}
}
